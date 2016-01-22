using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

#if WIN_RT
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace LibAzyotter.Connection
{
    public abstract class RequestBuilderBase : IRequestBuilder
    {
        public abstract Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters);

        protected virtual Uri BuildRequestUri(ApiHost host, string version, Uri relativeUri)
        {
            string baseUri;
            switch (host)
            {
                case ApiHost.Api:
                    baseUri = "https://api.twitter.com/";
                    break;
                case ApiHost.Upload:
                    baseUri = "https://upload.twitter.com/";
                    break;
                case ApiHost.UserStream:
                    baseUri = "https://userstream.twitter.com/";
                    break;
                case ApiHost.SiteStream:
                    baseUri = "https://sitestream.twitter.com/";
                    break;
                case ApiHost.Stream:
                    baseUri = "https://stream.twitter.com/";
                    break;
                default:
                    throw new ArgumentException("Invalid host.");
            }

            if (!string.IsNullOrEmpty(version))
                baseUri += version + "/";

            return new Uri(new Uri(baseUri), relativeUri);
        }

        private static object FormatObject(object x)
        {
            if (x is string) return x;
            if (x is int)
                return ((int)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is long)
                return ((long)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is double)
            {
                var s = ((double)x).ToString("F99", CultureInfo.InvariantCulture).TrimEnd('0');
                if (s[s.Length - 1] == '.') s += '0';
                return s;
            }
            if (x is float)
            {
                var s = ((float)x).ToString("F99", CultureInfo.InvariantCulture).TrimEnd('0');
                if (s[s.Length - 1] == '.') s += '0';
                return s;
            }
            if (x is uint)
                return ((uint)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is ulong)
                return ((ulong)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is short)
                return ((short)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is ushort)
                return ((ushort)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is decimal)
                return ((decimal)x).ToString(CultureInfo.InvariantCulture);
            if (x is byte)
                return ((byte)x).ToString("D", CultureInfo.InvariantCulture);
            if (x is sbyte)
                return ((sbyte)x).ToString("D", CultureInfo.InvariantCulture);

            if (x is UploadMediaType)
                return Api.Media.GetMediaTypeString((UploadMediaType)x);

            if (x is IEnumerable<string>
                || x is IEnumerable<int>
                || x is IEnumerable<long>
                || x is IEnumerable<double>
                || x is IEnumerable<float>
                || x is IEnumerable<uint>
                || x is IEnumerable<ulong>
                || x is IEnumerable<short>
                || x is IEnumerable<ushort>
                || x is IEnumerable<decimal>)
            {
                return (x as System.Collections.IEnumerable).Cast<object>().Select(FormatObject).JoinToString(",");
            }

            var type = x.GetType().GetTypeInfo();
            if (type.Name == "FSharpOption`1")
            {
                return FormatObject(type.GetDeclaredProperty("Value").GetValue(x));
            }

            return x;
        }

        protected virtual IEnumerable<KeyValuePair<string, object>> FormatParameters(IEnumerable<KeyValuePair<string, object>> source)
        {
            if (source == null) return new KeyValuePair<string, object>[0];

            return source.Where(kvp => kvp.Key != null && kvp.Value != null)
                .Select(kvp => new KeyValuePair<string, object>(kvp.Key, FormatObject(kvp.Value)));
        }

        protected virtual
#if WIN_RT
        async
#endif
        Task<HttpContent> CreateHttpContentAsync(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var prmArray = parameters.ToArray();
            if (prmArray.Select(x => x.Value).Any(x => x is Stream || x is IEnumerable<byte>
#if !(PCL || WIN_RT || DOTNET5_2)
                || x is FileInfo
#endif
#if WIN_RT
                || x is IInputStream || x is IBuffer || x is IInputStreamReference || x is IStorageItem
#endif
            ))
            {
                var content = new MultipartFormDataContent();
                foreach (var x in prmArray)
                {
                    string fileName = null;

                    var valueStream = x.Value as Stream;
                    var valueBytes = x.Value as IEnumerable<byte>;

#if !(PCL || WIN_RT || DOTNET5_2)
                    var valueFileInfo = x.Value as FileInfo;
                    if (valueFileInfo != null)
                    {
                        valueStream = valueFileInfo.OpenRead();
                        fileName = valueFileInfo.Name;
                    }
#endif

#if WIN_RT
                    var valueInputStream = x.Value as IInputStream;
                    var valueBuffer = x.Value as IBuffer;
                    var valueInputStreamReference = x.Value as IInputStreamReference;
                    var valueStorageItem = x.Value as IStorageItem;

                    if (valueInputStreamReference != null)
                        valueInputStream = await valueInputStreamReference.OpenSequentialReadAsync();

                    if (valueInputStream != null)
                        valueStream = valueInputStream.AsStreamForRead();
                    if (valueBuffer != null)
                        valueStream = valueBuffer.AsStream();

                    if (valueStorageItem != null)
                        fileName = valueStorageItem.Name;
#endif

                    if (valueStream != null)
                        content.Add(new StreamContent(valueStream), x.Key, fileName ?? "file");
                    else if (valueBytes != null)
                    {
                        var valueByteArray = valueBytes as byte[] ?? valueBytes.ToArray();
                        content.Add(new ByteArrayContent(valueByteArray), x.Key, fileName ?? "file");
                    }
                    else
                        content.Add(new StringContent(x.Value.ToString()), x.Key);
                }
#if WIN_RT
                return content;
#else
                return Task.FromResult(content as HttpContent);
#endif
            }
            else
            {
                HttpContent content = new FormUrlEncodedContent(prmArray.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())));
#if WIN_RT
                return content;
#else
                return Task.FromResult(content);
#endif
            }
        }

        protected virtual async Task<HttpRequestMessage> BuildBaseRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            parameters = this.FormatParameters(parameters);
            var requestUri = this.BuildRequestUri(host, version, relativeUri);

            HttpContent content;
            if (method == HttpMethod.Get || method == HttpMethod.Head)
            {
                var ub = new UriBuilder(requestUri);
                var query = ub.Query.Length > 1 ? (ub.Query.Substring(1) + "&") : "";
                query += parameters
                    .Select(x => string.Format("{0}={1}", Uri.EscapeDataString(x.Key), Uri.EscapeDataString(x.Value.ToString())))
                    .JoinToString("&");
                ub.Query = query;
                requestUri = ub.Uri;
                content = null;
            }
            else
            {
                content = await this.CreateHttpContentAsync(parameters).ConfigureAwait(false);
            }

            return new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };
        }
    }
}
