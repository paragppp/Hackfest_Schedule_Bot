using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace SampleAADv2Bot.Services
{
    /// <summary>
    /// HTTP Service
    /// </summary>
    [Serializable]
    public class HttpService : IHttpService
    {
        private readonly ILoggingService loggingService;

        /// <summary>
        /// HTTP Service constructor
        /// </summary>
        /// <param name="loggingService">Instance of <see cref="ILoggingService"/></param>
        public HttpService(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        /// <summary>
        /// Post done with access token
        /// </summary>
        /// <param name="endpoint">HTTP endpoint</param>
        /// <param name="accessToken">Access token</param>
        /// <param name="payload">Data sent to endpoint</param>
        /// <param name="preferTimeZone">Preferred timezone</param>
        /// <returns>Task of <see cref="HttpResponseMessage"/></returns>
        public async Task<HttpResponseMessage> AuthenticatedPost(string endpoint, string accessToken, object payload, string preferTimeZone)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var serializedObject = JsonConvert.SerializeObject(payload);
                    var body = new StringContent(serializedObject);
                    body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    if (!string.IsNullOrEmpty(preferTimeZone))
                    {
                        body.Headers.Add("Prefer", preferTimeZone);
                    }
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    var httpResponseMessage = await httpClient.PostAsync(endpoint, body);
                    httpResponseMessage.EnsureSuccessStatusCode();
                    return httpResponseMessage;
                }
            }
            catch (Exception ex)
            {
                loggingService.Error(ex);
                throw;
            }
        }
    }
}
