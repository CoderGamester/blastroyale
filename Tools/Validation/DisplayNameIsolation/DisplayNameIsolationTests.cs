using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using GameLovers.Services;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.Authentication.Generated;
using Unity.Services.Authentication.Shared;
using Player = Unity.Services.Authentication.Generated.Player;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Authentication
{
    public sealed class DisplayNameIsolationTests
    {
        // The SDK implementation/state/name component and game AuthService are genuine imports.
        // Only the actual generated native name API contract has a controlled local response.
        [TestCase(false)]
        [TestCase(true)]
        public async Task LateOldAccountNameResponseCannotMutateReloadedGameProfile(bool samePlayerId)
        {
            var instance = typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            var names = new DelayedNameApi();
            var fixture = new NativeFixture(names);
            AuthenticationService.Instance = fixture.Service;
            try
            {
                await fixture.Service.SignInAnonymouslyAsync();
                var oldId = fixture.Service.PlayerId;
                var broker = new MessageBrokerService();
                var auth = new AuthService(null, null, new ClientDataService(), null, null, broker, null);
                SetLoadedName(auth, "AccountA#0001");
                var staleOperation = auth.SetDisplayName("ChangedA");
                Assert.That(names.CapturedPlayerId, Is.EqualTo(oldId));
                Assert.That(names.Request.Task.IsCompleted, Is.False);

                fixture.Service.SignOut();
                if (!samePlayerId)
                {
                    fixture.Service.ClearSessionToken();
                    fixture.Response.IdToken = Token("synthetic-second-player");
                }
                await fixture.Service.SignInAnonymouslyAsync();
                Assert.That(fixture.Service.PlayerId == oldId, Is.EqualTo(samePlayerId));
                SetLoadedName(auth, "ReloadedB#0002");
                var publications = 0;
                broker.Subscribe<DisplayNameChangedMessage>(_ => publications++);
                names.Request.SetResult(new ApiResponse<Player> { Data = new Player(oldId, "ChangedA#0003") });
                var error = await staleOperation;

                Assert.That(error, Does.StartWith("Error setting player name:"));
                Assert.That(auth.RawLocalPlayerName, Is.EqualTo("ReloadedB#0002"),
                    "An old successful API result must not overwrite a profile loaded after logout/account change.");
                Assert.That(publications, Is.Zero, "No obsolete name notification may cross the native account generation.");
            }
            finally
            {
                instance.SetValue(null, previous);
                UgsAccountSession.Invalidate();
            }
        }

        private static void SetLoadedName(AuthService auth, string name) => typeof(AuthService)
            .GetMethod("OnUnityAuthSetPlayerName", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(auth, new object[] { name });

        private static string Token(string id)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"" + id + "\",\"iat\":" + now + ",\"exp\":" + (now + 3600) + "}"))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return "e30." + payload + ".synthetic";
        }

        private sealed class DelayedNameApi : IPlayerNamesApi
        {
            public IApiConfiguration Configuration { get; } = new ApiConfiguration();
            public TaskCompletionSource<ApiResponse<Player>> Request { get; } = new();
            public string CapturedPlayerId { get; private set; }
            public string GetBasePath() => "synthetic-local";
            public Task<ApiResponse<Player>> GetNameAsync(string playerId, bool? autoGenerate = null,
                bool? showMetadata = null, CancellationToken cancellationToken = default) =>
                Task.FromResult(new ApiResponse<Player> { Data = new Player(playerId, "NativePlayer#0001") });
            public Task<ApiResponse<Player>> UpdateNameAsync(string playerId, UpdateNameRequest request, CancellationToken cancellationToken = default)
            {
                CapturedPlayerId = playerId;
                return Request.Task;
            }
        }
    }
}
