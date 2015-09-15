using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LibAzyotter.Connection
{
    public class OAuth2BearerRequestBuilder : RequestBuilderBase
    {
        public OAuth2BearerRequestBuilder(string accessToken)
        {
            this.AccessToken = accessToken;
        }

        private string accessToken;
        public string AccessToken
        {
            get
            {
                return this.accessToken;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException();
                this.accessToken = value;
            }
        }

        public override async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters)
        {
            var request = await this.BuildBaseRequestAsync(method, host, version, relativeUri, parameters).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.accessToken);
            return request;
        }
    }
}
