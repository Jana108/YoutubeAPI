using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using SentimentModelTraining;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeAPI.Services;
using YoutubeAPI.Utils;

namespace YoutubeAPI.AppLogic
{
    public class HttpRequestObserver(CancellationToken cancellationToken) : IObserver<HttpListenerContext>
    {
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public void OnCompleted()
        {
            LoggerAsync.Log(LogLevel.Info, "Stopped listening for HTTP requests.");
        }

        public void OnError(Exception error)
        {
            LoggerAsync.Log(LogLevel.Error, $"Error occurred: {error.Message}");
        }

        public void OnNext(HttpListenerContext context)
        {
            if (_cancellationToken.IsCancellationRequested) return;

            Console.WriteLine($"OnNext: {Environment.CurrentManagedThreadId}");
            _ = HandleRequestAsync(context);
        }

        private async static Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                Console.WriteLine($"HandleRequestAsync: {Environment.CurrentManagedThreadId}");
                var rawUrl = context.Request.RawUrl;
                LoggerAsync.Log(LogLevel.Info, $"Processing request: {rawUrl}");

                var videoIds = context.Request.QueryString.Get("videoIds")?.Split(',');

                if (videoIds == null || videoIds.Length == 0)
                {
                    await ReturnResponseAsync(StatusCode.BadRequest, "Invalid query parameters!", context, rawUrl);
                    return;
                }

                if (videoIds.Length > 5)
                {
                    await ReturnResponseAsync(StatusCode.BadRequest, "Cannot process more than 5 videos with 1 request!", context, rawUrl);
                    return;
                }

                bool hasInvalidVideoId = false;

                var tasks = videoIds.Select(async id =>
                {
                    try
                    {
                        var service = new YoutubeService();
                        var comments = await service.GetVideoCommentsAsync(id);
                        return new { VideoId = id, Comments = comments };
                    }
                    catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        LoggerAsync.Log(LogLevel.Error, $"Error processing video ID {id}: {ex.Message}");
                        hasInvalidVideoId = true;
                        return new { VideoId = id, Comments = Array.Empty<string>() };
                    }
                });

                var allComments = await Task.WhenAll(tasks);

                if (hasInvalidVideoId)
                {
                    await ReturnResponseAsync(StatusCode.BadRequest, "One or more video IDs are invalid.", context, rawUrl);
                    return;
                }

                var sentimentResults = allComments.SelectMany(video => video.Comments.Select(comment => new { video.VideoId, SentimentData = new SentimentData { SentimentText = comment } })).ToArray();
                var predictions = sentimentResults.Select(sr => new { sr.VideoId, SentimentPrediction = SentimentAnalysisService.AnalyzeSentiment([sr.SentimentData]).First() });

                var resultsGroupedByVideo = predictions
                    .GroupBy(p => p.VideoId)
                    .Select(group => new
                    {
                        VideoId = group.Key,
                        AverageSentiment = group.Any() ? group.Average(p => p.SentimentPrediction.Score) : 0,
                        Comments = group.Select(p => new { p.SentimentPrediction.SentimentText, p.SentimentPrediction.Score }).ToArray()
                    });

                await ReturnResponseAsync(StatusCode.Ok, resultsGroupedByVideo, context, rawUrl);
            }
            catch (Exception e)
            {
                LoggerAsync.Log(LogLevel.Error, e.Message);
                await ReturnResponseAsync(StatusCode.InternalError, e.Message, context);
            }

        }

        private static async Task ReturnResponseAsync(StatusCode status, object content, HttpListenerContext context, string? query = null)
        {
            if (context == null) return;

            string jsonResponse = status == StatusCode.Ok ? JsonConvert.SerializeObject(content) : (string)content;

            context.Response.ContentType = status == StatusCode.Ok ? "application/json" : "text/plain";
            context.Response.StatusCode = (int)status;
            context.Response.StatusDescription = status.ToString();
            context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(jsonResponse);

            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(jsonResponse));
            context.Response.OutputStream.Close();

            LoggerAsync.Log(LogLevel.Info, $"Returned {status} response to client. (query: {query})");
        }
    }


}
