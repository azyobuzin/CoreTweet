using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LibAzyotter.Connection
{
    public class OAuth2BasicRequestBuilder : RequestBuilderBase
    {
        public OAuth2BasicRequestBuilder(string consumerKey, string consumerSecret)
        {
            if (string.IsNullOrEmpty(consumerKey))
                throw new ArgumentNullException(nameof(consumerKey));
            if (string.IsNullOrEmpty(consumerSecret))
                throw new ArgumentNullException(nameof(consumerSecret));

            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
        }

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }

        public override async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters)
        {
            var request = await this.BuildBaseRequestAsync(method, host, version, relativeUri, parameters).ConfigureAwait(false);
            //TODO: URL encode with RFC 1738
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", this.ConsumerKey, this.ConsumerSecret)))
            );
            return request;
        }
    }
}
