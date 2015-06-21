// The MIT License (MIT)
//
// CoreTweet - A .NET Twitter Library supporting Twitter API 1.1
// Copyright (c) 2013-2015 CoreTweet Development Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using CoreTweet.Core;

#if !NET35
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CoreTweet.Streaming
{
    /// <summary>
    /// Provides the types of the Twitter Streaming API.
    /// </summary>
    public enum StreamingType
    {
        /// <summary>
        /// The user stream.
        /// </summary>
        User,
        /// <summary>
        /// The site stream.
        /// </summary>
        Site,
        /// <summary>
        /// The filter stream.
        /// </summary>
        Filter,
        /// <summary>
        /// The sample stream.
        /// </summary>
        Sample,
        /// <summary>
        /// The firehose stream.
        /// </summary>
        Firehose
    }

    /// <summary>
    /// Represents the wrapper for the Twitter Streaming API.
    /// </summary>
    public class StreamingApi : ApiProviderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTweet.Streaming.StreamingApi"/> class with a specified token.
        /// </summary>
        /// <param name="tokens"></param>
        protected internal StreamingApi(TokensBase tokens) : base(tokens) { }

        internal string GetUrl(StreamingType type)
        {
            var options = Tokens.ConnectionOptions ?? new ConnectionOptions();
            string baseUrl;
            string apiName;
            switch(type)
            {
                case StreamingType.User:
                    baseUrl = options.UserStreamUrl;
                    apiName = "user.json";
                    break;
                case StreamingType.Site:
                    baseUrl = options.SiteStreamUrl;
                    apiName = "site.json";
                    break;
                case StreamingType.Filter:
                    baseUrl = options.StreamUrl;
                    apiName = "statuses/filter.json";
                    break;
                case StreamingType.Sample:
                    baseUrl = options.StreamUrl;
                    apiName = "statuses/sample.json";
                    break;
                case StreamingType.Firehose:
                    baseUrl = options.StreamUrl;
                    apiName = "statuses/firehose.json";
                    break;
                default:
                    throw new ArgumentException("Invalid StreamingType.");
            }
            return InternalUtils.GetUrl(options, baseUrl, true, apiName);
        }

        internal MethodType GetMethodType(StreamingType type)
        {
            return type == StreamingType.Filter ? MethodType.Post : MethodType.Get;
        }

        private static IEnumerable<StreamingMessage> EnumerateMessages(Stream stream)
        {
            using(var reader = new StreamReader(stream))
            {
                foreach(var s in reader.EnumerateLines())
                {
                    if(!string.IsNullOrEmpty(s))
                    {
                        StreamingMessage m;
                        try
                        {
                            m = StreamingMessage.Parse(s);
                        }
                        catch(ParsingException ex)
                        {
                            m = RawJsonMessage.Create(s, ex);
                        }
                        yield return m;
                    }
                }
            }
        }

        #region Obsolete
#if !(PCL || WIN_RT || WP)
        /// <summary>
        /// Starts the Twitter stream.
        /// </summary>
        /// <param name="type">Type of streaming.</param>
        /// <param name="parameters">The parameters of streaming.</param>
        /// <returns>The stream messages.</returns>
        [Obsolete]
        public IEnumerable<StreamingMessage> StartStream(StreamingType type, StreamingParameters parameters = null)
        {
            if(parameters == null)
                parameters = new StreamingParameters();

            return this.AccessStreamingApiImpl(type, parameters.Parameters);
        }
#endif

#if !NET35
        /// <summary>
        /// Starts the Twitter stream asynchronously.
        /// </summary>
        /// <param name="type">Type of streaming.</param>
        /// <param name="parameters">The parameters of streaming.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The stream messages.</returns>
        [Obsolete]
        public Task<IEnumerable<StreamingMessage>> StartStreamAsync(StreamingType type, StreamingParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if(parameters == null)
                parameters = new StreamingParameters();

            return this.Tokens.SendStreamingRequestAsync(this.GetMethodType(type), this.GetUrl(type), parameters.Parameters, cancellationToken)
                .ContinueWith(t =>
                {
                    if(t.IsFaulted)
                        t.Exception.InnerException.Rethrow();

                    return t.Result.GetResponseStreamAsync();
                }, cancellationToken)
                .Unwrap()
                .ContinueWith(t =>
                {
                    if(t.IsFaulted)
                        t.Exception.InnerException.Rethrow();

                    return EnumerateMessages(t.Result);
                }, cancellationToken);
        }
#endif
        #endregion

#if !(PCL || WIN_RT || WP)
        private IEnumerable<StreamingMessage> AccessStreamingApiImpl(StreamingType type, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            return EnumerateMessages(
                this.Tokens.SendStreamingRequest(this.GetMethodType(type), this.GetUrl(type), parameters).GetResponseStream()
            );
        }

        private IEnumerable<StreamingMessage> AccessStreamingApi(StreamingType type, Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApiImpl(type, InternalUtils.ExpressionsToDictionary(parameters));
        }

        private IEnumerable<StreamingMessage> AccessStreamingApi(StreamingType type, IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApiImpl(type, parameters);
        }

        private IEnumerable<StreamingMessage> AccessStreamingApi(StreamingType type, object parameters)
        {
            return this.AccessStreamingApiImpl(type, InternalUtils.ResolveObject(parameters));
        }

        /// <summary>
        /// Streams messages for a single user.
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> User(params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApi(StreamingType.User, parameters);
        }

        /// <summary>
        /// Streams messages for a single user.
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> User(IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApi(StreamingType.User, parameters);
        }

        /// <summary>
        /// Streams messages for a single user.
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> User(object parameters)
        {
            return this.AccessStreamingApi(StreamingType.User, parameters);
        }

        /// <summary>
        /// Streams messages for a single user.
        /// </summary>
        /// <param name="stall_warnings">Specifies whether stall warnings should be delivered.</param>
        /// <param name="with">Specifies whether to return information for just the authenticating user, or include messages from accounts the user follows.</param>
        /// <param name="replies">Specifies whether to return additional &#64;replies.</param>
        /// <param name="track">Includes additional Tweets matching the specified keywords. Phrases of keywords are specified by a comma-separated list.</param>
        /// <param name="locations">Includes additional Tweets falling within the specified bounding boxes.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> User(bool? stall_warnings = null, string with = null, string replies = null, string track = null, IEnumerable<double> locations = null)
        {
            var parameters = new Dictionary<string, object>();
            if (stall_warnings != null) parameters.Add("stall_warnings", stall_warnings);
            if (with != null) parameters.Add("with", with);
            if (replies != null) parameters.Add("replies", replies);
            if (track != null) parameters.Add("track", track);
            if (locations != null) parameters.Add("locations", locations);
            return this.AccessStreamingApiImpl(StreamingType.User, parameters);
        }

        /// <summary>
        /// Streams messages for a set of users.
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (required)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Site(params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApi(StreamingType.Site, parameters);
        }

        /// <summary>
        /// Streams messages for a set of users.
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (required)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Site(IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApi(StreamingType.Site, parameters);
        }

        /// <summary>
        /// Streams messages for a set of users.
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (required)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// <para>- <c>string</c> with (optional)</para>
        /// <para>- <c>string</c> replies (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Site(object parameters)
        {
            return this.AccessStreamingApi(StreamingType.Site, parameters);
        }

        /// <summary>
        /// Streams messages for a set of users.
        /// </summary>
        /// <param name="follow">A comma separated list of user IDs, indicating the users to return statuses for in the stream.</param>
        /// <param name="stall_warnings">Specifies whether stall warnings should be delivered.</param>
        /// <param name="with">Specifies whether to return information for just the users specified in the follow parameter, or include messages from accounts they follow.</param>
        /// <param name="replies">Specifies whether to return additional &#64;replies.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Site(IEnumerable<long> follow, bool? stall_warnings = null, string with = null, string replies = null)
        {
            if (follow == null) throw new ArgumentNullException("follow");
            var parameters = new Dictionary<string, object>();
            parameters.Add("follow", follow);
            if (stall_warnings != null) parameters.Add("stall_warnings", stall_warnings);
            if (with != null) parameters.Add("with", with);
            if (replies != null) parameters.Add("replies", replies);
            return this.AccessStreamingApiImpl(StreamingType.Site, parameters);
        }

        /// <summary>
        /// Returns public statuses that match one or more filter predicates.
        /// <para>Multiple parameters may be specified which allows most clients to use a single connection to the Streaming API.</para>
        /// <para>Note: At least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Filter(params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApi(StreamingType.Filter, parameters);
        }

        /// <summary>
        /// Returns public statuses that match one or more filter predicates.
        /// <para>Multiple parameters may be specified which allows most clients to use a single connection to the Streaming API.</para>
        /// <para>Note: At least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Filter(IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApi(StreamingType.Filter, parameters);
        }

        /// <summary>
        /// Returns public statuses that match one or more filter predicates.
        /// <para>Multiple parameters may be specified which allows most clients to use a single connection to the Streaming API.</para>
        /// <para>Note: At least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>IEnumerable&lt;long&gt;</c> follow (optional)</para>
        /// <para>- <c>string</c> track (optional)</para>
        /// <para>- <c>IEnumerable&lt;double&gt;</c> locations (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Filter(object parameters)
        {
            return this.AccessStreamingApi(StreamingType.Filter, parameters);
        }

        /// <summary>
        /// Returns public statuses that match one or more filter predicates.
        /// <para>Multiple parameters may be specified which allows most clients to use a single connection to the Streaming API.</para>
        /// <para>Note: At least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// </summary>
        /// <param name="follow">A comma separated list of user IDs, indicating the users to return statuses for in the stream.</param>
        /// <param name="track">Keywords to track. Phrases of keywords are specified by a comma-separated list.</param>
        /// <param name="locations">Specifies a set of bounding boxes to track.</param>
        /// <param name="stall_warnings">Specifies whether stall warnings should be delivered.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Filter(IEnumerable<long> follow = null, string track = null, IEnumerable<double> locations = null, bool? stall_warnings = null)
        {
            if (follow == null && track == null && locations == null)
                throw new ArgumentException("At least one predicate parameter (follow, locations, or track) must be specified.");
            var parameters = new Dictionary<string, object>();
            if (follow != null) parameters.Add("follow", follow);
            if (track != null) parameters.Add("track", track);
            if (locations != null) parameters.Add("locations", locations);
            if (stall_warnings != null) parameters.Add("stall_warnings", stall_warnings);
            return this.AccessStreamingApiImpl(StreamingType.Filter, parameters);
        }

        /// <summary>
        /// Returns a small random sample of all public statuses.
        /// <para>The Tweets returned by the default access level are the same, so if two different clients connect to this endpoint, they will see the same Tweets.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Sample(params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApi(StreamingType.Sample, parameters);
        }

        /// <summary>
        /// Returns a small random sample of all public statuses.
        /// <para>The Tweets returned by the default access level are the same, so if two different clients connect to this endpoint, they will see the same Tweets.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Sample(IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApi(StreamingType.Sample, parameters);
        }

        /// <summary>
        /// Returns a small random sample of all public statuses.
        /// <para>The Tweets returned by the default access level are the same, so if two different clients connect to this endpoint, they will see the same Tweets.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Sample(object parameters)
        {
            return this.AccessStreamingApi(StreamingType.Sample, parameters);
        }

        /// <summary>
        /// Returns a small random sample of all public statuses.
        /// <para>The Tweets returned by the default access level are the same, so if two different clients connect to this endpoint, they will see the same Tweets.</para>
        /// </summary>
        /// <param name="stall_warnings">Specifies whether stall warnings should be delivered.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Sample(bool? stall_warnings = null)
        {
            var parameters = new Dictionary<string, object>();
            if (stall_warnings != null) parameters.Add("stall_warnings", stall_warnings);
            return this.AccessStreamingApiImpl(StreamingType.Sample, parameters);
        }

        /// <summary>
        /// Returns all public statuses. Few applications require this level of access.
        /// <para>Creative use of a combination of other resources and various access levels can satisfy nearly every application use case.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>int</c> count (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Firehose(params Expression<Func<string, object>>[] parameters)
        {
            return this.AccessStreamingApi(StreamingType.Firehose, parameters);
        }

        /// <summary>
        /// Returns all public statuses. Few applications require this level of access.
        /// <para>Creative use of a combination of other resources and various access levels can satisfy nearly every application use case.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>int</c> count (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Firehose(IDictionary<string, object> parameters)
        {
            return this.AccessStreamingApi(StreamingType.Firehose, parameters);
        }

        /// <summary>
        /// Returns all public statuses. Few applications require this level of access.
        /// <para>Creative use of a combination of other resources and various access levels can satisfy nearly every application use case.</para>
        /// <para>Available parameters:</para>
        /// <para>- <c>int</c> count (optional)</para>
        /// <para>- <c>string</c> delimited (optional, not affects CoreTweet)</para>
        /// <para>- <c>bool</c> stall_warnings (optional)</para>
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Firehose(object parameters)
        {
            return this.AccessStreamingApi(StreamingType.Firehose, parameters);
        }

        /// <summary>
        /// Returns all public statuses. Few applications require this level of access.
        /// <para>Creative use of a combination of other resources and various access levels can satisfy nearly every application use case.</para>
        /// </summary>
        /// <param name="count">The number of messages to backfill.</param>
        /// <param name="stall_warnings">Specifies whether stall warnings should be delivered.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> Firehose(int? count = null, bool? stall_warnings = null)
        {
            var parameters = new Dictionary<string, object>();
            if (count != null) parameters.Add("count", count);
            if (stall_warnings != null) parameters.Add("stall_warnings", stall_warnings);
            return this.AccessStreamingApiImpl(StreamingType.Firehose, parameters);
        }
