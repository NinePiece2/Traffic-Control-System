using LiveStreamingServerNet.Networking.Contracts;
using LiveStreamingServerNet.Rtmp.Server.Auth;
using LiveStreamingServerNet.Rtmp.Server.Auth.Contracts;
using Traffic_Control_System.Services;

namespace Traffic_Control_System.Handlers
{
    public class AuthorizationHandler : IAuthorizationHandler
    {
        private readonly IValidator _passwordValidator;

        public AuthorizationHandler(IValidator passwordValidator)
        {
            _passwordValidator = passwordValidator;
        }

        public async Task<AuthorizationResult> AuthorizePublishingAsync(
            ISessionInfo client,
            string streamPath,
            IReadOnlyDictionary<string, string> streamArguments,
            string publishingType)
        {
            if (streamArguments.TryGetValue("password", out var password) &&
                await _passwordValidator.Validate(password, streamPath))
                return AuthorizationResult.Authorized();

            return AuthorizationResult.Unauthorized("Invalid");
        }

        public Task<AuthorizationResult> AuthorizeSubscribingAsync(
            ISessionInfo client,
            string streamPath,
            IReadOnlyDictionary<string, string> streamArguments)
        {
            return Task.FromResult(AuthorizationResult.Authorized());
        }
    }
}
