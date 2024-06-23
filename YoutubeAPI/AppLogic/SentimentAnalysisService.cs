using Microsoft.ML;
using SentimentModelTraining;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeAPI.AppLogic
{
    public class SentimentAnalysisService
    {
        private static ITransformer? _sentimentModel;
        private static MLContext? _mlContext;
        private static readonly string _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\YoutubeAPI\\sentiment_model.zip");

        public static void InitializeSentimentModel()
        {
            _mlContext = new MLContext();
            _sentimentModel = _mlContext.Model.Load(_modelPath, out _);
        }

        public static SentimentPrediction[] AnalyzeSentiment(SentimentData[] data)
        {
            var dataView = _mlContext!.Data.LoadFromEnumerable(data);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_sentimentModel);
            var predictions = data.Select(predictionEngine.Predict).ToArray();
            return predictions;
        }
    }
}
