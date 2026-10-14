using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using FirstLight.Game.Presenters;
using FirstLight.Game.Domains.HomeScreen.UI;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using Quantum;
using Unity.Services.Authentication;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using LogType = UnityEngine.LogType;
using Debug = UnityEngine.Debug;
using Button = UnityEngine.UIElements.Button;

// Temporary Editor instrumentation: selects the actual UI action, then observes the real game.
// It does not create a runner, inject a login result, replace network services, or fabricate frames.
[InitializeOnLoad]
public static class MainMatchFlowSmoke
{
    private const string Active = "BlastModernization.MatchFlow.Active";
    private const string Deadline = "BlastModernization.MatchFlow.Deadline";
    private static readonly List<string> States = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static int _errors, _firstFrame = -1, _lastFrame = -1, _players, _bots;
    private static bool _selectedTutorial, _entered;
    private static bool _clickedPlay;
    private static bool _gameStarted;
    private static bool _submittedName;
    private static readonly bool Expert = Environment.GetEnvironmentVariable("BLAST_MATCH_FLOW_SELECTION") == "expert";
    private static double _stableSince;
    private static string[] _stateTree = Array.Empty<string>(), _screens = Array.Empty<string>();

    static MainMatchFlowSmoke() { if (SessionState.GetBool(Active, false)) Subscribe(); }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Editor already playing");
        SessionState.SetBool(Active, true);
        var timeout = int.TryParse(Environment.GetEnvironmentVariable("BLAST_MATCH_FLOW_SECONDS"), out var requested) ? requested : 180;
        SessionState.SetString(Deadline, DateTime.UtcNow.AddSeconds(timeout).Ticks.ToString());
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void Subscribe()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _errors++;
            if (Errors.Count < 8) Errors.Add(message);
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(Active, false)) return;
        try
        {
            if (EditorApplication.isPlaying)
            {
                _entered = true;
                if (MainInstaller.TryResolve<IGameStateMachine>(out var machine))
                {
                    var state = machine.GetCurrentStateDebug();
                    if (States.Count == 0 || States[States.Count - 1] != state)
                    {
                        States.Add(state);
                        Debug.Log("[Blast Match Flow] State: " + state);
                    }
                    var chart = Field(machine, "_statechart");
                    var tree = new List<string>();
                    WalkState(Field(chart, "_currentState"), "", tree);
                    _stateTree = tree.ToArray();
                }
                if (MainInstaller.TryResolve<IGameServices>(out var observedServices) &&
                    Field(observedServices.UIService, "_screens") is IDictionary screens)
                {
                    var names = new List<string>();
                    foreach (var key in screens.Keys) names.Add(((Type)key).Name);
                    _screens = names.ToArray();
                }
                if (!_selectedTutorial && MainInstaller.TryResolve<IGameServices>(out var services) &&
                    services.UIService.IsScreenOpen<SkipTutorialPopupPresenter>())
                {
                    var screen = services.UIService.GetScreen<SkipTutorialPopupPresenter>();
                    var button = Expert ? screen._skipButton : screen._tutorialButton;
                    if (button != null && button.enabledInHierarchy && button.panel != null)
                    {
                        // Invoke the installed Unity Clickable's actual click action on this authored control.
                        var invoke = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (invoke == null) throw new MissingMethodException("Native Unity Clickable.Invoke");
                        _selectedTutorial = true;
                        using (var evt = ClickEvent.GetPooled()) { evt.target = button; invoke.Invoke(button.clickable, new object[] { evt }); }
                        Debug.Log("[Blast Match Flow] Selected actual " + (Expert ? "Expert" : "Beginner") + " UI control");
                    }
                }
                if (Expert && !_clickedPlay && MainInstaller.TryResolve<IGameServices>(out var menuServices) &&
                    menuServices.UIService.IsScreenOpen<HomeScreenPresenter>())
                {
                    var button = menuServices.UIService.GetScreen<HomeScreenPresenter>().Root?.Q<FirstLight.Game.UIElements.LocalizedButton>("PlayButton");
                    if (button != null && button.enabledInHierarchy && button.panel != null)
                    {
                        var invoke = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        _clickedPlay = true;
                        using (var evt = ClickEvent.GetPooled()) { evt.target = button; invoke.Invoke(button.clickable, new object[] { evt }); }
                        Debug.Log("[Blast Match Flow] Selected actual home Play control");
                    }
                }
                if (Expert && !_submittedName && MainInstaller.TryResolve<IGameServices>(out var dialogServices) &&
                    dialogServices.UIService.IsScreenOpen<GenericInputDialogPresenter>())
                {
                    var dialog = dialogServices.UIService.GetScreen<GenericInputDialogPresenter>();
                    var input = dialog.Root?.Q<TextField>();
                    var button = dialog.Root?.Q<FirstLight.Game.UIElements.LocalizedButton>("ConfirmButton");
                    var callback = typeof(GenericDialogPresenterBase).GetField("_confirmCallback", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(dialog);
                    if (input != null && button != null && callback != null && button.enabledInHierarchy && button.panel != null)
                    {
                        input.value = "Blast tester";
                        _submittedName = true;
                        var invoke = typeof(Clickable).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        using (var evt = ClickEvent.GetPooled()) { evt.target = button; invoke.Invoke(button.clickable, new object[] { evt }); }
                        Debug.Log("[Blast Match Flow] Submitted actual name dialog");
                    }
                }
                var frame = QuantumRunner.Default?.Game?.Frames.Verified;
                if (frame != null)
                {
                    if (_firstFrame < 0) _firstFrame = frame.Number;
                    _lastFrame = frame.Number;
                    _players = 0; _bots = 0;
                    foreach (var pair in frame.GetComponentIterator<PlayerCharacter>()) _players++;
                    foreach (var pair in frame.GetComponentIterator<BotCharacter>()) _bots++;
                    _gameStarted = GameContainer.HasGameStarted(frame);
                    if (_gameStarted && _players > 0 && _lastFrame - _firstFrame >= 120)
                    {
                        if (_stableSince == 0) _stableSince = EditorApplication.timeSinceStartup;
                        if (EditorApplication.timeSinceStartup - _stableSince >= 5) { Finish(); return; }
                    }
                }
            }
            if (long.TryParse(SessionState.GetString(Deadline, ""), out var ticks) && DateTime.UtcNow.Ticks >= ticks) Finish();
        }
        catch (Exception error) { Debug.LogException(error); Finish(); }
    }

    [Serializable] private class Result
    {
        public bool entered_play_mode, selected_actual_tutorial_ui, native_authenticated, actual_runner_present, passed;
        public bool selected_actual_home_play;
        public bool native_game_started;
        public bool submitted_actual_name_dialog;
        public string selected_tutorial_option, native_map;
        public int error_logs, first_verified_frame, last_verified_frame, player_entities, bot_entities;
        public string[] states;
        public string[] errors;
        public string[] native_state_tree, active_screens;
    }

    private static void Finish()
    {
        var result = new Result {
            entered_play_mode = _entered, selected_actual_tutorial_ui = _selectedTutorial,
            native_authenticated = AuthenticationService.Instance.IsAuthorized,
            selected_actual_home_play = _clickedPlay,
            native_game_started = _gameStarted,
            submitted_actual_name_dialog = _submittedName,
            selected_tutorial_option = Expert ? "expert" : "beginner",
            native_map = QuantumRunner.Default?.Game?.Frames.Verified?.Map?.Path,
            actual_runner_present = QuantumRunner.Default != null,
            error_logs = _errors, first_verified_frame = _firstFrame, last_verified_frame = _lastFrame,
            player_entities = _players, bot_entities = _bots, states = States.ToArray(), errors = Errors.ToArray(),
            native_state_tree = _stateTree, active_screens = _screens
        };
        result.passed = result.entered_play_mode && result.native_authenticated && result.actual_runner_present &&
            _gameStarted && _lastFrame - _firstFrame >= 120 && _players > 0 && _errors == 0 && (!Expert || _clickedPlay);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Update;
        Application.logMessageReceived -= OnLog;
        var path = Environment.GetEnvironmentVariable("BLAST_MATCH_FLOW_RESULT");
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Set BLAST_MATCH_FLOW_RESULT");
        File.WriteAllText(path, JsonUtility.ToJson(result, true));
        Debug.Log("[Blast Match Flow] Passed=" + result.passed + "; verified frames=" + (_lastFrame - _firstFrame) + "; errors=" + _errors);
        EditorApplication.Exit(result.passed ? 0 : 2);
    }

    private static object Field(object instance, string name)
    {
        if (instance == null) return null;
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(instance);
        }
        return null;
    }

    private static void WalkState(object state, string path, List<string> result)
    {
        if (state == null || result.Count >= 32) return;
        var name = state.GetType().GetProperty("Name")?.GetValue(state) as string ?? state.GetType().Name;
        path = path + "/" + name;
        result.Add(path);
        if (Field(state, "_innerStatesData") is IEnumerable children)
            foreach (var child in children) WalkState(Field(child, "CurrenState"), path, result);
    }
}
