using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using AssetResolverService = FirstLight.Game.Services.AssetResolverService;
using RngData = FirstLight.Game.Data.RngData;
using System;
using System.IO;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using GameLovers.Services;
using FirstLight.UiService;
using FirstLight.Game.UI;
using FirstLightServerSDK.Services;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Src.FirstLight.Server.ServerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// Minimal test fixture which include services, current configuration and initial data provider.
	/// It will use serialized configs from the backend folder to ensure they are valid.
	/// This means it will use only the configs available to backend.
	/// </summary>
	public abstract class IntegrationTestFixture
	{
		private static string _serverResourcesPath => $"{Application.dataPath}/../../Backend/ServerCommon/Resources";

		protected string TestPlayerId;
		protected IGameServices TestServices;
		protected ITimeManipulator TimeService;
		protected IRemoteConfigProvider RemoteConfigProvider;
		protected GameLogic TestLogic;
		protected ClientDataService TestData;
		protected ConfigsProvider TestConfigs;
		protected GameStateMachine TestStates;
		protected GameNetworkService TestNetwork;
		protected TutorialService TestTutorial;
		protected AssetResolverService TestAssetResolver = new ();
		protected VfxService<VfxId> TestVfx;
		private IGameDataProvider _previousDataProvider;
		private IGameServices _previousServices;
		private readonly List<ScriptableObject> _nativeConfigs = new();

		[OneTimeSetUp]
		public void SetupOnce()
		{
			var legacyJson = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText($"{_serverResourcesPath}/gameConfig.json"));
			// Backend remains an immutable Q2 reference; adapt only this test's in-memory type keys.
			var configRows = (Newtonsoft.Json.Linq.JObject) legacyJson["Configs"];
			foreach (var entry in configRows.Properties().ToArray())
			{
				if (!entry.Name.StartsWith("Quantum.") || !entry.Name.Contains(", quantum.code,")) continue;
				var typeName = entry.Name.Split(',')[0];
				var currentType = typeof(Quantum.QuantumGameModeConfig).Assembly.GetType(typeName, throwOnError: true);
				configRows[currentType.AssemblyQualifiedName] = entry.Value;
				entry.Remove();
			}
			// Newtonsoft also embeds the old simulation assembly inside dictionary $type values.
			foreach (var property in legacyJson.Descendants().OfType<Newtonsoft.Json.Linq.JProperty>())
			{
				if (property.Name != "$type" || property.Value.Type != Newtonsoft.Json.Linq.JTokenType.String) continue;
				var serializedType = (string) property.Value;
				if (serializedType.Contains(", quantum.code]"))
					property.Value = serializedType.Replace(", quantum.code]", ", " + typeof(Quantum.QuantumGameModeConfig).Assembly.GetName().Name + "]");
			}
			var serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				Converters = {new StringEnumConverter()},
				ContractResolver = new NativeConfigContractResolver(_nativeConfigs)
			});
			var configs = configRows.ToObject<Dictionary<Type, IEnumerable>>(serializer);
			TestConfigs = new ConfigsProvider();
			TestConfigs.UpdateTo(ulong.TryParse((string) legacyJson["Version"], out var version) ? version : 0, configs);

			// TODO: Fix async issue with asset resolver on NUnit
			// TestStates.Run();      // Not working due to async asset loading
			WorkaroundForStateRun(); // Workaround for now
		}

		[OneTimeTearDown]
		public void DisposeNativeConfigs()
		{
			foreach (var config in _nativeConfigs) UnityEngine.Object.DestroyImmediate(config);
			_nativeConfigs.Clear();
		}

		private sealed class NativeConfigContractResolver : DefaultContractResolver
		{
			private readonly List<ScriptableObject> _created;

			public NativeConfigContractResolver(List<ScriptableObject> created) => _created = created;

			protected override JsonObjectContract CreateObjectContract(Type objectType)
			{
				var contract = base.CreateObjectContract(objectType);
				if (typeof(ScriptableObject).IsAssignableFrom(objectType))
				{
					contract.DefaultCreator = () =>
					{
						var config = ScriptableObject.CreateInstance(objectType);
						_created.Add(config);
						return config;
					};
				}
				return contract;
			}
		}

		[SetUp]
		public void Setup()
		{
			MainInstaller.TryResolve(out _previousDataProvider);
			MainInstaller.TryResolve(out _previousServices);
			MainInstaller.Clean<IGameDataProvider>();
			MainInstaller.Clean<IGameServices>();
			var messageBroker = new MessageBrokerService();
			TimeService = new TimeService();
			RemoteConfigProvider = new UnityRemoteConfigProvider();
			TestNetwork = new GameNetworkService(TestConfigs);


			TestData = SetupPlayer(TestConfigs);
			TestLogic = new GameLogic(messageBroker, RemoteConfigProvider, TimeService, TestData, TestConfigs);

			TestServices = new StubGameServices(TestNetwork, messageBroker, TimeService, TestData,
				TestConfigs, TestLogic, TestData,
				TestAssetResolver, TestTutorial);
			TestNetwork.StartNetworking(TestLogic, TestServices);
			TestLogic.Init();

			TestStates = new GameStateMachine(TestLogic, TestServices, TestNetwork, TestAssetResolver);

			FLog.Init();

			var integrationAppData = TestData.GetData<AccountData>();
			integrationAppData.DeviceId = "integration_test";
			//TestData.SaveData<AppData>();


			MainInstaller.Bind<IGameDataProvider>(TestLogic);
			MainInstaller.Bind<IGameServices>(TestServices);
		}

		[TearDown]
		public void TearDown()
		{
			MainInstaller.Clean<IGameDataProvider>();
			MainInstaller.Clean<IGameServices>();
			if (_previousDataProvider != null) MainInstaller.Bind(_previousDataProvider);
			if (_previousServices != null) MainInstaller.Bind(_previousServices);
			_previousDataProvider = null;
			_previousServices = null;
		}

		protected async Task WaitFor(Func<bool> condition, int timeoutMillis = 10000)
		{
			var timeouts = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMillis);
			while (DateTime.UtcNow < timeouts && !condition())
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}
		}

		private ClientDataService SetupPlayer(IConfigsProvider configs)
		{
			TestPlayerId = Guid.NewGuid().ToString();
			var playerSetup = new BlastRoyalePlayerSetup(configs);
			var initialServerState = playerSetup.GetInitialState(TestPlayerId);
			var data = new ClientDataService();
			data.AddData(initialServerState.DeserializeModel<PlayerData>());
			data.AddData(initialServerState.DeserializeModel<EquipmentData>());
			data.AddData(initialServerState.DeserializeModel<RngData>());
			data.AddData(initialServerState.DeserializeModel<IdData>());
			data.AddData(new AppData());
			return data;
		}

		private void WorkaroundForStateRun()
		{
#pragma warning disable CS0612
			var quantumAddress = AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address;
			var quantumAsset = Addressables.LoadAssetAsync<QuantumRunnerConfigs>(quantumAddress).WaitForCompletion();
			TestConfigs.AddSingletonConfig(quantumAsset);
#pragma warning restore CS0612
		}
	}
}