#endif
    }

    /// <summary>
    /// Represents the parameters for the Twitter Streaming API.
    /// </summary>
    [Obsolete]
    public class StreamingParameters
    {
        /// <summary>
        /// Gets the raw parameters.
        /// </summary>
        public List<KeyValuePair<string, object>> Parameters { get; private set; }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="CoreTweet.Streaming.StreamingParameters"/> class with a specified option.</para>
        /// <para>Available parameters: </para>
        /// <para>*Note: In filter stream, at least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// <para><c>bool</c> stall_warnings (optional)" : Specifies whether stall warnings should be delivered.</para>
        /// <para><c>string / IEnumerable&lt;long&gt;</c> follow (optional*, required in site stream, ignored in user stream)</para>
        /// <para><c>string / IEnumerable&lt;string&gt;</c> track (optional*)</para>
        /// <para><c>string / IEnumerable&lt;string&gt;</c> location (optional*)</para>
        /// <para><c>string</c> with (optional)</para>
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public StreamingParameters(params Expression<Func<string, object>>[] streamingParameters)
         : this(InternalUtils.ExpressionsToDictionary(streamingParameters)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTweet.Streaming.StreamingParameters"/> class with a specified option.
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public StreamingParameters(IEnumerable<KeyValuePair<string, object>> streamingParameters)
        {
            Parameters = streamingParameters.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreTweet.Streaming.StreamingParameters"/> class with a specified option.
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public static StreamingParameters Create<T>(T streamingParameters)
        {
            return new StreamingParameters(InternalUtils.ResolveObject(streamingParameters));
        }
    }

}

