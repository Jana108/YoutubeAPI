using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoutubeAPI.Utils;

namespace YoutubeAPI.AppLogic
{
    public class HttpListenerObservable : IObservable<HttpListenerContext>
    {
        private readonly HttpListener _listener;
        private readonly List<IObserver<HttpListenerContext>> _observers;

        public HttpListenerObservable(string url, int port)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"{url}:{port}/");
            _observers = [];
        }

        public IDisposable Subscribe(IObserver<HttpListenerContext> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber(_observers, observer);
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            Console.WriteLine($"StartListening: {Environment.CurrentManagedThreadId}");
            _listener.Start();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    foreach (var observer in _observers)
                    {
                        observer.OnNext(context);
                    }
                }
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                LoggerAsync.Log(LogLevel.Info, "Listener was stopped. Press ENTER to exit.");
            }
            finally
            {
                _listener.Stop();
                foreach (var observer in _observers)
                {
                    observer.OnCompleted();
                }
            }
        }

        private class Unsubscriber(List<IObserver<HttpListenerContext>> observers, IObserver<HttpListenerContext> observer) : IDisposable
        {
            private readonly List<IObserver<HttpListenerContext>> _observers = observers;
            private readonly IObserver<HttpListenerContext> _observer = observer;

            public void Dispose()
            {
                _observers.Remove(_observer);
            }
        }
    }


}
