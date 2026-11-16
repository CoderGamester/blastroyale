using FirstLight.Inspector;
using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using IAssetAdderService = FirstLight.Game.Services.IAssetAdderService;
using AssetResolverService = FirstLight.Game.Services.AssetResolverService;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Serializers;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using GameLovers.Services;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.UnityConsent;
using Debug = UnityEngine.Debug;
using GameObject = UnityEngine.GameObject;

namespace FirstLight.Game
{
	/// <summary>
	/// Contains the complete startup / init of our game
	/// </summary>
	public class Startup : MonoBehaviour
	{
		[SerializeField, InspectorField(Required = true)] private AudioSource _audioSource;

		private void Start()
		{
			StartTask().Forget();
		}

		private async UniTask StartTask()
		{
			InitTaskLogging();
			InitPlugins();

			InitGlobalShaderData();

			await ATTrackingUtils.RequestATTPermission();
			await VersionUtils.LoadVersionDataAsync();
			await FLEnvironment.CheckForEnvironmentRedirect(VersionUtils.BuildNumber);
			await InitUnityServices();
			InitAnalytics();

			FLGTestRunner.Instance.CheckAutomations();
			FLGTestRunner.Instance.BeforeGameAwaken();

			TouchSimulation.Enable();
			EnhancedTouchSupport.Enable();

			FLGCustomSerializers.RegisterAOT();
			FLGCustomSerializers.RegisterSerializers();
			FeatureFlags.ParseLocalFeatureFlags();

			// This uglyness is here because we need to show the loading screen before loading configs, which need this tuple
			var (services, assetResolver, configsProvider) = InitFLGServices();
			OhYeah();

			await StartupLoadingHelper.LoadConfigs(services, assetResolver, configsProvider);

			InitSettings();
			InitAppEventsListener();
			// TODO: MOVE THIS AWAY FORM HERE
			await services.NotificationService.RegisterForNotifications();

			StartGameStateMachine();

			FLGTestRunner.Instance.AfterGameAwaken();

			Destroy(gameObject);
		}

		private static void InitAppEventsListener()
		{
			var go = new GameObject("AppEventsListener");
			go.AddComponent<AppEventsListener>();
			DontDestroyOnLoad(go);
		}

		private void InitTaskLogging()
		{
			TaskScheduler.UnobservedTaskException += (obj, e) =>
			{
				try
				{
					if (obj is Task task)
					{
						var convert = Convert(task);
						var objName = convert.Exception?.ToString();

						FLog.Error("Async task Exception happened " + objName);
					}
					else
					{
						FLog.Error("Try Exception Logging called");
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

				Debug.LogException(e.Exception);
				return;

				static async Task<object> Convert(Task task)
				{
					await task;
					var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
					if (voidTaskType.IsInstanceOfType(task))
						throw new InvalidOperationException("Task does not have a return value (" + task.GetType() + ")");
					var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
					if (property == null)
						throw new InvalidOperationException("Task does not have a return value (" + task.GetType() + ")");
					return property.GetValue(task);
				}
			};
		}

		private static void StartGameStateMachine()
		{
			MainInstaller.Resolve<IGameStateMachine>().Run();
		}

		// TODO: This should not return the tuple, but we need UI Service before we await config loading
		private static (IGameServices, IAssetAdderService, IConfigsAdder) InitFLGServices()
		{
			var messageBroker = new MessageBrokerService();
			var timeService = new TimeService();
			var dataService = new ClientDataService();
			dataService.LoadData<AppData>();
			var assetResolver = new AssetResolverService();
			var configsProvider = new ConfigsProvider();
			var serverConfigProvider = new UnityRemoteConfigProvider();
			var networkService = new GameNetworkService(configsProvider);

			var gameLogic = new GameLogic(messageBroker, serverConfigProvider, timeService, dataService, configsProvider);
			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService, configsProvider, gameLogic, assetResolver);

			networkService.StartNetworking(gameLogic, gameServices);
			networkService.EnableQuantumPingCheck(true);

			MainInstaller.Bind<IGameServices>(gameServices);
			MainInstaller.Bind<IGameStateMachine>(new GameStateMachine(gameLogic, gameServices, networkService, assetResolver));

			return (gameServices, assetResolver, configsProvider);
		}

		private static async UniTask InitUnityServices()
		{
			// UGS Core reads Application.cloudProjectId; an arbitrary initialization option cannot override it.
			// The local ID verifies the developer linked the intended free project in Editor > Project Settings > Services.
			var localProjectId = FLEnvironment.Configuration.UnityProjectId;
			if (string.IsNullOrWhiteSpace(localProjectId) || localProjectId != Application.cloudProjectId)
				throw new InvalidOperationException("Link your free Unity Cloud project in Project Settings > Services and put its Project ID in Assets/Resources/LocalClientConfig.json.");
			var initOpts = new InitializationOptions();

			FLog.Info("Using unity environment: " + FLEnvironment.Current.UCSEnvironmentName);
			initOpts.SetEnvironmentName(FLEnvironment.Current.UCSEnvironmentName);
			if (!string.IsNullOrWhiteSpace(FLEnvironment.Current.UCSEnvironmentID))
				RemoteConfigService.Instance.SetEnvironmentID(FLEnvironment.Current.UCSEnvironmentID);
			await UnityServices.InitializeAsync(initOpts).AsUniTask();
			await Addressables.InitializeAsync().Task.AsUniTask();

		}

		private static void InitAnalytics()
		{
			var consent = EndUserConsent.GetConsentState();
			consent.AnalyticsIntent = ATTrackingUtils.IsTrackingAllowed() ? ConsentStatus.Granted : ConsentStatus.Denied;
			EndUserConsent.SetConsentState(consent);
		}

		private void OhYeah()
		{
			if (MainInstaller.ResolveServices().LocalPrefsService.IsSFXEnabled)
			{
				_audioSource.Play();
			}
		}

		private static void InitPlugins()
		{
#if DEBUG
			FirstLight.Game.Cheats.RuntimeDebugPanel.Initialize();
#endif

			Debug.developerConsoleEnabled = false;
			DebugManager.instance.enableRuntimeUI = false;
			FLog.Init();
		}

		private static void InitGlobalShaderData()
		{
			// Used for DPI-based scaling in shaders
			Shader.SetGlobalVector(Shader.PropertyToID("_PhysicalScreenSize"),
				new Vector4(Screen.width / Screen.dpi, Screen.height / Screen.dpi, Screen.dpi, 69));
		}

		private static void InitSettings()
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			// Probably not the best place for this
			MainInstaller.ResolveServices().LocalPrefsService.IsFPSLimitEnabled.InvokeObserve((_, limitEnabled) =>
			{
				Application.targetFrameRate = limitEnabled ? 30 : 60;
			});
		}

	}
}
