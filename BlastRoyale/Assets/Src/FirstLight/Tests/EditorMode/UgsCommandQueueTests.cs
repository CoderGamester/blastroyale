using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using GameLovers.Services;
using NSubstitute;
using NUnit.Framework;
using Unity.Services.Authentication;

namespace FirstLight.Tests.EditorMode
{
	public class UgsCommandQueueTests
	{
		private static readonly BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
		private IAuthenticationService _authentication;
		private object _previousAuthentication;
		private object _previousLocalFlags;
		private Type _authenticationType;
		private object _playerIdComponent;
		private ServerCommandQueue _queue;
		private PlayerData _player;
		private IGameCommand _command;
		private readonly List<SaveRequest> _saves = new();

		[SetUp]
		public void SetUp()
		{
			FLog.Init();
			// Exercise the installed SDK's actual state/event engine without network requests.
			// Only its real player-ID/access-token components are needed by these transitions.
			var assembly = typeof(AuthenticationService).Assembly;
			_authenticationType = assembly.GetType("Unity.Services.Authentication.AuthenticationServiceInternal", true);
			var native = FormatterServices.GetUninitializedObject(_authenticationType);
			_playerIdComponent = FormatterServices.GetUninitializedObject(assembly.GetType("Unity.Services.Authentication.PlayerIdComponent", true));
			SetCachedPlayerId("queue-account-a");
			SetNativeComponent(native, "PlayerIdComponent", _playerIdComponent);
			SetNativeComponent(native, "AccessTokenComponent", Activator.CreateInstance(assembly.GetType("Unity.Services.Authentication.AccessTokenComponent", true), true));
			var state = _authenticationType.GetProperty("State", InstanceMembers);
			state.SetValue(native, Enum.Parse(state.PropertyType, "Authorized"));
			_authentication = (IAuthenticationService) native;
			var singleton = typeof(AuthenticationService).GetField("s_Instance", StaticMembers);
			_previousAuthentication = singleton.GetValue(null);
			singleton.SetValue(null, native);

			var flags = typeof(FeatureFlags).GetField("_localConfig", StaticMembers);
			_previousLocalFlags = flags.GetValue(null);
			flags.SetValue(null, new LocalFeatureFlagConfig { OfflineMode = false });
			_saves.Clear();
			var backend = Substitute.For<IGameBackendService, IClientAuthorityBackend>();
			((IClientAuthorityBackend) backend).PersistPlayerState(Arg.Any<ServerState>()).Returns(call =>
			{
				var request = new SaveRequest(call.Arg<ServerState>());
				_saves.Add(request);
				return request.Completion.Task;
			});
			_player = new PlayerData();
			var data = new ClientDataService();
			data.AddData(_player);
			_queue = new ServerCommandQueue(data, null, backend, null);
			_command = Substitute.For<IGameCommand>();
		}

