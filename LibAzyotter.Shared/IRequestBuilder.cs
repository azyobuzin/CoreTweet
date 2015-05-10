using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibAzyotter
{
    public interface IRequestBuilder
    {
        Task<HttpRequestMessage> BuildRequest(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters);
    }
}
