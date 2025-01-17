using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Linq;

namespace HealthGuard.Services
{
    public interface IMLModelService
    {
        Task<(string diagnose, double confidence)> PredictFromImage(IFormFile imageFile); 
    }
    public class MLModelService : IMLModelService
    {
        private readonly InferenceSession _session;

        public MLModelService()
        {
            // Load the ResNet-50 ONNX model
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "resnet50-v2-7.onnx");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found at {modelPath}");

            _session = new InferenceSession(modelPath);
        }

        public async Task<(string diagnose, double confidence)> PredictFromImage(IFormFile imageFile)
        {
            // Read the image file
            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Preprocess the image (resize, normalize, etc.)
            var inputTensor = PreprocessImage(memoryStream);

            // Create input data for the ONNX model
            var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("data", inputTensor)
    };

            // Run inference
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // Postprocess the output (e.g., get the predicted class and confidence)
            var probabilities = output.ToArray();
            var maxProbability = probabilities.Max();
            var predictedClass = probabilities.ToList().IndexOf(maxProbability);

            // Map the predicted class to a label
            var diagnose = $"Class {predictedClass}"; // Example: Replace with actual labels if needed

            return (diagnose, maxProbability); // Ensure names match
        }

        private Tensor<float> PreprocessImage(Stream imageStream)
        {
            // Load the image using ImageSharp
            using var image = Image.Load<Rgb24>(imageStream);

            // Resize the image to the required input size (224x224)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(224, 224),
                Mode = ResizeMode.Crop
            }));

            // Convert the image to a tensor
            var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    tensor[0, 0, y, x] = pixel.R / 255.0f; // Normalize to [0, 1]
                    tensor[0, 1, y, x] = pixel.G / 255.0f;
                    tensor[0, 2, y, x] = pixel.B / 255.0f;
                }
            }

            return tensor;
        }

        private (string diagnose, double confidence) PostprocessOutput(Tensor<float> output)
        {
            // Get the predicted class and confidence
            var probabilities = output.ToArray();
            var maxProbability = probabilities.Max(); // Define maxProbability
            var predictedClass = probabilities.ToList().IndexOf(maxProbability);

            // Map the predicted class to a label
            var diagnose = $"Class {predictedClass}"; // Example: Replace with actual labels if needed

            return (diagnose, maxProbability); // Return maxProbability
        }
    }
}