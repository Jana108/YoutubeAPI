using Microsoft.ML;
using SentimentModelTraining;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YoutubeAPI.AppLogic
{
    public static class SentimentAnalysisService
    {
        private static readonly PredictionEngine<SentimentData, SentimentPrediction> _predictionEngine;

        private static readonly string _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\YoutubeAPI\\sentiment_model.zip");
        private static readonly object _lock;

        static SentimentAnalysisService()
        {
            var mlContext = new MLContext();
            var sentimentModel = mlContext.Model.Load(_modelPath, out _);
            _lock = new();

            _predictionEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(sentimentModel);
        }

        public static SentimentPrediction[] AnalyzeSentiment(SentimentData[] data)
        {
            lock (_lock)
            {
                var predictions = data.Select(_predictionEngine.Predict).ToArray();
                return predictions;
            }
        }
    }
}
