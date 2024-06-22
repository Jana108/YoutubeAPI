using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentModelTraining
{
    public class SentimentModelBuilder
    {
        private static readonly string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\YoutubeAPI\\DataSet.csv");
        private static readonly string _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\YoutubeAPI\\sentiment_model.zip");

        public static void BuildAndTrainModel()
        {
            var context = new MLContext();

            var data = context.Data.LoadFromTextFile<SentimentData>(_dataPath, hasHeader: true, separatorChar: ',');

            var dataPipeline = context.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.SentimentText))
                .Append(context.Transforms.CopyColumns("Label", nameof(SentimentData.Sentiment)))
                .Append(context.BinaryClassification.Trainers.SdcaLogisticRegression("Label", "Features"));

            var model = dataPipeline.Fit(data);

            context.Model.Save(model, data.Schema, _modelPath);

            Console.WriteLine("Model trained and saved.");
        }
    }
}
