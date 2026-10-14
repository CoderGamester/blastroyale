using GameLovers.GameData;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableDictionaryTest
	{
		private const int _key = 0;
		private const int _previousValue = 5;
		private const int _newValue = 10;
		
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in TKey, in TValue>
		{
			void Call(TKey key, TValue previousValue, TValue newValue, ObservableUpdateType updateType);
		}
		
		private ObservableDictionary<int, int> _observableDictionary;
		private ObservableDictionary<int, int> _rebindableDictionary;
		private IDictionary<int,int> _mockDictionary;
		private IMockCaller<int, int> _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int, int>>();
			_mockDictionary = Substitute.For<IDictionary<int, int>>();
			_observableDictionary = new ObservableDictionary<int, int>(_mockDictionary) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
			_rebindableDictionary = new ObservableDictionary<int, int>(_mockDictionary) {ObservableUpdateFlag = ObservableUpdateFlag.Both};

			_mockDictionary.TryGetValue(_key, out _).Returns(callInfo =>
			{
				callInfo[1] = _mockDictionary[_key];
				return true;
			});
		}

		[Test]
		public void Rebind_PreservesReaderAndSubscription()
		{
			var original = new Dictionary<int, int> {{_key, 1}};
			var replacement = new Dictionary<int, int> {{_key, 2}};
			var dictionary = new ObservableDictionary<int, int>(original) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
			IObservableDictionaryReader<int, int> reader = dictionary;
			var keyedNotifications = 0;
			var globalNotifications = 0;
			dictionary.Observe(_key, (key, previous, current, update) => keyedNotifications++);
			dictionary.Observe((key, previous, current, update) => globalNotifications++);

			dictionary.Rebind(replacement);
			dictionary[_key] = 3;

			Assert.AreEqual(1, original[_key]);
			Assert.AreEqual(3, replacement[_key]);
			Assert.AreEqual(3, reader[_key]);
			Assert.AreEqual(1, keyedNotifications);
			Assert.AreEqual(1, globalNotifications);
		}

		[Test]
		public void ValueCheck()
		{
			_mockDictionary[_key].Returns(_previousValue);
			
			Assert.AreEqual(_previousValue, _observableDictionary[_key]);
			Assert.AreEqual(_previousValue, _rebindableDictionary[_key]);
		}

		[Test]
		public void ValueSetCheck()
		{
			const int valueCheck1 = 5;
			const int valueCheck2 = 6;
			const int valueCheck3 = 7;
			
			_mockDictionary[_key] = valueCheck1;
			
			Assert.AreEqual(valueCheck1, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck1, _rebindableDictionary[_key]);

			_observableDictionary[_key] = valueCheck2;
			
			Assert.AreEqual(valueCheck2, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck2, _rebindableDictionary[_key]);

			_rebindableDictionary[_key] = valueCheck3;
			
			Assert.AreEqual(valueCheck3, _observableDictionary[_key]);
			Assert.AreEqual(valueCheck3, _rebindableDictionary[_key]);
		}

		[Test]
		public void ObserveCheck()
		{
			// Real storage exercises Add/Update/Remove, including successful removal.
			_observableDictionary = new ObservableDictionary<int, int>(new Dictionary<int, int>()) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
			_rebindableDictionary = new ObservableDictionary<int, int>(new Dictionary<int, int>()) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
			_observableDictionary.Observe(_key, _caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_key, _caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
			
			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			
			_observableDictionary[_key] = _newValue;
			
			_rebindableDictionary[_key] = _newValue;
			
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.Received(4).Call(_key, 0, _previousValue, ObservableUpdateType.Added);
			_caller.Received(4).Call(_key, _previousValue, _newValue,ObservableUpdateType.Updated);
			_caller.Received(4).Call(_key, _newValue, 0, ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeObserveCheck()
		{
			_observableDictionary.InvokeObserve(_key, _caller.Call);
			_rebindableDictionary.InvokeObserve(_key, _caller.Call);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received(2).Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck()
		{
			_observableDictionary.Observe(_key, _caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_key, _caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			
			_observableDictionary.InvokeUpdate(_key);
			_rebindableDictionary.InvokeUpdate(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received(4).Call(_key, 0, 0, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(),Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_observableDictionary.InvokeUpdate(_key);
			_rebindableDictionary.InvokeUpdate(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(),Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserveCheck()
		{
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			_observableDictionary.StopObserving(_caller.Call);
			_rebindableDictionary.StopObserving(_caller.Call);

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserve_KeyCheck()
		{
			_observableDictionary.Observe(_key, _caller.Call);
			_rebindableDictionary.Observe(_key, _caller.Call);
			_observableDictionary.StopObserving(_key);
			_rebindableDictionary.StopObserving(_key);

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAllCheck()
		{
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll(_caller);
			_rebindableDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_MultipleCalls_Check()
		{
			_observableDictionary.Observe(_caller.Call);
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll(_caller);
			_rebindableDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_Everything_Check()
		{
			_observableDictionary.Observe(_caller.Call);
			_rebindableDictionary.Observe(_caller.Call);
			_observableDictionary.StopObservingAll();
			_rebindableDictionary.StopObservingAll();

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_observableDictionary.StopObservingAll(_caller);
			_rebindableDictionary.StopObservingAll(_caller);

			_observableDictionary.Add(_key, _previousValue);
			_rebindableDictionary.Add(_key, _previousValue);
			_observableDictionary[_key] = _previousValue;
			_rebindableDictionary[_key] = _previousValue;
			_observableDictionary.Remove(_key);
			_rebindableDictionary.Remove(_key);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(),Arg.Any<ObservableUpdateType>());
		}
	}
}
