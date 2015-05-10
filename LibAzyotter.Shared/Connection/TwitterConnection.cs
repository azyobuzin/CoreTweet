using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#if NET45
using System.Net.Security;
#endif

namespace LibAzyotter.Connection
{
    public class TwitterConnection : ITwitterConnection
    {
        public TwitterConnection(IRequestBuilder requestBuilder)
        {
            if (requestBuilder == null)
                throw new ArgumentNullException("requestBuilder");
            this.requestBuilder = requestBuilder;
        }

        private IRequestBuilder requestBuilder;
        public IRequestBuilder RequestBuilder
        {
            get
            {
                return this.requestBuilder;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                this.requestBuilder = value;
            }
        }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

#if NET45
        public int ReadWriteTimeout { get; set; } = 300000;
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
#endif

        public virtual async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken)
        {
            var request = await this.requestBuilder.BuildRequestAsync(method, host, version, relativeUri, parameters, authorizationParameters).ConfigureAwait(false);
            var isStreaming = host == ApiHost.Api || host == ApiHost.Upload;

#if NET45
            var handler = new WebRequestHandler();
            handler.ReadWriteTimeout = isStreaming ? System.Threading.Timeout.Infinite : this.ReadWriteTimeout;
            handler.ServerCertificateValidationCallback = this.ServerCertificateValidationCallback;
#else
            var handler = new HttpClientHandler();
#endif

            if (!isStreaming && handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var client = new HttpClient(handler);
            client.Timeout = this.Timeout;
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}
