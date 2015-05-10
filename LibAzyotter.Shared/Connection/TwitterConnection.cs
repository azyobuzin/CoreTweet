using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

#if !(PCL || WIN_RT)
using System.Net.Security;
#endif

namespace LibAzyotter.Connection
{
    public class TwitterConnection : ITwitterConnection
    {
        public TwitterConnection(IRequestBuilder requestBuilder)
        {
            if (requestBuilder == null)
                throw new ArgumentNullException(nameof(requestBuilder));
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
        public IWebProxy Proxy { get; set; }
        public bool UseProxy { get; set; } = true;
        public ProductInfoHeaderValue UserAgent { get; set; } = new ProductInfoHeaderValue("LibAzyotter", "0.0");

#if !(PCL || WIN_RT)
        public int ReadWriteTimeout { get; set; } = 300000;
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
#endif

        public virtual async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken)
        {
            var request = await this.requestBuilder.BuildRequestAsync(method, host, version, relativeUri, parameters, authorizationParameters).ConfigureAwait(false);
            request.Headers.UserAgent.Add(this.UserAgent);
            request.Headers.ExpectContinue = false;

            var isStreaming = !(host == ApiHost.Api || host == ApiHost.Upload);

#if !(PCL || WIN_RT)
            var handler = new WebRequestHandler();
            handler.ReadWriteTimeout = isStreaming ? int.MaxValue : this.ReadWriteTimeout; // -1 is invalid
            handler.ServerCertificateValidationCallback = this.ServerCertificateValidationCallback;
#else
            var handler = new HttpClientHandler();
#endif
            handler.Proxy = this.Proxy;
            handler.UseProxy = this.UseProxy;

            if (!isStreaming && handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var client = new HttpClient(handler);
            client.Timeout = this.Timeout;
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}
