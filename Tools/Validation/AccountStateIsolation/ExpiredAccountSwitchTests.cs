using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using NUnit.Framework;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Authentication
{
    public sealed class ExpiredAccountSwitchTests
    {
        [Test] public async Task ExpiredNativeAccountSwitchCancellationReleasesGateAndAllowsCachedProviderRetry()
        {
            var instance = typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            var fixture = new NativeFixture();
            AuthenticationService.Instance = fixture.Service;
            try
            {
                await fixture.Service.SignInAnonymouslyAsync();
                var account = UgsAccountSession.Capture();
                var auth = new AuthService(null, null, new ClientDataService(), null, null, new GameLovers.Services.MessageBrokerService(), null);
                // Isolate orchestration before backend load; actual AuthService and provider remain imported game code.
                typeof(AuthService).GetField("_authenticationHooks", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(auth, new List<IAuthenticationHook>());
                ((SessionData)auth.SessionData).IsAuthenticated = true;
                fixture.Scheduler.Execute(fixture.Service.ExpirationActionId.Value);
                Assert.That(fixture.Service.IsSignedIn && fixture.Service.IsExpired && !fixture.Service.IsAuthorized, Is.True);
                Assert.That(account.IsCurrent, Is.False);
                var attempts = 0;
                Func<UniTask<LoginResult>> canceledSwitch = () =>
                {
                    attempts++;
                    return UniTask.FromException<LoginResult>(new OperationCanceledException("Controlled hosted account-switch cancellation"));
                };
                var canceled = await Error(LoginProcess(auth, canceledSwitch));
                Assert.That(canceled, Is.TypeOf<OperationCanceledException>());
                Assert.That(attempts, Is.EqualTo(1), "Native Expired must reach the switch callback instead of throwing before try/finally.");
                Assert.That(IsBusy(auth), Is.False, "Every failure must release the actual AuthService login gate.");
                Assert.That(auth.SessionData.IsAuthenticated, Is.False);

                var before = fixture.NetworkCalls;
                var provider = typeof(AuthService).GetField("_loginProvider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(auth);
                Func<UniTask<LoginResult>> retry = async () =>
                {
                    attempts++;
                    await (UniTask<LoginResult>)provider.GetType().GetMethod("LoginWithDevice").Invoke(provider, null);
                    throw new StopBeforeBackendLoadException();
                };
                var retried = await Error(LoginProcess(auth, retry));
                Assert.That(retried, Is.TypeOf<StopBeforeBackendLoadException>());
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(fixture.NetworkCalls, Is.EqualTo(before + 1));
                Assert.That(fixture.Service.IsAuthorized && UgsAccountSession.Capture().IsCurrent, Is.True);
                Assert.That(IsBusy(auth), Is.False);
            }
            finally { instance.SetValue(null, previous); UgsAccountSession.Invalidate(); }
        }

        private static UniTask LoginProcess(AuthService auth, Func<UniTask<LoginResult>> login) =>
            (UniTask)typeof(AuthService).GetMethod("LoginProcess", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(auth, new object[] { login, false });
        private static bool IsBusy(AuthService auth) => (bool)typeof(AuthService)
            .GetField("_authenticationInProgress", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(auth);
        private static async Task<Exception> Error(UniTask operation)
        {
            try { await operation; return null; } catch (Exception error) { return error; }
        }
        private sealed class StopBeforeBackendLoadException : Exception { }
    }
}
