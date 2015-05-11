using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAzyotter.Connection;
using LibAzyotter.Internal;
using Newtonsoft.Json.Linq;

namespace LibAzyotter.Api
{
    public class OAuth2 : ApiProviderBase
    {
        internal OAuth2(TwitterClient e) : base(e) { }

        private Task<OAuth2TokenResponse> TokenAsyncImpl(IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            return this.Client.AccessApiAsyncImpl(HttpMethod.Post, ApiHost.Api, null, "oauth2/token", parameters, null, cancellationToken, s => CoreBase.Convert<OAuth2TokenResponse>(s));
        }

        public Task<OAuth2TokenResponse> TokenAsync(params Expression<Func<string, object>>[] parameters)
        {
            return this.TokenAsyncImpl(InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);
        }

        public Task<OAuth2TokenResponse> TokenAsync(object parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.TokenAsyncImpl(InternalUtils.ResolveObject(parameters), cancellationToken);
        }

        private Task<string> InvalidateTokenAsyncImpl(IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            return this.Client.AccessApiAsyncImpl(HttpMethod.Post, ApiHost.Api, null, "oauth2/invalidate_token",
                parameters.Select(x => x.Value != null && x.Value is string
                    ? new KeyValuePair<string, object>(x.Key, Uri.UnescapeDataString((string)x.Value)) : x),
                null, cancellationToken, s => (string)JObject.Parse(s)["access_token"]);
        }

        public Task<string> InvalidateTokenAsync(params Expression<Func<string, object>>[] parameters)
        {
            return this.InvalidateTokenAsyncImpl(InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);
        }

        public Task<string> InvalidateTokenAsync(object parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InvalidateTokenAsyncImpl(InternalUtils.ResolveObject(parameters), cancellationToken);
        }
    }
}
