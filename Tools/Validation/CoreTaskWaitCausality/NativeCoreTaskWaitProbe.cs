using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using GameLovers.Services;
using GameLovers.StatechartMachine;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NativeCoreTaskWaitProbe
{
	private interface IMenuBinding { }
	private sealed class MenuBinding : IMenuBinding { }
	[Serializable] private sealed class Result { public string Name; public bool Boundary; public bool AlreadyConnected; public int BindAttempts; public int Errors; public int CoreEntries; }
	private sealed class Fixture
	{
		public readonly Result Result;
		public bool Connected;
		public readonly Statechart Chart;
		private readonly Installer _installer = new Installer();
		private readonly UniTaskCompletionSource _menuLoading = new UniTaskCompletionSource();
		public Fixture(bool boundary, bool connected)
		{
			Connected = connected;
			Result = new Result { Name = boundary ? "BeginNextFrame" : "Original", Boundary = boundary, AlreadyConnected = connected };
			Chart = new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");
				var core = factory.Split("Core");
				initial.Transition().Target(core);
				core.Split(IdleBranch, IdleBranch, IdleBranch, SetupCoreLoop).Target(final);
			});
		}
		private void IdleBranch(IStateFactory factory)
		{
			var initial = factory.Initial("Other Initial");
			factory.Final("Other Final");
			var state = factory.State("Other Connected");
			initial.Transition().Target(state);
		}
		private void SetupCoreLoop(IStateFactory factory)
		{
			var initial = factory.Initial("Core Initial");
			factory.Final("Core Final");
			var connection = factory.TaskWait("Connection Wait");
			var reconnection = factory.Nest("Reconnection");
			var firstMatch = factory.Choice("First Match Check");
			var menu = factory.Nest("Main Menu");
			var stay = factory.State("Unused Pending Match");
			initial.OnExit(() => Result.CoreEntries++);
			initial.Transition().Target(connection);
			connection.WaitingFor(WaitForConnection).Target(reconnection);
			reconnection.Nest(f =>
			{
				var start = f.Initial("Reconnect Initial");
				var end = f.Final("Reconnect Final");
				var choice = f.Choice("Check for Reconnection");
				var pending = f.State("Pending Match");
				start.Transition().Target(choice);
				choice.Transition().Condition(() => false).Target(pending);
				choice.Transition().Target(end);
			}).Target(firstMatch);
			firstMatch.Transition().Condition(() => true).Target(menu);
			firstMatch.Transition().Target(stay);
			menu.Nest(f =>
			{
				var start = f.Initial("Menu Initial");
				var final = f.Final("Menu Final");
				var loading = f.TaskWait("Main Menu Loading");
				var loaded = f.State("Main Menu Loaded");
				start.Transition().Target(loading);
				loading.WaitingFor(LoadMenu).Target(loaded);
			}).Target(stay);
		}
		private async UniTask WaitForConnection()
		{
			if (Result.Boundary) await UniTask.NextFrame();
			while (!Connected) await UniTask.Yield();
		}
		private UniTask LoadMenu()
		{
			Result.BindAttempts++;
			_installer.Bind<IMenuBinding>(new MenuBinding());
			// The real application's scene/audio load is still pending after Bind.
			return _menuLoading.Task;
		}
	}
	private static readonly List<Result> Results = new List<Result>();
	private static Fixture _current;
	private static int _frame;
	private static int _case;
	private static int _lastFrame;
	private const string Pending = "NativeCoreTaskWaitProbe.Pending";
	static NativeCoreTaskWaitProbe() { if (SessionState.GetBool(Pending, false)) EditorApplication.delayCall += Begin; }
	public static void Run()
	{
		if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Owned native project required");
		SessionState.SetBool(Pending, true);
		UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
		EditorApplication.EnterPlaymode();
	}
	private static void Begin()
	{
		if (!EditorApplication.isPlaying) return;
		Application.logMessageReceived += Capture;
		StartCase(false, true);
		EditorApplication.update += Tick;
	}
	private static void Capture(string text, string stack, LogType type)
	{
		if (_current != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
			(text.Contains("Exception in the state 'Main Menu Loading'") || text.Contains("NativeCoreTaskWaitProbe+IMenuBinding"))) _current.Result.Errors++;
	}
	private static void StartCase(bool boundary, bool connected)
	{
		_current = new Fixture(boundary, connected);
		_frame = 0;
		_lastFrame = Time.frameCount;
		_current.Chart.Run();
	}
	private static void Tick()
	{
		try
		{
			if (_lastFrame == Time.frameCount) return;
			_lastFrame = Time.frameCount;
			if (++_frame == 3 && !_current.Connected) _current.Connected = true;
			if (_frame < 8) return;
			var row = _current.Result;
			Require(row.BindAttempts == (row.Boundary || !row.AlreadyConnected ? 1 : 2), "Unexpected native Bind count " + JsonUtility.ToJson(row));
			Require(row.Errors == (row.Boundary || !row.AlreadyConnected ? 0 : 2), "Unexpected native exception count " + JsonUtility.ToJson(row));
			_current.Chart.Pause();
			Results.Add(row);
			if (++_case == 1) { StartCase(true, true); return; }
			if (_case == 2) { StartCase(true, false); return; }
			File.WriteAllText("native-core-taskwait-proof.json", "{\"Rows\":[" + string.Join(",", Results.ConvertAll(JsonUtility.ToJson)) + "]}");
			Debug.Log("[Native Core TaskWait] PASS original2binds/2errors; NextFrame connected+waiting1bind/0errors each");
			Finish(0);
		}
		catch (Exception exception) { Debug.LogException(exception); Finish(1); }
	}
	private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
	private static void Finish(int code)
	{
		SessionState.SetBool(Pending, false);
		EditorApplication.update -= Tick;
		Application.logMessageReceived -= Capture;
		EditorApplication.Exit(code);
	}
}
