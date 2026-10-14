using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using GameLovers.StatechartMachine;
using FirstLightServerSDK.Services;
using FirstLight.Game.Localization;
using Quantum.Systems.Bots;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;

namespace FirstLight.Game.StateMachines
{
	public interface IGameStateMachine
	{
		string GetCurrentStateDebug();

		void Run();
	}

	/// <summary>
	/// The State Machine that controls the entire flow of the game
	/// </summary>
	public class GameStateMachine : IGameStateMachine
	{
		private readonly GameLovers.StatechartMachine.Statechart _statechart;
		private readonly AuthenticationState _authenticationState;
		private readonly AudioState _audioState;
		private readonly NetworkState _networkState;
		private readonly TutorialState _tutorialState;
		private readonly GameLogic _gameLogic;
		private readonly CoreLoopState _coreLoopState;
		private readonly ReconnectionState _reconnection;
		private readonly IGameServices _services;

		public GameStateMachine(GameLogic gameLogic, IGameServices services,
								IInternalGameNetworkService networkService,
								IAssetAdderService assetAdderService)
		{
			_gameLogic = gameLogic; // TODO: Should not be here
			_services = services;
			_authenticationState = new AuthenticationState(_gameLogic, services, services.DataService, Trigger);
			_audioState = new AudioState(gameLogic, services, Trigger);
			_reconnection = new ReconnectionState(services, gameLogic, networkService, Trigger);
			_networkState = new NetworkState(gameLogic, services, networkService, Trigger);
			_tutorialState = new TutorialState(services, Trigger);
			_coreLoopState = new CoreLoopState(_reconnection, services, gameLogic, services.DataService, networkService, assetAdderService, Trigger,
				services.RoomService);
			_statechart = new GameLovers.StatechartMachine.Statechart(Setup);
		}

		/// <inheritdoc cref="IStatechart.Run"/>
		public void Run()
		{
			_statechart.Run();
		}

		private void Trigger(IStatechartEvent eventTrigger)
		{
			_statechart.Trigger(eventTrigger);
		}

		public string GetCurrentStateDebug()
		{
			#if UNITY_EDITOR
			return _statechart.CurrentState;
#else
			return "Runtime state tracing unavailable";
#endif
		}

		private void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var internetCheck = stateFactory.Choice("Internet Check");
			var authentication = stateFactory.Nest("Authentication");
			var core = stateFactory.Split("Core");

			initial.Transition().Target(internetCheck);
			initial.OnExit(() =>
			{
				if (BotCharacterSystem.Debug)
				{
					_services.UIService.OpenScreen<WorldDebugPresenter>().Forget();
				}
			});

			internetCheck.Transition().Condition(NetworkUtils.IsOffline).OnTransition(OpenNoInternetPopUp).Target(final);
			internetCheck.Transition().Target(authentication);

			authentication.Nest(_authenticationState.Setup).Target(core);

			core.Split(_networkState.Setup, _audioState.Setup, _tutorialState.Setup, _coreLoopState.Setup).Target(final);
		}

		private void OpenNoInternetPopUp()
		{
#if UNITY_EDITOR
			var desc = string.Format(ScriptLocalization.General.NoInternet);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.ExitGame,
				ButtonOnClick = () => { _services.QuitGame("Closing no internet popup"); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);
#else
			var button = new GameLovers.MobileServices.NativeUi.AlertButton
			{
				Callback = () => { _services.QuitGame("Closing no internet popup"); },
				Style = GameLovers.MobileServices.NativeUi.AlertButtonStyle.Cancel,
				Text = ScriptLocalization.General.ExitGame
			};

			GameLovers.MobileServices.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet,
				ScriptLocalization.General.NoInternetDescription, button);
#endif
		}
	}
}
