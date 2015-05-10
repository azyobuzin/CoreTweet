using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibAzyotter.Connection
{
    public interface IRequestBuilder
    {
        Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters);
    }
}
