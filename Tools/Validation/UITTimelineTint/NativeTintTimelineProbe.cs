using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Game.UI.Timeline;
using UITTimeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

public static class NativeTintTimelineProbe
{
	[Serializable] private sealed class Report { public string Unity; public int TintClips; public int FamilyTracks; public int Samples; public List<Row> Rows = new List<Row>(); }
	[Serializable] private sealed class Row { public string Asset; public string Track; public double Time; public Color Actual; public Color Expected; public float TotalWeight; }
	private static IEnumerable<TrackAsset> Descendants(TrackAsset track)
	{
		yield return track;
		foreach (var child in track.GetChildTracks()) foreach (var descendant in Descendants(child)) yield return descendant;
	}
	private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
	public static void Run()
	{
		Require(Application.dataPath.Contains("/.local/tint-timeline-project/"), "Isolated owned project required");
		var report = new Report { Unity = Application.unityVersion };
		var assets = new[] { "SeasonBannerAnimation", "Rewards-Equipment", "ProductBundleBannerAnimation", "GameModeNewEvent" };
		foreach (var assetName in assets)
		{
			var path = "Assets/Art/UI/Animations/" + assetName + ".playable";
			var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
			Require(timeline != null, "Missing native Timeline " + path);
			var objects = AssetDatabase.LoadAllAssetsAtPath(path);
			Require(objects.All(o => o != null), "Missing Timeline subasset " + path);
			var clips = objects.OfType<BackgroundImageTintClip>().ToArray();
			Require(clips.Length == 2, "Expected two original tint subassets in " + path);
			report.TintClips += clips.Length;
			var tracks = timeline.GetRootTracks().SelectMany(Descendants).ToArray();
			report.FamilyTracks += tracks.Count(t => t is BackgroundImageTintTrack);
			var roots = tracks.OfType<BackgroundImageTintTrack>().Where(t => !(t.parent is TrackAsset)).ToArray();
			Require(roots.Length == 1, "Expected one tint root family in " + path);
			var rootTrack = roots[0];
			Require(Descendants(rootTrack).All(t => t is BackgroundImageTintTrack), "Exact native child-track type identity lost");
			Require(Descendants(rootTrack).All(t => !t.hasCurves), "Authored track curves need separate layer support");
			var tintClips = Descendants(rootTrack).SelectMany(t => t.GetClips()).Where(c => c.asset is BackgroundImageTintClip).ToArray();
			Require(tintClips.Length == 2, "Native Timeline clip references lost");
			var panel = ScriptableObject.CreateInstance<PanelSettings>();
			panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
			var go = new GameObject("Native authored tint Timeline probe");
			try
			{
				var document = go.AddComponent<UIDocument>();
				document.panelSettings = panel;
				Require(document.rootVisualElement != null, "Actual UIDocument root unavailable");
				var element = new VisualElement { name = rootTrack.name.Substring(1) };
				element.style.unityBackgroundImageTintColor = Color.white;
				document.rootVisualElement.Add(element);
				var director = go.AddComponent<PlayableDirector>();
				director.playOnAwake = false;
				director.timeUpdateMode = DirectorUpdateMode.Manual;
				director.playableAsset = timeline;
				director.Play();
				foreach (var clip in tintClips)
				{
					foreach (var fraction in new[] { 0.1, 0.5, 0.9 })
					{
						var time = clip.start + clip.duration * fraction;
						director.time = time;
						director.Evaluate();
						var mixers = new List<Playable>();
						for (var output = 0; output < director.playableGraph.GetOutputCount(); output++)
							Collect(director.playableGraph.GetOutput(output).GetSourcePlayable(), mixers);
						var playable = mixers.Distinct().Single(p => ((ScriptPlayable<BackgroundImageTintMixerBehaviour>) p).GetBehaviour().NativeMixer.Elements.Contains(element));
						var expected = Color.clear;
						var total = 0f;
						for (var input = 0; input < playable.GetInputCount(); input++)
						{
							var behaviour = ((ScriptPlayable<UITBehaviour>) playable.GetInput(input)).GetBehaviour();
							if (!(behaviour is BackgroundImageTintBehaviour tint)) continue;
							var weight = playable.GetInputWeight(input);
							expected += tint.Color * weight;
							total += weight;
						}
						Require(total > 0f, "Original tint has zero native contribution at " + time);
						if (total > 1f) expected /= total;
						else expected += Color.white * (1f - total);
						var actual = element.style.unityBackgroundImageTintColor.value;
						Require(Distance(actual, expected) < 0.0001f, "Native tint blend mismatch " + path + " at " + time + ": " + actual + " != " + expected);
						report.Rows.Add(new Row { Asset = path, Track = rootTrack.name, Time = time, Actual = actual, Expected = expected, TotalWeight = total });
						report.Samples++;
					}
				}
				director.Stop();
				// Manual Editor evaluation does not issue runtime destruction callbacks;
				// restoration is verified by the actual attached-panel Play Mode probe.
			}
			finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(panel); }
		}
		Require(report.TintClips == 8 && report.FamilyTracks == 11 && report.Samples == 24, "Acceptance inventory mismatch");
		File.WriteAllText("native-tint-timeline-proof.json", JsonUtility.ToJson(report, true));
		Debug.Log("[Native Tint Timeline] PASS: original8clips,11familytracks,24native graph/readback samples");
	}
	private static float Distance(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
	private static void Collect(Playable playable, ICollection<Playable> result)
	{
		if (!playable.IsValid()) return;
		if (playable.GetPlayableType() == typeof(BackgroundImageTintMixerBehaviour)) result.Add(playable);
		for (var input = 0; input < playable.GetInputCount(); input++) Collect(playable.GetInput(input), result);
	}
}
