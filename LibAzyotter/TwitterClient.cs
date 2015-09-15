using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAzyotter.Api;
using LibAzyotter.Connection;
using LibAzyotter.Internal;

namespace LibAzyotter
{
    public class TwitterClient
    {
        public TwitterClient(ITwitterConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            this.connection = connection;
        }

        private ITwitterConnection connection;
        public ITwitterConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                this.connection = value;
            }
        }

        #region Endpoints for Twitter API

        /// <summary>
        /// Gets the wrapper of account.
        /// </summary>
        public Account Account => new Account(this);
        /// <summary>
        /// Gets the wrapper of application.
        /// </summary>
        public Application Application => new Application(this);
        /// <summary>
        /// Gets the wrapper of blocks.
        /// </summary>
        public Blocks Blocks => new Blocks(this);
        /// <summary>
        /// Gets the wrapper of direct_messages.
        /// </summary>
        public DirectMessages DirectMessages => new DirectMessages(this);
        /// <summary>
        /// Gets the wrapper of favorites.
        /// </summary>
        public Favorites Favorites => new Favorites(this);
        /// <summary>
        /// Gets the wrapper of friends.
        /// </summary>
        public Friends Friends => new Friends(this);
        /// <summary>
        /// Gets the wrapper of followers.
        /// </summary>
        public Followers Followers => new Followers(this);
        /// <summary>
        /// Gets the wrapper of friendships.
        /// </summary>
        public Friendships Friendships => new Friendships(this);
        /// <summary>
        /// Gets the wrapper of geo.
        /// </summary>
        public Geo Geo => new Geo(this);
        /// <summary>
        /// Gets the wrapper of help.
        /// </summary>
        public Help Help => new Help(this);
        /// <summary>
        /// Gets the wrapper of lists.
        /// </summary>
        public Lists Lists => new Lists(this);
        /// <summary>
        /// Gets the wrapper of media.
        /// </summary>
        public Media Media => new Media(this);
        /// <summary>
        /// Gets the wrapper of oauth.
        /// </summary>
        public OAuth OAuth => new OAuth(this);
        /// <summary>
        /// Gets the wrapper of oauth2
        /// </summary>
        public OAuth2 OAuth2 => new OAuth2(this);
        /// <summary>
        /// Gets the wrapper of mutes.
        /// </summary>
        public Mutes Mutes => new Mutes(this);
        /// <summary>
        /// Gets the wrapper of search.
        /// </summary>
        public Search Search => new Search(this);
        /// <summary>
        /// Gets the wrapper of saved_searches.
        /// </summary>
        public SavedSearches SavedSearches => new SavedSearches(this);
        /// <summary>
        /// Gets the wrapper of statuses.
        /// </summary>
        public Statuses Statuses => new Statuses(this);
        /// <summary>
        /// Gets the wrapper of trends.
        /// </summary>
        public Trends Trends => new Trends(this);
        /// <summary>
        /// Gets the wrapper of users.
        /// </summary>
        public Users Users => new Users(this);
        /// <summary>
        /// Gets the wrapper of the Streaming API.
        /// </summary>
        public StreamingApi Streaming => new StreamingApi(this);

        #endregion

        public static TwitterClient CreateOAuthClient(string consumerKey, string consumerSecret, string oauthToken = null, string oauthTokenSecret = null)
        {
            return new TwitterClient(new TwitterConnection(new OAuthRequestBuilder(consumerKey, consumerSecret, oauthToken, oauthTokenSecret)));
        }

        public static TwitterClient CreateOAuth2BasicClient(string consumerKey, string consumerSecret)
        {
            return new TwitterClient(new TwitterConnection(new OAuth2BasicRequestBuilder(consumerKey, consumerSecret)));
        }

        public static TwitterClient CreateOAuth2BearerClient(string accessToken)
        {
            return new TwitterClient(new TwitterConnection(new OAuth2BearerRequestBuilder(accessToken)));
        }

        internal const string ApiVersion = "1.1";

        internal async Task<HttpResponseMessage> InternalSendRequestAsync(HttpMethod method, ApiHost host, string version, string relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken)
        {
            var res = await this.connection.SendRequestAsync(method, host, version, new Uri(relativeUri, UriKind.Relative), parameters, authorizationParameters, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                var tex = await TwitterException.CreateAsync(res).ConfigureAwait(false);
                if (tex != null) throw tex;
                res.EnsureSuccessStatusCode();
            }
            return res;
        }

        private Task<HttpResponseMessage> InternalSendRequestAsync(HttpMethod method, string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            return this.InternalSendRequestAsync(method, ApiHost.Api, ApiVersion, url + ".json", parameters, null, cancellationToken);
        }

        internal static async Task<T> ReadResponse<T>(HttpResponseMessage res, Func<string, T> parse)
        {
            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = parse(json);
            var twitterResponse = result as ITwitterResponse;
            if (twitterResponse != null)
            {
                twitterResponse.RateLimit = InternalUtils.ReadRateLimit(res);
                twitterResponse.Json = json;
            }
            return result;
        }

        internal async Task<T> AccessApiAsyncImpl<T>(HttpMethod method, ApiHost host, string version, string relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken, Func<string, T> parse)
        {
            using (var res = await this.InternalSendRequestAsync(method, host, version, relativeUri, parameters, authorizationParameters, cancellationToken).ConfigureAwait(false))
                return await ReadResponse(res, parse).ConfigureAwait(false);
        }

        private async Task<T> AccessApiAsyncImpl<T>(HttpMethod method, string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken, Func<string, T> parse)
        {
            using (var res = await this.InternalSendRequestAsync(method, url, parameters, cancellationToken).ConfigureAwait(false))
                return await ReadResponse(res, parse).ConfigureAwait(false);
        }

        internal Task<T> AccessApiAsync<T>(HttpMethod type, string url, Expression<Func<string, object>>[] parameters, string jsonPath = "")
        {
            return this.AccessApiAsyncImpl<T>(type, url, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None, jsonPath);
        }

        internal Task<T> AccessApiAsync<T>(HttpMethod type, string url, object parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiAsyncImpl<T>(type, url, InternalUtils.ResolveObject(parameters), cancellationToken, jsonPath);
        }

        internal Task<T> AccessApiAsync<T>(HttpMethod type, string url, IDictionary<string, object> parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiAsyncImpl<T>(type, url, parameters, cancellationToken, jsonPath);
        }

        internal Task<T> AccessApiAsyncImpl<T>(HttpMethod type, string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken, string jsonPath)
        {
            return this.AccessApiAsyncImpl(type, url, parameters, cancellationToken, s => CoreBase.Convert<T>(s, jsonPath));
        }

        internal Task<ListedResponse<T>> AccessApiArrayAsync<T>(HttpMethod type, string url, Expression<Func<string, object>>[] parameters, string jsonPath = "")
        {
            return this.AccessApiArrayAsyncImpl<T>(type, url, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None, jsonPath);
        }

        internal Task<ListedResponse<T>> AccessApiArrayAsync<T>(HttpMethod type, string url, object parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiArrayAsyncImpl<T>(type, url, InternalUtils.ResolveObject(parameters), cancellationToken, jsonPath);
        }

        internal Task<ListedResponse<T>> AccessApiArrayAsync<T>(HttpMethod type, string url, IDictionary<string, object> parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiArrayAsyncImpl<T>(type, url, parameters, cancellationToken, jsonPath);
        }

        internal Task<ListedResponse<T>> AccessApiArrayAsyncImpl<T>(HttpMethod type, string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken, string jsonPath)
        {
            return this.AccessApiAsyncImpl(type, url, parameters, cancellationToken, s => new ListedResponse<T>(CoreBase.ConvertArray<T>(s, jsonPath)));
        }

        internal Task<DictionaryResponse<TKey, TValue>> AccessApiDictionaryAsync<TKey, TValue>(HttpMethod type, string url, Expression<Func<string, object>>[] parameters, string jsonPath = "")
        {
            return this.AccessApiDictionaryAsyncImpl<TKey, TValue>(type, url, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None, jsonPath);
        }

        internal Task<DictionaryResponse<TKey, TValue>> AccessApiDictionaryAsync<TKey, TValue>(HttpMethod type, string url, object parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiDictionaryAsyncImpl<TKey, TValue>(type, url, InternalUtils.ResolveObject(parameters), cancellationToken, jsonPath);
        }

        internal Task<DictionaryResponse<TKey, TValue>> AccessApiDictionaryAsync<TKey, TValue>(HttpMethod type, string url, IDictionary<string, object> parameters, CancellationToken cancellationToken, string jsonPath = "")
        {
            return this.AccessApiDictionaryAsyncImpl<TKey, TValue>(type, url, parameters, cancellationToken, jsonPath);
        }

        internal Task<DictionaryResponse<TKey, TValue>> AccessApiDictionaryAsyncImpl<TKey, TValue>(HttpMethod type, string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken, string jsonPath)
        {
            return this.AccessApiAsyncImpl(type, url, parameters, cancellationToken, s => new DictionaryResponse<TKey, TValue>(CoreBase.Convert<Dictionary<TKey, TValue>>(s, jsonPath)));
        }

        internal Task AccessApiNoResponseAsync(string url, Expression<Func<string, object>>[] parameters)
        {
            return this.AccessApiNoResponseAsyncImpl(url, InternalUtils.ExpressionsToDictionary(parameters), CancellationToken.None);
        }

        internal Task AccessApiNoResponseAsync(string url, object parameters, CancellationToken cancellationToken)
        {
            return this.AccessApiNoResponseAsyncImpl(url, InternalUtils.ResolveObject(parameters), cancellationToken);
        }

        internal Task AccessApiNoResponseAsync(string url, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return this.AccessApiNoResponseAsyncImpl(url, parameters, cancellationToken);
        }

        internal async Task AccessApiNoResponseAsyncImpl(string url, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            var res = await this.InternalSendRequestAsync(HttpMethod.Post, url, parameters, cancellationToken).ConfigureAwait(false);
            res.Dispose();
        }
    }
}
