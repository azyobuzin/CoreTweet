using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LibAzyotter
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

        public TimeSpan Timeout { get; set; }

        public virtual async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken)
        {
            var request = await this.requestBuilder.BuildRequest(method, host, version, relativeUri, parameters, authorizationParameters).ConfigureAwait(false);

#if NET45
            var handler = new WebRequestHandler();
            //TODO: set to .net45 only properties
#else
            var handler = new HttpClientHandler();
#endif

            if (handler.SupportsAutomaticDecompression && (host == ApiHost.Api || host == ApiHost.Upload))
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            var client = new HttpClient(handler);
            client.Timeout = this.Timeout;
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }
}
