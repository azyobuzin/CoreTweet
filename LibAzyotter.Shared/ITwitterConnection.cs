using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LibAzyotter
{
    public interface ITwitterConnection
    {
        Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, ApiHost host, string version, Uri relativeUri, IEnumerable<KeyValuePair<string, object>> parameters, IEnumerable<KeyValuePair<string, string>> authorizationParameters, CancellationToken cancellationToken);
    }
}
