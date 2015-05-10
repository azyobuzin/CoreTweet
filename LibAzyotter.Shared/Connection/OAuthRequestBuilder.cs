using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LibAzyotter.Internal;

namespace LibAzyotter.Connection
{
    public class OAuthRequestBuilder : RequestBuilderBase
    {
        public OAuthRequestBuilder(string consumerKey, string consumerSecret, string oauthToken = null, string oauthTokenSecret = null)
        {
            if (string.IsNullOrEmpty(consumerKey))
                throw new ArgumentNullException(nameof(consumerKey));
            if (string.IsNullOrEmpty(consumerSecret))
                throw new ArgumentNullException(nameof(consumerSecret));
            if (string.IsNullOrEmpty(oauthToken) && !string.IsNullOrEmpty(oauthTokenSecret))
                throw new ArgumentNullException(nameof(oauthToken));
            if (string.IsNullOrEmpty(oauthTokenSecret) && !string.IsNullOrEmpty(oauthToken))
                throw new ArgumentNullException(nameof(oauthTokenSecret));

            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
            this.OAuthToken = oauthToken;
            this.OAuthTokenSecret = oauthTokenSecret;
        }

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string OAuthToken { get; set; }
        public string OAuthTokenSecret { get; set; }

        public override async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters)
        {
            var request = await this.BuildBaseRequestAsync(method, host, version, relativeUri, parameters).ConfigureAwait(false);

            string callback = null;
            string verifier = null;
            if (authorizationParameters != null)
            {
                var dic = authorizationParameters.AsDictionary();
                dic.TryGetValue("oauth_callback", out callback);
                dic.TryGetValue("oauth_verifier", out verifier);
            }

            var query = new StringBuilder(request.RequestUri.Query);
            if (query.Length > 0 && query[0] == '?') query.Remove(0, 1);

            var content = request.Content;
            if (content != null && content.Headers.ContentType.MediaType == "application/x-www-form-urlencoded")
            {
                query.Append('&');
                query.Append(await content.ReadAsStringAsync().ConfigureAwait(false));
            }

            var oauthParams = OAuthHelper.OAuthParameters(this.ConsumerKey, this.OAuthToken, callback, verifier);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "OAuth",
                OAuthHelper.CreateHeader(oauthParams, OAuthHelper.Signature(
                    OAuthHelper.SignatureBaseString(request.Method.Method, request.RequestUri.AbsoluteUri, query.ToString(), oauthParams),
                    this.ConsumerSecret, this.OAuthTokenSecret
                ))
            );

            return request;
        }
    }
}
