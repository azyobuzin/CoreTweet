// The MIT License (MIT)
//
// CoreTweet - A .NET Twitter Library supporting Twitter API 1.1
// Copyright (c) 2014 lambdalice
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
using System.Net.Http;
using LibAzyotter.Connection;
using LibAzyotter.Internal;

#if !NET35
using System.Threading;
using System.Threading.Tasks;
#endif

namespace LibAzyotter
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
    /// Represents the parameters for the Twitter Streaming API.
    /// </summary>
    public class StreamingParameters
    {
        /// <summary>
        /// Gets the raw parameters.
        /// </summary>
        public List<KeyValuePair<string, object>> Parameters { get; private set; }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="StreamingParameters"/> class with a specified option.</para>
        /// <para>Available parameters: </para>
        /// <para>*Note: In filter stream, at least one predicate parameter (follow, locations, or track) must be specified.</para>
        /// <para><c>bool</c> stall_warnings (optional)"/> : Specifies whether stall warnings should be delivered.</para>
        /// <para><c>string / IEnumerable&lt;long&gt;</c> follow (optional*, required in site stream, ignored in user stream)</para>
        /// <para><c>string / IEnumerable&lt;string&gt;</c> track (optional*)</para>
        /// <para><c>string / IEnumerable&lt;string&gt;</c> location (optional*)</para>
        /// <para><c>string</c> with (optional)</para>
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public StreamingParameters(params Expression<Func<string, object>>[] streamingParameters)
         : this(InternalUtils.ExpressionsToDictionary(streamingParameters))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingParameters"/> class with a specified option.
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public StreamingParameters(IEnumerable<KeyValuePair<string, object>> streamingParameters)
        {
            Parameters = streamingParameters.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingParameters"/> class with a specified option.
        /// </summary>
        /// <param name="streamingParameters">The streaming parameters.</param>
        public static StreamingParameters Create<T>(T streamingParameters)
        {
            return new StreamingParameters(InternalUtils.ResolveObject(streamingParameters));
        }
    }
}

namespace LibAzyotter.Api
{
    /// <summary>
    /// Represents the wrapper for the Twitter Streaming API.
    /// </summary>
    public class StreamingApi : ApiProviderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingApi"/> class with a specified token.
        /// </summary>
        /// <param name="tokens"></param>
        protected internal StreamingApi(TwitterClient tokens) : base(tokens) { }

        internal Tuple<ApiHost, string> GetUrl(StreamingType type)
        {
            switch(type)
            {
                case StreamingType.User:
                    return Tuple.Create(ApiHost.UserStream, "user.json");
                case StreamingType.Site:
                    return Tuple.Create(ApiHost.SiteStream, "site.json");
                case StreamingType.Filter:
                    return Tuple.Create(ApiHost.Stream, "statuses/filter.json");
                case StreamingType.Sample:
                    return Tuple.Create(ApiHost.Stream, "statuses/sample.json");
                case StreamingType.Firehose:
                    return Tuple.Create(ApiHost.Stream, "statuses/firehose.json");
                default:
                    throw new ArgumentException("Invalid StreamingType.");
            }
        }

        internal HttpMethod GetMethodType(StreamingType type)
        {
            return type == StreamingType.Filter ? HttpMethod.Post : HttpMethod.Get;
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

#if false
        /// <summary>
        /// Starts the Twitter stream.
        /// </summary>
        /// <param name="type">Type of streaming.</param>
        /// <param name="parameters">The parameters of streaming.</param>
        /// <returns>The stream messages.</returns>
        public IEnumerable<StreamingMessage> StartStream(StreamingType type, StreamingParameters parameters = null)
        {
            if(parameters == null)
                parameters = new StreamingParameters();

            return EnumerateMessages(
                this.Tokens.SendStreamingRequest(this.GetMethodType(type), this.GetUrl(type), parameters.Parameters).GetResponseStream()
            );
        }
#endif

#if !NET35
        internal async Task<Stream> GetStreamAsync(StreamingType type, StreamingParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                parameters = new StreamingParameters();

            var t = this.GetUrl(type);
            return await
                (await this.Client.InternalSendRequestAsync(this.GetMethodType(type), t.Item1, TwitterClient.ApiVersion, new Uri(t.Item2, UriKind.Relative), parameters.Parameters, null, cancellationToken).ConfigureAwait(false))
                .Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the Twitter stream asynchronously.
        /// </summary>
        /// <param name="type">Type of streaming.</param>
        /// <param name="parameters">The parameters of streaming.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The stream messages.</returns>
        public async Task<IEnumerable<StreamingMessage>> StartStreamAsync(StreamingType type, StreamingParameters parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if(parameters == null)
                parameters = new StreamingParameters();

            return EnumerateMessages(await this.GetStreamAsync(type, parameters, cancellationToken).ConfigureAwait(false));
        }
#endif
    }
}

