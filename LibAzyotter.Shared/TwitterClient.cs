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
        public Account Account { get { return new Account(this); } }
        /// <summary>
        /// Gets the wrapper of application.
        /// </summary>
        public Application Application { get { return new Application(this); } }
        /// <summary>
        /// Gets the wrapper of blocks.
        /// </summary>
        public Blocks Blocks { get { return new Blocks(this); } }
        /// <summary>
        /// Gets the wrapper of direct_messages.
        /// </summary>
        public DirectMessages DirectMessages { get { return new DirectMessages(this); } }
        /// <summary>
        /// Gets the wrapper of favorites.
        /// </summary>
        public Favorites Favorites { get { return new Favorites(this); } }
        /// <summary>
        /// Gets the wrapper of friends.
        /// </summary>
        public Friends Friends { get { return new Friends(this); } }
        /// <summary>
        /// Gets the wrapper of followers.
        /// </summary>
        public Followers Followers { get { return new Followers(this); } }
        /// <summary>
        /// Gets the wrapper of friendships.
        /// </summary>
        public Friendships Friendships { get { return new Friendships(this); } }
        /// <summary>
        /// Gets the wrapper of geo.
        /// </summary>
        public Geo Geo { get { return new Geo(this); } }
        /// <summary>
        /// Gets the wrapper of help.
        /// </summary>
        public Help Help { get { return new Help(this); } }
        /// <summary>
        /// Gets the wrapper of lists.
        /// </summary>
        public Lists Lists { get { return new Lists(this); } }
        /// <summary>
        /// Gets the wrapper of media.
        /// </summary>
        public Media Media { get { return new Media(this); } }
        /// <summary>
        /// Gets the wrapper of mutes.
        /// </summary>
        public Mutes Mutes { get { return new Mutes(this); } }
        /// <summary>
        /// Gets the wrapper of search.
        /// </summary>
        public Search Search { get { return new Search(this); } }
        /// <summary>
        /// Gets the wrapper of saved_searches.
        /// </summary>
        public SavedSearches SavedSearches { get { return new SavedSearches(this); } }
        /// <summary>
        /// Gets the wrapper of statuses.
        /// </summary>
        public Statuses Statuses { get { return new Statuses(this); } }
        /// <summary>
        /// Gets the wrapper of trends.
        /// </summary>
        public Trends Trends { get { return new Trends(this); } }
        /// <summary>
        /// Gets the wrapper of users.
        /// </summary>
        public Users Users { get { return new Users(this); } }
        /// <summary>
        /// Gets the wrapper of the Streaming API.
        /// </summary>
        public StreamingApi Streaming { get { return new StreamingApi(this); } }

        #endregion

        public static TwitterClient CreateOAuthClient(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            if (string.IsNullOrEmpty(consumerKey)) throw new ArgumentNullException(nameof(consumerKey));
            if (string.IsNullOrEmpty(consumerSecret)) throw new ArgumentNullException(nameof(consumerSecret));
            if (string.IsNullOrEmpty(accessToken)) throw new ArgumentNullException(nameof(accessToken));
            if (string.IsNullOrEmpty(accessTokenSecret)) throw new ArgumentNullException(nameof(accessTokenSecret));

            return new TwitterClient(new TwitterConnection(new OAuthRequestBuilder(consumerKey, consumerSecret, accessToken, accessTokenSecret)));
        }

        internal const string ApiVersion = "1.1";

        internal async Task<HttpResponseMessage> InternalSendRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken)
        {
            var res = await this.connection.SendRequestAsync(method, host, version, relativeUri, parameters, authorizationParameters, cancellationToken).ConfigureAwait(false);
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
            return this.InternalSendRequestAsync(method, ApiHost.Api, ApiVersion, new Uri(url + ".json", UriKind.Relative), parameters, null, cancellationToken);
        }

        private static async Task<T> ReadResponse<T>(HttpResponseMessage res, Func<string, T> parse)
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

        internal async Task<T> AccessApiAsyncImpl<T>(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken, Func<string, T> parse)
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

        internal Task<T> AccessApiAsync<T, TV>(HttpMethod type, string url, TV parameters, CancellationToken cancellationToken, string jsonPath = "")
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

        internal Task<ListedResponse<T>> AccessApiArrayAsync<T, TV>(HttpMethod type, string url, TV parameters, CancellationToken cancellationToken, string jsonPath = "")
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

        internal Task<DictionaryResponse<TKey, TValue>> AccessApiDictionaryAsync<TKey, TValue, TV>(HttpMethod type, string url, TV parameters, CancellationToken cancellationToken, string jsonPath = "")
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

        internal Task AccessApiNoResponseAsync<TV>(string url, TV parameters, CancellationToken cancellationToken)
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
