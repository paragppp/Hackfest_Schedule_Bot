using System.Threading.Tasks;
using System.Net.Http;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// Interface for HttpService
    /// </summary>
    public interface IHttpService
    {
        /// <summary>
        /// Post made with access token
        /// </summary>
        /// <param name="endpoint">HTTP endpoint</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="payload">Data sent to endpoint</param>
        /// <param name="preferTimeZone">Preferred timezone</param>
        /// <returns>Task of <see cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> AuthenticatedPost(string endpoint, string accessToken, object payload, string preferTimeZone);
    }
}
