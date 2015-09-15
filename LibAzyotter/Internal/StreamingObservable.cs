using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LibAzyotter.Api;
using static LibAzyotter.Api.StreamingApi;

namespace LibAzyotter.Internal
{
    internal class StreamingObservable : IObservable<StreamingMessage>
    {
        public StreamingObservable(TwitterClient client, StreamingType type, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            this.client = client;
            this.type = type;
            this.parameters = parameters.ToArray();
        }

        private readonly TwitterClient client;
        private readonly StreamingType type;
        private readonly KeyValuePair<string, object>[] parameters;

        public IDisposable Subscribe(IObserver<StreamingMessage> observer)
        {
            var conn = new StreamingConnection();
            conn.Start(observer, this.client, this.type, this.parameters);
            return conn;
        }
    }

    internal class StreamingConnection : IDisposable
    {
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();

        public async void Start(IObserver<StreamingMessage> observer, TwitterClient client, StreamingType type, KeyValuePair<string, object>[] parameters)
        {
            try
            {
                var token = this.cancel.Token;
                var t = GetUrl(type);
                using (var res = await client.InternalSendRequestAsync(GetMethodType(type), t.Item1, TwitterClient.ApiVersion, t.Item2, parameters, null, token).ConfigureAwait(false))
                using (var stream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                using (token.Register(reader.Dispose))
                {
                    while (!reader.EndOfStream)
                    {
                        var s = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(s)) continue;
                        try
                        {
                            observer.OnNext(StreamingMessage.Parse(s));
                        }
                        catch (ParsingException ex)
                        {
                            observer.OnNext(RawJsonMessage.Create(s, ex));
                        }
                    }
                }
                observer.OnCompleted();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!this.cancel.IsCancellationRequested)
                    observer.OnError(ex);
            }
        }

        public void Dispose()
        {
            this.cancel.Cancel();
        }
    }
}
