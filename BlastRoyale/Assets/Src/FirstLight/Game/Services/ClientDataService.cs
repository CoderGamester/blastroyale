using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Scripting;
using ServerDataProvider = FirstLight.Server.SDK.Models.IDataProvider;

namespace FirstLight.Game.Services
{
	public interface IClientDataSaver
	{
		void SaveData<T>() where T : class;
		void SaveAllData();
	}

	/// <summary>Client state bridge to the legacy server command contract.</summary>
	public interface IClientDataService : ServerDataProvider, IClientDataSaver
	{
		T LoadData<T>() where T : class;
		void AddData<T>(T data, bool isLocal = false) where T : class;
		void AddData(Type type, object data, bool isLocal = false);
	}

	/// <summary>
	/// Uses maintained GameLovers stores while keeping remote player state out of
	/// PlayerPrefs. The dynamic registry exists for server state serialization and
	/// command dispatch; local persistence remains typed and preserves existing keys.
	/// </summary>
	public sealed class ClientDataService : IClientDataService
	{
		private sealed class Entry
		{
			public Func<object> Read;
			public Action Save;
		}

		private readonly GameLovers.Services.DataService _remote = new GameLovers.Services.DataService();
		private readonly GameLovers.Services.DataService _local = new GameLovers.Services.DataService();
		private readonly Dictionary<Type, Entry> _entries = new Dictionary<Type, Entry>();
		private static readonly MethodInfo AddTyped = typeof(ClientDataService).GetMethods()
			.Single(method => method.Name == nameof(AddData) && method.IsGenericMethodDefinition);

		public T GetData<T>() where T : class
		{
			if (!TryGetData<T>(out var data))
			{
				data = Activator.CreateInstance<T>();
				AddData(data);
			}
			return data;
		}

		public object GetData(Type type) => _entries[type].Read();
		public IEnumerable<Type> GetKeys() => _entries.Keys;

		public bool TryGetData<T>(out T data) where T : class
		{
			var found = TryGetData(typeof(T), out var value);
			data = value as T;
			return found;
		}

		public bool TryGetData(Type type, out object data)
		{
			if (_entries.TryGetValue(type, out var entry))
			{
				data = entry.Read();
				return true;
			}
			data = null;
			return false;
		}

		[Preserve]
		public void AddData<T>(T data, bool isLocal = false) where T : class
		{
			var store = isLocal ? _local : _remote;
			store.AddOrReplaceData(data);
			_entries[typeof(T)] = new Entry
			{
				Read = () => store.GetData<T>(),
				Save = isLocal ? (Action)(() => store.SaveData<T>()) : null
			};
		}

		public void AddData(Type type, object data, bool isLocal = false)
		{
			if (type == null || type.IsValueType || type.ContainsGenericParameters || data != null && !type.IsInstanceOfType(data))
				throw new ArgumentException("Client state must match its declared reference type.", nameof(type));
			// ServerState carries runtime types. All state is reference-typed, so
			// this call uses IL2CPP's shared reference-type generic implementation.
			AddTyped.MakeGenericMethod(type).Invoke(this, new[] { data, (object)isLocal });
		}

		public T LoadData<T>() where T : class
		{
			var value = _local.LoadData<T>() ?? Activator.CreateInstance<T>();
			AddData(value, true);
			return value;
		}

		public void SaveData<T>() where T : class => _entries[typeof(T)].Save?.Invoke();

		public void SaveAllData()
		{
			foreach (var entry in _entries.Values) entry.Save?.Invoke();
		}
	}
}
