using GameLovers.GameData;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.DataExtensions.Tests
{
	[TestFixture]
	public class ObservableListTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller<in T>
		{
			void Call(int index, T value, T valueChange, ObservableUpdateType updateType);
		}
		
		private const int _index = 0;
		private const int _previousValue = 5;
		private const int _newValue = 10;

		private ObservableList<int> _observableList;
		private ObservableList<int> _rebindableList;
		private List<int> _list;
		private IMockCaller<int> _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller<int>>();
			_list = Substitute.For<List<int>>();
			_observableList = new ObservableList<int>(_list);
			_rebindableList = new ObservableList<int>(_list);
		}

		[Test]
		public void Rebind_PreservesReaderAndSubscription()
		{
			var original = new List<int> {1};
			var replacement = new List<int> {2};
			var list = new ObservableList<int>(original);
			IObservableListReader<int> reader = list;
			var notifications = 0;
			list.Observe((index, previous, current, update) => notifications++);

			list.Rebind(replacement);
			list[0] = 3;

			Assert.AreEqual(1, original[0]);
			Assert.AreEqual(3, replacement[0]);
			Assert.AreEqual(3, reader[0]);
			Assert.AreEqual(1, notifications);
		}

		[Test]
		public void ValueCheck()
		{
			_list.Add(_previousValue);
			
			Assert.AreEqual(_previousValue, _observableList[_index]);
			Assert.AreEqual(_previousValue, _rebindableList[_index]);
		}

		[Test]
		public void ValueSetCheck()
		{
			const int valueCheck1 = 5;
			const int valueCheck2 = 6;
			const int valueCheck3 = 7;
			
			_list.Add(valueCheck1);
			
			Assert.AreEqual(valueCheck1, _observableList[_index]);
			Assert.AreEqual(valueCheck1, _rebindableList[_index]);

			_observableList[_index] = valueCheck2;
			
			Assert.AreEqual(valueCheck2, _observableList[_index]);
			Assert.AreEqual(valueCheck2, _rebindableList[_index]);

			_rebindableList[_index] = valueCheck3;
			
			Assert.AreEqual(valueCheck3, _observableList[_index]);
			Assert.AreEqual(valueCheck3, _rebindableList[_index]);
		}

		[Test]
		public void ObserveCheck()
		{
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
			
			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			
			_observableList[_index] = _newValue;
			_list[_index] = _previousValue;
			_rebindableList[_index] = _newValue;
			
			_observableList.RemoveAt(_index);
			_list[_index] = _newValue;
			_rebindableList.RemoveAt(_index);
			
			_caller.Received(2).Call(Arg.Any<int>(), Arg.Is(0), Arg.Is(_previousValue), ObservableUpdateType.Added);
			_caller.Received(2).Call(_index, _previousValue, _newValue, ObservableUpdateType.Updated);
			_caller.Received(2).Call(_index, _newValue, 0, ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeObserveCheck()
		{
			_list.Add(_previousValue);
			
			_observableList.InvokeObserve(_index, _caller.Call);
			_rebindableList.InvokeObserve(_index, _caller.Call);
			
			_caller.DidNotReceive().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Added);
			_caller.Received(2).Call(_index, _previousValue, _previousValue, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(_index, _previousValue, _previousValue, ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck()
		{
			_list.Add(_previousValue);
			
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
			
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Added);
			_caller.Received(2).Call(_index, _previousValue, _previousValue, ObservableUpdateType.Updated);
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), ObservableUpdateType.Removed);
		}

		[Test]
		public void InvokeCheck_NotObserving_DoesNothing()
		{
			_list.Add(_previousValue);
			
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObserveCheck()
		{
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			_observableList.StopObserving(_caller.Call);
			_rebindableList.StopObserving(_caller.Call);
			
			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			_observableList[_index] = _previousValue;
			_rebindableList[_index] = _previousValue;
			_observableList.RemoveAt(_index);
			_rebindableList.RemoveAt(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAllCheck()
		{
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			_observableList.StopObservingAll(_caller);
			_rebindableList.StopObservingAll(_caller);

			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_MultipleCalls_Check()
		{
			_observableList.Observe(_caller.Call);
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			_observableList.StopObservingAll(_caller);
			_rebindableList.StopObservingAll(_caller);

			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_Everything_Check()
		{
			_observableList.Observe(_caller.Call);
			_rebindableList.Observe(_caller.Call);
			_observableList.StopObservingAll();
			_rebindableList.StopObservingAll();

			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}

		[Test]
		public void StopObservingAll_NotObserving_DoesNothing()
		{
			_observableList.StopObservingAll();
			_rebindableList.StopObservingAll();

			_observableList.Add(_previousValue);
			_rebindableList.Add(_previousValue);
			_observableList.InvokeUpdate(_index);
			_rebindableList.InvokeUpdate(_index);
			
			_caller.DidNotReceive().Call(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ObservableUpdateType>());
		}
	}
}
