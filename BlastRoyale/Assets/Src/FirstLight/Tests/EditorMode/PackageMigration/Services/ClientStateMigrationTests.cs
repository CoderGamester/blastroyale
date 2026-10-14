using System.Collections.Generic;
using FirstLight.Game.Services;
using GameLovers.Services;
using NUnit.Framework;
using UnityEngine;

namespace FirstLight.Tests.EditorMode.PackageMigration
{
	public sealed class ClientStateMigrationTests
	{
		public sealed class MigrationLocalState { public int Value; }
		public sealed class MigrationRemoteState { public int Value; }

		[SetUp, TearDown]
		public void CleanOnlyTestKeys()
		{
			PlayerPrefs.DeleteKey(nameof(MigrationLocalState));
			PlayerPrefs.DeleteKey(nameof(MigrationRemoteState));
		}

		[Test]
		public void PersistsLocalStateAndLeavesRemoteStateInMemory()
		{
			var data = new ClientDataService();
			data.AddData(new MigrationLocalState { Value = 7 }, true);
			data.AddData(new MigrationRemoteState { Value = 8 });
			data.SaveAllData();
			Assert.That(new ClientDataService().LoadData<MigrationLocalState>().Value, Is.EqualTo(7));
			Assert.That(PlayerPrefs.HasKey(nameof(MigrationRemoteState)), Is.False);
		}

		[Test]
		public void DynamicServerRegistryPreservesReferenceIdentity()
		{
			var data = new ClientDataService();
			var state = new MigrationLocalState { Value = 42 };
			data.AddData(typeof(MigrationLocalState), state, true);
			Assert.That(data.GetData(typeof(MigrationLocalState)), Is.SameAs(state));
			Assert.That(data.GetData<MigrationLocalState>(), Is.SameAs(state));
			data.SaveData<MigrationLocalState>();
			Assert.That(new ClientDataService().LoadData<MigrationLocalState>().Value, Is.EqualTo(42));
		}

		[Test]
		public void ReplacingLocalStateWithRemoteStateDoesNotSaveStaleData()
		{
			var data = new ClientDataService();
			data.AddData(new MigrationLocalState { Value = 1 }, true);
			data.AddData(new MigrationLocalState { Value = 2 }, false);
			data.SaveAllData();
			Assert.That(PlayerPrefs.HasKey(nameof(MigrationLocalState)), Is.False);
			Assert.That(data.GetData<MigrationLocalState>().Value, Is.EqualTo(2));
		}

		[Test]
		public void KeepsLegacyLocalPersistenceKey()
		{
			PlayerPrefs.SetString(nameof(MigrationLocalState), "{\"Value\":99}");
			Assert.That(new ClientDataService().LoadData<MigrationLocalState>().Value, Is.EqualTo(99));
		}

		private struct TransitionMessage : IMessage { }
		private sealed class Listener
		{
			private readonly string _name;
			private readonly List<string> _calls;
			public System.Action AfterCall;
			public Listener(string name, List<string> calls) { _name = name; _calls = calls; }
			public void OnMessage(TransitionMessage _) { _calls.Add(_name); AfterCall?.Invoke(); }
		}

		[Test]
		public void MessageSnapshotAllowsSubscriptionChangesDuringTransitions()
		{
			var broker = new MessageBrokerService();
			var calls = new List<string>();
			var first = new Listener("first", calls);
			var second = new Listener("second", calls);
			var third = new Listener("third", calls);
			first.AfterCall = () =>
			{
				broker.Unsubscribe<TransitionMessage>(second);
				broker.Subscribe<TransitionMessage>(third.OnMessage);
			};
			broker.Subscribe<TransitionMessage>(first.OnMessage);
			broker.Subscribe<TransitionMessage>(second.OnMessage);
			Assert.DoesNotThrow(() => broker.PublishSafe(new TransitionMessage()));
			CollectionAssert.AreEqual(new[] { "first", "second" }, calls);
			calls.Clear();
			broker.PublishSafe(new TransitionMessage());
			CollectionAssert.AreEqual(new[] { "first", "third" }, calls);
		}
	}
}
