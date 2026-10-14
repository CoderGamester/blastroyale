using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Validation.Collaborators;
using NUnit.Framework;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using Unity.Services.Authentication.Generated;
using Unity.Services.Authentication.Shared;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Scheduler.Internal;
using Player = Unity.Services.Authentication.Generated.Player;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Authentication
{
    // Uses the native SDK test-friend assembly identity; all SDK/state/provider types are imported, not redefined.
    public sealed class UgsAuthenticationLifecycleTests
    {
        private object _previous;
        private NativeFixture _fixture;
        [SetUp] public void SetUp()
        {
            _previous = typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _fixture = new NativeFixture();
            AuthenticationService.Instance = _fixture.Service;
        }
        [TearDown] public void TearDown()
        {
            typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _previous);
            UgsAccountSession.Invalidate();
        }
        private static object Provider() => Activator.CreateInstance(typeof(UgsAccountSession).Assembly.GetType(
            "FirstLight.Game.Services.Authentication.UnityCloudLoginProvider", throwOnError: true), nonPublic: true);
        private static async Task<LoginResult> Login(string method)
        {
            var provider = Provider();
            var operation = (UniTask<LoginResult>)provider.GetType().GetMethod(method).Invoke(provider, null);
            return await operation;
        }

        [TestCase("LoginWithDevice")]
        [TestCase("LoginWithNewGuestAccount")]
        public async Task ExpiredCachedSessionReauthenticatesThroughActualProvider(string method)
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            var loaded = UgsAccountSession.Capture();
            _fixture.Service.Expire();
            Assert.That(_fixture.Service.IsSignedIn && _fixture.Service.IsExpired && !_fixture.Service.IsAuthorized, Is.True);
            var before = _fixture.NetworkCalls;
            var result = await Login(method);
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(before + 1), "Expired recovery must execute the native cached-session request.");
            Assert.That(_fixture.Service.IsAuthorized, Is.True);
            Assert.That(result.PlayFabId, Is.EqualTo(_fixture.Service.PlayerId));
            Assert.That(loaded.IsCurrent, Is.False, "State loaded under the previous session generation must be reloaded.");
            Assert.That(UgsAccountSession.Capture().IsCurrent, Is.True);
        }

        [Test] public async Task NativeScheduledRefreshPreservesGenerationAndAvoidsRedundantProviderSignin()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            var before = _fixture.NetworkCalls;
            var generation = UgsAccountSession.Capture();
            await Login("LoginWithDevice");
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(before));
            _fixture.Service.ChangeState(AuthenticationState.Refreshing);
            Assert.That(_fixture.Service.IsAuthorized, Is.True);
            await Login("LoginWithDevice");
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(before), "A refresh must not initiate a second anonymous sign-in.");
            _fixture.Scheduler.Execute(_fixture.Service.RefreshActionId.Value);
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(before + 1), "The actual SDK scheduled refresh invokes its cached-token request.");
            Assert.That(_fixture.Service.IsAuthorized && generation.IsCurrent, Is.True);
        }

        [Test] public async Task SignedOutCachedSessionRestoresSameIdAndInvalidatesOldGeneration()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            var old = UgsAccountSession.Capture();
            var id = _fixture.Service.PlayerId;
            _fixture.Service.SignOut();
            Assert.That(!old.IsCurrent && _fixture.Service.SessionTokenExists, Is.True);
            var before = _fixture.NetworkCalls;
            await Login("LoginWithDevice");
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(before + 1));
            Assert.That(_fixture.Service.PlayerId, Is.EqualTo(id));
            Assert.That(!old.IsCurrent && UgsAccountSession.Capture().IsCurrent, Is.True);
        }

        [Test] public async Task FreshGuestCreatesExactlyOneNativeAnonymousSession()
        {
            _fixture.Service.ClearSessionToken();
            Assert.That(!_fixture.Service.SessionTokenExists && !_fixture.Service.IsSignedIn, Is.True);
            var result = await Login("LoginWithNewGuestAccount");
            Assert.That(_fixture.NetworkCalls, Is.EqualTo(1));
            Assert.That(_fixture.Service.IsAuthorized && result.NewlyCreated, Is.True);
        }
        [Test] public async Task ExpiredNativeSessionRejectsNewCloudSnapshot()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            var authorized = UgsAccountSession.Capture();
            _fixture.Scheduler.Execute(_fixture.Service.ExpirationActionId.Value);
            Assert.That(_fixture.Service.IsSignedIn && !_fixture.Service.IsAuthorized, Is.True);
            Assert.That(authorized.IsCurrent, Is.False);
            Assert.Throws<InvalidOperationException>(() => UgsAccountSession.Capture());
        }

        private static async Task Logout(bool clearRecoverableCredentials)
        {
            var provider = Provider();
            await (UniTask)provider.GetType().GetMethod("Logout").Invoke(provider, new object[] { clearRecoverableCredentials });
        }

        private void MarkNativeUnityLinkedIdentity()
        {
            _fixture.Service.PlayerInfo = new PlayerInfo(_fixture.Service.PlayerId, null,
                new List<ExternalIdentity> { new ExternalIdentity { ProviderId = "unity", ExternalId = "synthetic-hosted-identity" } }, null, null);
            Assert.That(_fixture.Service.PlayerInfo.GetUnityId(), Is.EqualTo("synthetic-hosted-identity"));
        }

        [Test] public async Task ExplicitHostedLogoutClearsCachedSessionAndAutomaticLoginEligibility()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            MarkNativeUnityLinkedIdentity();
            await Logout(true);
            Assert.That(_fixture.Service.IsSignedIn || _fixture.Service.SessionTokenExists, Is.False);
            var provider = Provider();
            Assert.That((bool)provider.GetType().GetMethod("CanLoginAutomatically").Invoke(provider, null), Is.False,
                "The same HasDeviceLinked gate used by next-boot LoginFlow must reach account choice.");
        }

        [Test] public async Task ExplicitGuestLogoutPreservesSoleRecoveryCredentialAndSamePlayerId()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            var id = _fixture.Service.PlayerId;
            Assert.That(_fixture.Service.PlayerInfo.GetUnityId(), Is.Null);
            await Logout(true);
            Assert.That(!_fixture.Service.IsSignedIn && _fixture.Service.SessionTokenExists, Is.True);
            await Login("LoginWithDevice");
            Assert.That(_fixture.Service.PlayerId, Is.EqualTo(id));
        }

        [Test] public async Task OrdinaryHostedSignoutKeepsSessionForRecoveryOrEnvironmentSwitch()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            MarkNativeUnityLinkedIdentity();
            await Logout(false);
            Assert.That(!_fixture.Service.IsSignedIn && _fixture.Service.SessionTokenExists, Is.True);
            var provider = Provider();
            Assert.That((bool)provider.GetType().GetMethod("CanLoginAutomatically").Invoke(provider, null), Is.True);
        }

        [Test] public async Task UnknownNativeIdentityCannotClearAnUnlinkedGuestCredential()
        {
            await _fixture.Service.SignInAnonymouslyAsync();
            _fixture.Service.PlayerInfo = null;
            await Logout(true);
            Assert.That(_fixture.Service.SessionTokenExists, Is.True);
        }

    }
}

sealed class NativeFixture
{
    public AuthenticationServiceInternal Service { get; }
    public SignInResponse Response { get; }
    public int NetworkCalls { get; private set; }
    public ControlledScheduler Scheduler { get; } = new ControlledScheduler();

    public NativeFixture()
    {
        var cache = new MemoryCache();
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"synthetic-lifecycle-player\",\"iat\":" + iat + ",\"exp\":" + (iat + 3600) + "}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Response = new SignInResponse { IdToken = "e30." + payload + ".synthetic", SessionToken = "synthetic-local-session" };
        var network = new LocalNetwork(Response, () => NetworkCalls++);
        var names = new LocalNames();
        Service = new AuthenticationServiceInternal(new AuthenticationSettings(), network, names,
            new ProfileComponent("default"), new JwtDecoder(), cache, Scheduler,
            new LocalMetrics(), new AccessTokenComponent(), new EnvironmentIdComponent(),
            new TargetingComponent(), new PlayerIdComponent(cache), new PlayerNameComponent(cache),
            new SessionTokenComponent(cache), new LocalEnvironment());
    }
}
