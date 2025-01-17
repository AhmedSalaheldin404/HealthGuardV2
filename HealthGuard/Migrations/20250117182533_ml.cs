using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthGuard.Migrations
{
    /// <inheritdoc />
    public partial class ml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Prediction",
                table: "Diagnoses",
                newName: "Diagnose");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Diagnose",
                table: "Diagnoses",
                newName: "Prediction");
        }
    }
}
