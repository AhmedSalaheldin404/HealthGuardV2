using HealthGuard.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace HealthGuard.Services;

public interface IMLModelService
{
    Task<(string prediction, double confidence)> Predict(Dictionary<string, double> features, ModelType modelType);
    void LoadModel(ModelType modelType, string modelPath);
}

public class MLModelService : IMLModelService
{
    private readonly MLContext _mlContext;
    private readonly Dictionary<ModelType, ITransformer> _models = new();

    public MLModelService()
    {
        _mlContext = new MLContext(seed: 1);
    }

    public void LoadModel(ModelType modelType, string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("Model file not found", modelPath);

        var model = _mlContext.Model.Load(modelPath, out var _);
        _models[modelType] = model;
    }

    public async Task<(string prediction, double confidence)> Predict(Dictionary<string, double> features, ModelType modelType)
    {
        if (!_models.ContainsKey(modelType))
            throw new InvalidOperationException($"Model '{modelType}' not loaded");

        ITransformer model = _models[modelType];

        // Determine the prediction engine and prediction class based on model type
        switch (modelType)
        {
            case ModelType.BreastCancerModel:
                var predictionEngineBreastCancer = _mlContext.Model.CreatePredictionEngine<BreastCancerData, BreastCancerPrediction>(model);
                var predictionBreastCancer = predictionEngineBreastCancer.Predict(new BreastCancerData { Features = features.Values.ToArray() });
                return (predictionBreastCancer.Prediction.ToString(), predictionBreastCancer.Probability);

            case ModelType.HeartDiseaseModel:
                var predictionEngineHeartDisease = _mlContext.Model.CreatePredictionEngine<HeartDiseaseData, HeartDiseasePrediction>(model);
                var predictionHeartDisease = predictionEngineHeartDisease.Predict(new HeartDiseaseData { Features = features.Values.ToArray() });
                return (predictionHeartDisease.Prediction.ToString(), predictionHeartDisease.Score);

            case ModelType.DiabetesModel:
                var predictionEngineDiabetes = _mlContext.Model.CreatePredictionEngine<DiabetesData, DiabetesPrediction>(model);
                var predictionDiabetes = predictionEngineDiabetes.Predict(new DiabetesData { Features = features.Values.ToArray() });
                return (predictionDiabetes.Prediction.ToString(), predictionDiabetes.Confidence);

            default:
                throw new NotSupportedException($"Model type '{modelType}' is not supported.");
        }
    }
}

public class BreastCancerData
{
    [VectorType(30)]
    public double[] Features { get; set; } = Array.Empty<double>();
    public bool Label { get; set; }
}

public class BreastCancerPrediction
{
    public bool Prediction { get; set; }
    public double Probability { get; set; }
}

public class HeartDiseaseData
{
    [VectorType(13)]
    public double[] Features { get; set; } = Array.Empty<double>();
    public bool Label { get; set; }
}

public class HeartDiseasePrediction
{
    public bool Prediction { get; set; }
    public float Score { get; set; }
}

public class DiabetesData
{
    [VectorType(8)]
    public double[] Features { get; set; } = Array.Empty<double>();
    public bool Label { get; set; }
}

public class DiabetesPrediction
{
    public bool Prediction { get; set; }
    public double Confidence { get; set; }
}
