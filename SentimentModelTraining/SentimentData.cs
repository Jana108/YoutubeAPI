using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentModelTraining
{
    public class SentimentData
    {
        [LoadColumn(1)]
        public bool Sentiment { get; set; }

        [LoadColumn(2)]
        public string? SentimentText { get; set; }
    }
}
