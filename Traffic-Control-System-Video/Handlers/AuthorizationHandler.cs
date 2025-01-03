using LiveStreamingServerNet.Networking.Contracts;
using LiveStreamingServerNet.Rtmp.Server.Auth;
using LiveStreamingServerNet.Rtmp.Server.Auth.Contracts;
using Traffic_Control_System.Data;

namespace Traffic_Control_System_Video.Handlers
{
    public class AuthorizationHandler : IAuthorizationHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public AuthorizationHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<AuthorizationResult> AuthorizePublishingAsync(
            ISessionInfo client,
            string streamPath,
            IReadOnlyDictionary<string, string> streamArguments,
            string publishingType)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (streamArguments.TryGetValue("key", out var key) && await Validate(context, key, streamPath))
                    return AuthorizationResult.Authorized();

                return AuthorizationResult.Unauthorized("Invalid");
            }
        }

        public Task<AuthorizationResult> AuthorizeSubscribingAsync(
            ISessionInfo client,
            string streamPath,
            IReadOnlyDictionary<string, string> streamArguments)
        {
            return Task.FromResult(AuthorizationResult.Authorized());
        }

        private async Task<bool> Validate(ApplicationDbContext context, string key, string streamPath)
        {
            string[] streamPathSplit;
            bool streamPathCorrect = false, streamKeyCorrect = false;

            try
            {
                streamPathSplit = streamPath.Split("/");

                var streamDeviceList = context.StreamClients
                    .Where(s => s.DeviceStreamID == streamPathSplit.ElementAtOrDefault(2))
                    .ToList();

                streamPathCorrect = streamDeviceList.Any();

                if (streamPathCorrect)
                {
                    var streamDevice = streamDeviceList.FirstOrDefault();
                    streamKeyCorrect = streamDevice?.DeviceStreamKEY == key;
                }
            }
            catch (Exception)
            {
                streamPathCorrect = false;
                streamKeyCorrect = false;
            }

            return streamPathCorrect && streamKeyCorrect;
        }
    }

}
