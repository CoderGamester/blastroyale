using System;
using System.IO;
using FirstLight.Game.UI.Timeline;
using UITTimeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class NativeTintRuntimeProbe
{
	private const string Key = "NativeTintRuntimeProbe.Pending";
	private static GameObject _go;
	private static PanelSettings _panel;
	private static VisualElement _target;
	private static VisualElement _control;
	private static PlayableGraph _graph;
	private static PlayableGraph _controlGraph;
	private static Playable _mixer;
	private static int _frame;
	private static int _stage;
	private static int _errors;
	static NativeTintRuntimeProbe()
	{
		if (SessionState.GetBool(Key, false)) EditorApplication.delayCall += Initialize;
	}
	public static void Run()
	{
		if (!Application.dataPath.Contains("/.local/tint-timeline-project/")) throw new InvalidOperationException("Owned project required");
		SessionState.SetBool(Key, true);
		// Prime the actual native default Search index while still in Edit Mode.
		// Entering Play before its delayed startup creation otherwise triggers a SearchDatabase editor exception.
		var search = Type.GetType("UnityEditor.Search.SearchDatabase, UnityEditor.QuickSearchModule", true);
		search.GetMethod("GetDefaultSearchDatabase", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
		EditorApplication.EnterPlaymode();
	}
	private static void Initialize()
	{
		if (!EditorApplication.isPlaying) return;
		Application.logMessageReceived += OnLog;
		_go = new GameObject("Actual runtime UI Toolkit tint probe");
		_panel = ScriptableObject.CreateInstance<PanelSettings>();
		_panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
		if (_panel.themeStyleSheet == null) throw new InvalidOperationException("Actual native theme stylesheet is required");
		var document = _go.AddComponent<UIDocument>();
		document.panelSettings = _panel;
		_target = new VisualElement { name = "Target" };
		_control = new VisualElement { name = "Control" };
		_target.style.unityBackgroundImageTintColor = Color.white;
		_control.style.unityBackgroundImageTintColor = Color.white;
		document.rootVisualElement.Add(_target);
		document.rootVisualElement.Add(_control);
		EditorApplication.update += Update;
	}
	private static void OnLog(string message, string stack, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++;
	}
	private static void Update()
	{
		try
		{
			if (++_frame < 4) return;
			_frame = 0;
			switch (_stage++)
			{
				case 0:
					Require(_target.panel != null && _control.panel != null, "Real UIDocument elements are not attached to a runtime panel");
					Build(true, out _graph, out _mixer);
					Build(false, out _controlGraph, out _);
					break;
				case 1:
					Require(ColorDistance(_control.style.unityBackgroundImageTintColor.value, Color.white) < 0.0001f, "Actual upstream negative control unexpectedly handles tint");
					Require(ColorDistance(_target.style.unityBackgroundImageTintColor.value, new Color(.5f,.5f,.5f,1)) < 0.0001f, "Actual half-weight tint did not blend with original white");
					Require(Mathf.Abs(_target.style.scale.value.value.x - 1.25f) < 0.0001f, "Actual package scale clip was lost in composition");
					_mixer.SetInputWeight(0, 1f);
					break;
				case 2:
					Require(ColorDistance(_target.style.unityBackgroundImageTintColor.value, Color.black) < 0.0001f, "Actual full-weight tint not black");
					_mixer.SetInputWeight(0, 0f);
					break;
				case 3:
					Require(ColorDistance(_target.style.unityBackgroundImageTintColor.value, Color.white) < 0.0001f, "Zero tint weight did not restore baseline");
					_mixer.SetInputWeight(0, 1f);
					break;
				case 4:
					Require(ColorDistance(_target.style.unityBackgroundImageTintColor.value, Color.black) < 0.0001f, "Second full-weight tint not black");
					_graph.Destroy();
					break;
				case 5:
					Require(ColorDistance(_target.style.unityBackgroundImageTintColor.value, Color.white) < 0.0001f, "Actual runtime graph destruction did not restore original tint");
					Require(_errors == 0, "Actual runtime emitted errors/exceptions: " + _errors);
					File.WriteAllText("native-tint-runtime-proof.json", "{\"AttachedRuntimePanel\":true,\"ActualUpstreamIgnoresTint\":true,\"HalfFullZeroWeightsPass\":true,\"NativeScaleClipPreserved\":true,\"DestroyedGraphRestoresTint\":true,\"Errors\":0}");
					Debug.Log("[Native Tint Runtime] PASS attached runtime panel, upstream negative, weighted tint, native scale, graph destruction");
					Finish(0);
					break;
			}
		}
		catch (Exception exception) { Debug.LogException(exception); Finish(1); }
	}
	private static void Build(bool extended, out PlayableGraph graph, out Playable mixer)
	{
		graph = PlayableGraph.Create(extended ? "Native tint extension" : "Actual upstream negative control");
		var track = extended ? (UITVisualElementTrack) ScriptableObject.CreateInstance<BackgroundImageTintTrack>() : ScriptableObject.CreateInstance<UITVisualElementTrack>();
		track.name = extended ? "#Target" : "#Control";
		mixer = track.CreateTrackMixer(graph, _go, 2);
		var tint = ScriptableObject.CreateInstance<BackgroundImageTintClip>();
		tint._template.Color = Color.black;
		var scale = ScriptPlayable<UITScaleBehaviour>.Create(graph);
		scale.GetBehaviour().Scale = new Vector2(.5f,.5f);
		graph.Connect(tint.CreatePlayable(graph,_go),0,mixer,0);
		graph.Connect(scale,0,mixer,1);
		mixer.SetInputWeight(0,.5f);
		mixer.SetInputWeight(1,.5f);
		var output = ScriptPlayableOutput.Create(graph,"Actual runtime UI");
		output.SetSourcePlayable(mixer);
		graph.Play();
	}
	private static float ColorDistance(Color a, Color b) => Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a);
	private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
	private static void Finish(int code)
	{
		SessionState.SetBool(Key,false);
		EditorApplication.update -= Update;
		Application.logMessageReceived -= OnLog;
		if (_graph.IsValid()) _graph.Destroy();
		if (_controlGraph.IsValid()) _controlGraph.Destroy();
		if (_go != null) UnityEngine.Object.Destroy(_go);
		if (_panel != null) UnityEngine.Object.Destroy(_panel);
		EditorApplication.Exit(code);
	}
}
