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
using System.Threading;

namespace YoutubeAPI.AppLogic
{
    public class WebAPI
    {
        private static readonly string _url = "http://localhost";
        private static readonly int _port = 5000;

        private static readonly CancellationTokenSource _cts = new();
        private static HttpListenerObservable _httpListenerObservable = null!;
        private static IDisposable _observerSubscription = null!;

        static WebAPI()
        {
            SentimentAnalysisService.InitializeSentimentModel();
        }

        public static void Start()
        {
            ThreadPool.SetMaxThreads(16, 16);

            try
            {
                _httpListenerObservable = new HttpListenerObservable(_url, _port);
                var httpRequestObserver = new HttpRequestObserver(_cts.Token);

                _observerSubscription = _httpListenerObservable
                    .SubscribeOn(TaskPoolScheduler.Default)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(httpRequestObserver);

                LoggerAsync.Log(LogLevel.Info, $"WebAPI started listening at port {_port}");

                Task.Factory.StartNew(() => _httpListenerObservable.StartListening(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                LoggerAsync.Log(LogLevel.Error, $"Unexpected error: {ex.Message}");
                _cts.Cancel();
            }
        }

        public static async Task StopAsync()
        {
            LoggerAsync.Dispose();
            _cts.Cancel();
            _observerSubscription.Dispose();
            await Task.CompletedTask;
        }
    }
}
