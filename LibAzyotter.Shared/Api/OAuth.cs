using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAzyotter.Connection;
using LibAzyotter.Internal;

namespace LibAzyotter.Api
{
    public class OAuth : ApiProviderBase
    {
        internal OAuth(TwitterClient e) : base(e) { }

        private async Task<Dictionary<string, string>> AccessOAuthApiAsync(string apiName, string callback, string verifier, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            using (var res = await this.Client.InternalSendRequestAsync(HttpMethod.Post, ApiHost.Api, null, "oauth/" + apiName, parameters, new Dictionary<string, string>()
            {
                ["oauth_callback"] = callback,
                ["oauth_verifier"] = verifier
            }, cancellationToken).ConfigureAwait(false))
            {
                return (await res.Content.ReadAsStringAsync().ConfigureAwait(false))
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split('='))
                    .ToDictionary(x => x[0], x => x[1]);
            }
        }

        private async Task<RequestTokenResponse> RequestTokenAsyncImpl(string oauthCallback, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            var dic = await this.AccessOAuthApiAsync("request_token", oauthCallback, null, parameters, cancellationToken).ConfigureAwait(false);
            return new RequestTokenResponse()
            {
                OAuthToken = dic["oauth_token"],
                OAuthTokenSecret = dic["oauth_token_secret"],
                OAuthCallbackConfirmed = bool.Parse(dic["oauth_callback_confirmed"])
            };
        }

        public Task<RequestTokenResponse> RequestTokenAsync(string oauthCallback, params Expression<Func<string, object>>[] parameters)
        {
            return this.RequestTokenAsyncImpl(oauthCallback, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);
        }

        public Task<RequestTokenResponse> RequestTokenAsync(string oauthCallback, object parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.RequestTokenAsyncImpl(oauthCallback, InternalUtils.ResolveObject(parameters), cancellationToken);
        }

        private async Task<AccessTokenResponse> AccessTokenAsyncImpl(string oauthVerifier, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            var dic = await this.AccessOAuthApiAsync("access_token", null, oauthVerifier, parameters, cancellationToken).ConfigureAwait(false);
            return new AccessTokenResponse()
            {
                OAuthToken = dic["oauth_token"],
                OAuthTokenSecret = dic["oauth_token_secret"],
                UserId = long.Parse(dic["user_id"], CultureInfo.InvariantCulture),
                ScreenName = dic["screen_name"]
            };
        }

        public Task<AccessTokenResponse> AccessTokenAsync(string oauthVerifier, params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessTokenAsyncImpl(oauthVerifier, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);
        }

        public Task<AccessTokenResponse> AccessTokenAsync(string oauthVerifier, object parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.AccessTokenAsyncImpl(oauthVerifier, InternalUtils.ResolveObject(parameters), cancellationToken);
        }
    }
}
