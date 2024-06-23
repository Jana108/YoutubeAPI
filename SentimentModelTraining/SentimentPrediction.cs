using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentModelTraining
{
    public class SentimentPrediction : SentimentData
    {
        [ColumnName("PredictedLabel")]
        public new bool Sentiment { get ; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
