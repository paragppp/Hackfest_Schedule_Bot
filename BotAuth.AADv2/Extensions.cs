using BotAuth.Models;
using Microsoft.Identity.Client;

namespace BotAuth.AADv2
{
    public static class Extensions
    {
        public static AuthResult FromMSALAuthenticationResult(this AuthenticationResult authResult, TokenCache tokenCache)
        {
            var result = new AuthResult
            {
                AccessToken = authResult.Token,
                UserName = $"{authResult.User.Name}",
                UserUniqueId = authResult.User.UniqueId,
                ExpiresOnUtcTicks = authResult.ExpiresOn.UtcTicks,
                TokenCache = tokenCache.Serialize()
            };

            return result;
        }
    }
}