		[TearDown]
		public void TearDown()
		{
			_authentication.SignOut();
			// Drain any intentionally delayed save; a stale completion cannot dispatch another account's work.
			foreach (var save in _saves) save.Completion.TrySetResult();
			var invalidate = (Action) UgsAccountSession.Invalidate;
			_authentication.SignedIn -= invalidate;
			_authentication.SignedOut -= invalidate;
			_authentication.Expired -= invalidate;
			var playerChanged = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>),
				typeof(UgsAccountSession).GetMethod("OnPlayerIdChanged", StaticMembers));
			_authentication.PlayerIdChanged -= playerChanged;
			typeof(UgsAccountSession).GetField("_authentication", StaticMembers).SetValue(null, null);
			typeof(AuthenticationService).GetField("s_Instance", StaticMembers).SetValue(null, _previousAuthentication);
			typeof(FeatureFlags).GetField("_localConfig", StaticMembers).SetValue(null, _previousLocalFlags);
		}

		[Test]
		public void DelayedOldAccountCompletionPreservesNewAccountLatestSave()
		{
			Enqueue(10);
			_authentication.SignOut();
			SetCachedPlayerId("queue-account-b");
			ChangeNativeState("Authorized");
			Enqueue(20);
			Enqueue(30);
			Assert.That(_saves.Count, Is.EqualTo(1), "The old request is still in flight.");

			_saves[0].Completion.TrySetResult();
			Assert.That(_saves.Count, Is.EqualTo(2));
			Assert.That(_saves[1].Trophies, Is.EqualTo(30), "Old completion must not remove the new account's head.");
			Assert.That(PendingCount, Is.EqualTo(1));
			_saves[1].Completion.TrySetResult();
			Assert.That(PendingCount, Is.Zero);
		}

		[Test]
		public void SamePlayerSignOutAndSignInInvalidatesPreviousSession()
		{
			var old = UgsAccountSession.Capture();
			_authentication.SignOut();
			Assert.That(old.IsCurrent, Is.False);
			ChangeNativeState("Authorized");
			Assert.That(_authentication.PlayerId, Is.EqualTo(old.PlayerId));
			Assert.That(old.IsCurrent, Is.False);
			Assert.Throws<OperationCanceledException>(() => old.ThrowIfChanged());
			Assert.That(UgsAccountSession.Capture().Generation, Is.GreaterThan(old.Generation));
		}

		[Test]
		public void NativeNormalRefreshPreservesAccountGenerationAndPendingSave()
		{
			var account = UgsAccountSession.Capture();
			Enqueue(10);
			ChangeNativeState("Refreshing");
			Assert.That(_authentication.IsAuthorized, Is.True);
			ChangeNativeState("Authorized");
			Assert.That(account.IsCurrent, Is.True);
			Assert.That(UgsAccountSession.Capture().Generation, Is.EqualTo(account.Generation));
			_saves[0].Completion.TrySetResult();
			Assert.That(PendingCount, Is.Zero);
		}

		[Test]
		public void FailedSaveRetainsHeadAndLatestImmutableSnapshotForRetry()
		{
			Enqueue(10);
			Enqueue(20);
			Enqueue(30);
			_saves[0].Completion.TrySetException(new InvalidOperationException("Expected persistence failure"));
			Assert.That(PendingCount, Is.EqualTo(2));
			Assert.That(_saves.Count, Is.EqualTo(1));

			// Another command retries the retained head and the latest unchanged player state.
			Enqueue(30);
			Assert.That(_saves[1].Trophies, Is.EqualTo(10));
			_saves[1].Completion.TrySetResult();
			Assert.That(_saves[2].Trophies, Is.EqualTo(30));
			Assert.That(_saves[2].State.DeserializeModel<PlayerData>().Trophies, Is.EqualTo(30));
			_saves[2].Completion.TrySetResult();
			Assert.That(PendingCount, Is.Zero);
		}

		[Test]
		public void NativeExpiryInvalidatesSessionAndRecoveryEmitsNewSignIn()
		{
			var account = UgsAccountSession.Capture();
			_authenticationType.GetMethod("Expire", InstanceMembers).Invoke(_authentication, null);
			Assert.That(_authentication.IsSignedIn, Is.True);
			Assert.That(_authentication.IsAuthorized, Is.False);
			Assert.That(account.IsCurrent, Is.False);
			ChangeNativeState("Authorized");
			Assert.That(account.IsCurrent, Is.False);
			Assert.That(UgsAccountSession.Capture().Generation, Is.GreaterThan(account.Generation));
		}

		private int PendingCount => ((ICollection) typeof(ServerCommandQueue).GetField("_clientSaves", InstanceMembers).GetValue(_queue)).Count;
		private void Enqueue(uint trophies) { _player.Trophies = trophies; _queue.EnqueueCommand(_command); }
		private void SetCachedPlayerId(string playerId) => _playerIdComponent.GetType().GetField("m_PlayerId", InstanceMembers).SetValue(_playerIdComponent, playerId);
		private void ChangeNativeState(string state)
		{
			var method = _authenticationType.GetMethod("ChangeState", InstanceMembers);
			method.Invoke(_authentication, new[] { Enum.Parse(method.GetParameters()[0].ParameterType, state) });
		}
		private void SetNativeComponent(object instance, string name, object value) =>
			_authenticationType.GetField("<" + name + ">k__BackingField", InstanceMembers).SetValue(instance, value);

		private sealed class SaveRequest
		{
			public readonly ServerState State;
			public readonly UniTaskCompletionSource Completion = new();
			public uint Trophies => State.DeserializeModel<PlayerData>().Trophies;
			public SaveRequest(ServerState state) { State = state; }
		}
	}
}
