using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using HealthGuard.Services;
using HealthGuard.Models;
using HealthGuard.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using static HealthGuard.Services.AuthService;
using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== Configure Services ==========
builder.Services.AddControllers();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Debugging: Print SendGrid API Key to verify it's being read
var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"];
Console.WriteLine($"SendGrid API Key: {sendGridApiKey}");

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Health API", Version = "v1" });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<HealthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSqlConnection")));

// Register services
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDiagnosticService, DiagnosticService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMLModelService, MLModelService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();


// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured"))),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ========== Configure Middleware ==========

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


// ========== Gemini Chatbot Endpoint ==========
app.MapPost("/chatbot", async ([FromBody] ChatbotRequest request) =>
{
    if (string.IsNullOrEmpty(request.Message))
    {
        return Results.BadRequest("Invalid input. Please provide a message.");
    }

    var apiKey = builder.Configuration["Gemini:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("Gemini API Key is not configured.");
    }

    var httpClient = new HttpClient();

    // Prepare the request for Gemini API
    var geminiRequestBody = new
    {
        contents = new[]
        {
            new { parts = new[] { new { text = request.Message } } }
        }
    };

    var requestContent = new StringContent(
        JsonSerializer.Serialize(geminiRequestBody),
        Encoding.UTF8,
        "application/json"
    );

    try
    {
        var geminiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

        // Send the request to Gemini API
        var response = await httpClient.PostAsync(geminiEndpoint, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            return Results.Problem($"Error calling Gemini API: {response.StatusCode}. Details: {errorDetails}");
        }

        var geminiResponse = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(geminiResponse);
        string? text = responseObject
                             .GetProperty("candidates")[0]
                             .GetProperty("content")
                             .GetProperty("parts")[0]
                             .GetProperty("text")
                             .GetString();
        return Results.Ok(new
        {
            message = text
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("Chatbot")
.WithTags("Chatbot");

app.Run();

// Request class for Chatbot
public class ChatbotRequest
{
    public string Message { get; set; }
}