using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Security.Cryptography;
using System.Text;

namespace UserService.Services
{
    public class CognitoService : ICognitoService
    {
        private readonly string _userPoolId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly RegionEndpoint _region;

        public CognitoService(IConfiguration configuration)
        {
            _userPoolId = configuration["Cognito__UserPoolId"] ?? string.Empty;
            _clientId = configuration["Cognito__ClientId"] ?? string.Empty;
            _clientSecret = configuration["Cognito__ClientSecret"] ?? string.Empty;
            var regionName = configuration["Cognito__Region"] ?? string.Empty;

            if (
                string.IsNullOrWhiteSpace(_userPoolId)
                || string.IsNullOrWhiteSpace(_clientId)
                || string.IsNullOrWhiteSpace(_clientSecret)
                || string.IsNullOrWhiteSpace(regionName)
            )
            {
                throw new InvalidOperationException("Cognito configuration is incomplete. Set Cognito__UserPoolId, Cognito__ClientId, Cognito__ClientSecret, and Cognito__Region.");
            }

            _region = RegionEndpoint.GetBySystemName(regionName);
        }

        public async Task<string> RegisterAsync(string email, string password, string name)
        {
            try
            {
                using var client = new AmazonCognitoIdentityProviderClient(_region);

                var response = await client.SignUpAsync(new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = email.Trim().ToLowerInvariant(),
                    Password = password,
                    SecretHash = ComputeSecretHash(email.Trim().ToLowerInvariant()),
                    UserAttributes = new List<AttributeType>
                    {
                        new() { Name = "email", Value = email.Trim().ToLowerInvariant() },
                        new() { Name = "name", Value = name.Trim() }
                    },
                    ClientMetadata = new Dictionary<string, string>
                    {
                        { "userPoolId", _userPoolId }
                    }
                });

                if (string.IsNullOrWhiteSpace(response.UserSub))
                {
                    throw new InvalidOperationException("Cognito signup did not return a valid user sub.");
                }

                return response.UserSub;
            }
            catch (UsernameExistsException)
            {
                throw new ArgumentException("A user with this email already exists.");
            }
            catch (InvalidPasswordException ex)
            {
                throw new ArgumentException($"Password does not meet Cognito policy requirements: {ex.Message}");
            }
            catch (InvalidParameterException ex)
            {
                throw new ArgumentException($"Invalid registration input: {ex.Message}");
            }
        }

        public async Task<string> AuthenticateAsync(string email, string password)
        {
            try
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();
                using var client = new AmazonCognitoIdentityProviderClient(_region);

                var response = await client.InitiateAuthAsync(new InitiateAuthRequest
                {
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    ClientId = _clientId,
                    AuthParameters = new Dictionary<string, string>
                    {
                        { "USERNAME", normalizedEmail },
                        { "PASSWORD", password },
                        { "SECRET_HASH", ComputeSecretHash(normalizedEmail) }
                    }
                });

                var accessToken = response.AuthenticationResult?.AccessToken;
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new InvalidOperationException("Cognito did not return an access token.");
                }

                return accessToken;
            }
            catch (NotAuthorizedException ex)
            {
                throw new ArgumentException($"Authentication failed: {ex.Message}");
            }
            catch (UserNotFoundException)
            {
                throw new ArgumentException("Authentication failed: user not found.");
            }
            catch (UserNotConfirmedException)
            {
                throw new ArgumentException("Authentication failed: user is not confirmed.");
            }
            catch (InvalidParameterException ex)
            {
                throw new ArgumentException($"Authentication failed: {ex.Message}");
            }
        }

        private string ComputeSecretHash(string username)
        {
            var message = username + _clientId;
            var keyBytes = Encoding.UTF8.GetBytes(_clientSecret);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(messageBytes);
            return Convert.ToBase64String(hash);
        }
    }
}
