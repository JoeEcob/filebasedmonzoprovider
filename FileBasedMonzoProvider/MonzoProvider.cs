using Microsoft.Extensions.Configuration;
using Monzo;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileBasedMonzoProvider
{
    public class MonzoProvider
    {
        private readonly MonzoConfig _config = new MonzoConfig();
        private readonly MonzoAuthorizationClient _authClient;

        public MonzoProvider(IConfigurationRoot config)
        {
            config.Bind(_config);
            _authClient = new MonzoAuthorizationClient(_config.MonzoClientId, _config.MonzoClientSecret);
        }

        public async Task<AccessToken> GetAccessToken()
        {
            var token = TryReadExistingToken();

            if (token == null)
            {
                return await SetupNewToken();
            }

            var isValid = await IsValid(token);
            if (!isValid)
            {
                try
                {
                    token = await RefreshToken(token.RefreshToken);
                }
                catch (MonzoException)
                {
                    throw new Exception($"Unable to refresh token. Please delete existing config at: {_config.OAuthPath}");
                }
            }

            return token;
        }

        private AccessToken TryReadExistingToken()
        {
            try
            {
                var json = File.ReadAllText(_config.OAuthPath);
                return JsonConvert.DeserializeObject<AccessToken>(json);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private async Task StoreToken(AccessToken token)
        {
            var file = File.CreateText(_config.OAuthPath);
            await file.WriteLineAsync(JsonConvert.SerializeObject(token));
            file.Dispose();
        }

        private async Task<AccessToken> SetupNewToken()
        {
            var loginPageUrl = _authClient.GetAuthorizeUrl(null, _config.MonzoRedirectUri);

            Console.WriteLine("Visit the following URL to get the magic code:");
            Console.WriteLine(loginPageUrl);
            Console.WriteLine("Enter magic code:");

            var code = Console.ReadLine();

            var accessToken = await _authClient.GetAccessTokenAsync(code, _config.MonzoRedirectUri);

            await StoreToken(accessToken);

            Console.WriteLine($"Successfully created {_config.OAuthPath}!");

            return accessToken;
        }

        private async Task<bool> IsValid(AccessToken token)
        {
            try
            {
                var whoAmI = await new MonzoClient(token.Value).WhoAmIAsync();
                return whoAmI.Authenticated;
            }
            catch (MonzoException)
            {
                return false;
            }
        }

        private async Task<AccessToken> RefreshToken(string refresh)
        {
            var token = await _authClient.RefreshAccessTokenAsync(refresh, CancellationToken.None);

            await StoreToken(token);

            return token;
        }
    }
}
