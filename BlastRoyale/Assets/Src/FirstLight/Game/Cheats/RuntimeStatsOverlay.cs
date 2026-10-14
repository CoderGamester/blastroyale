using Quantum;
using UnityEngine;

namespace FirstLight.Game.Cheats
{
	/// <summary>Development-only simulation counters using the active native Quantum session.</summary>
	public sealed class RuntimeStatsOverlay : MonoBehaviour
	{
		private static RuntimeStatsOverlay _instance;
		private bool _visible;
		private Rect _window = new Rect(16, 76, 300, 170);

		public static void Toggle()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (!_instance)
			{
				var owner = new GameObject("Development simulation counters");
				DontDestroyOnLoad(owner);
				_instance = owner.AddComponent<RuntimeStatsOverlay>();
			}
			_instance._visible = !_instance._visible;
#endif
		}

		private void OnDestroy()
		{
			if (_instance == this) _instance = null;
		}

		private void OnGUI()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (_visible) _window = GUILayout.Window(GetInstanceID(), _window, DrawCounters, "Simulation counters");
#endif
		}

		private void DrawCounters(int windowId)
		{
			var runner = QuantumRunner.Default;
			if (runner?.Session == null)
			{
				GUILayout.Label("No active simulation");
			}
			else
			{
				GUILayout.Label("State: " + runner.State);
				GUILayout.Label("Verified tick: " + (runner.Game?.Frames.Verified?.Number ?? 0));
				GUILayout.Label("Predicted tick: " + (runner.Game?.Frames.Predicted?.Number ?? 0));
				GUILayout.Label("Ping: " + runner.Session.Stats.Ping + " ms");
				GUILayout.Label(runner.Session.IsStalling ? "Waiting for input" : "Input available");
			}
			GUI.DragWindow();
		}
	}
}
