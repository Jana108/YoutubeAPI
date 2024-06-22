using Newtonsoft.Json;
using YoutubeAPI.Utils;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Google.Apis.YouTube.v3;
using SentimentModelTraining;
using Google.Apis.Services;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using YoutubeAPI.Services;

namespace YoutubeAPI.AppLogic
{
    public class WebAPI
    {
        private static readonly HttpListener _listener = new();
        private static readonly string _url = "http://localhost";
        private static readonly int _port = 5000;
        private static readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);

        private static readonly CancellationTokenSource _cts = new();
        private static Task _apiTask = null!;

        public static YouTubeService? _youtubeService;

        static WebAPI()
        {
            _listener.Prefixes.Add($"{_url}:{_port}/");
            InitializeYouTubeService();
            SentimentAnalysisService.InitializeSentimentModel();
        }

        public static void Start()
        {
            try
            {
                _listener.Start();
                LoggerAsync.Log(LogLevel.Info, $"WebAPI started listening at port {_port}");
                _apiTask = Task.Run(() => Listen(), _cts.Token);
            }
            catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
            {
                LoggerAsync.Log(LogLevel.Info, "Listener was stopped. Press ENTER to exit.");
                _cts.Cancel();
            }
            catch (Exception ex)
            {
                LoggerAsync.Log(LogLevel.Error, $"Unexcpeted error: {ex.Message}");
            }

        }

        private static async Task Listen()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
            }
        }

        public async static Task StopAsync()
        {
            LoggerAsync.Dispose();
            _cts.Cancel();
            _listener.Stop();
            await _apiTask;
        }

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            await _semaphore.WaitAsync(_cts.Token);
            try
            {
                var rawUrl = context.Request.RawUrl;
                LoggerAsync.Log(LogLevel.Info, $"Processing request: {rawUrl}");

                NameValueCollection queryParams = context.Request.QueryString;
                var videoIds = queryParams.Get("videoIds")?.Split(',');

                if (videoIds == null || videoIds.Length == 0)
                {
                    await ReturnResponseAsync(StatusCode.BadRequest, "Invalid query parameters!", context, rawUrl);
                    return;
                }

                bool hasInvalidVideoId = false;

                var commentTasks = videoIds.ToObservable()
                    .SelectMany(async videoId =>
                    {
                        try
                        {
                            var comments = await YoutubeClient.GetVideoCommentsAsync(videoId);
                            return new { VideoId = videoId, Comments = comments };
                        }
                        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            LoggerAsync.Log(LogLevel.Error, $"Error processing video ID {videoId}: {ex.Message}");
                            hasInvalidVideoId = true;
                            return new { VideoId = videoId, Comments = Array.Empty<string>() };
                        }
                    })
                    .ObserveOn(TaskPoolScheduler.Default);

                var allComments = await commentTasks.ToList();

                if (hasInvalidVideoId)
                {
                    await ReturnResponseAsync(StatusCode.BadRequest, "One or more video IDs are invalid.", context, rawUrl);
                    return;
                }

                var sentimentResults = allComments.SelectMany(video => video.Comments.Select(comment => new { video.VideoId, SentimentData = new SentimentData { SentimentText = comment } })).ToArray();
                var predictions = sentimentResults.Select(sr => new { sr.VideoId, SentimentPrediction = SentimentAnalysisService.AnalyzeSentiment(new[] { sr.SentimentData }).First() });

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
            finally
            {
                _semaphore.Release();
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

        private static void InitializeYouTubeService()
        {
            string apiKey = "AIzaSyD7TX0XdOkdGIFRzxixN2IhYcBSP6n43ek";
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTubeCommentAnalyzer"
            });
        }
    }
}
