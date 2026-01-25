using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lastfm.Scrobbler.Core.Interfaces
{
    public interface ICoreHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
        Task<HttpResponseMessage> PostAsync(string url, FormUrlEncodedContent content, CancellationToken cancellationToken);
    }
}
