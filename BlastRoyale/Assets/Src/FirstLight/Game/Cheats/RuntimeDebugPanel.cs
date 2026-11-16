using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.Cheats
{
	public sealed class DebugOption
	{
		public string Name { get; private set; }
		public string Category { get; private set; }
		public int Priority { get; private set; }
		public Type ValueType { get; private set; }
		public Func<object> Read { get; private set; }
		public Action<object> Write { get; private set; }
		public System.Action Invoke { get; private set; }

		public static DebugOption Action(string name, System.Action action, string category, int priority = 0)
			=> new DebugOption {Name = name, Category = category, Priority = priority, Invoke = action};

		public static DebugOption Value<T>(string name, Func<T> read, Action<T> write, string category, int priority = 0)
			=> new DebugOption {Name = name, Category = category, Priority = priority, ValueType = typeof(T), Read = () => read(), Write = write == null ? null : value => write((T) value)};

		public static DebugOption Property(object owner, PropertyInfo property, string category)
			=> new DebugOption
			{
				Name = property.Name, Category = category, Priority = property.GetCustomAttribute<DebugOptions.SortAttribute>()?.Priority ?? 0, ValueType = property.PropertyType,
				Read = () => property.GetValue(owner), Write = property.CanWrite ? value => property.SetValue(owner, value) : null
			};
	}

	/// <summary>Local diagnostic tools available in the Editor and development players.</summary>
	public sealed class RuntimeDebugPanel : MonoBehaviour
	{
#if DEBUG
		private static readonly List<DebugOption> Options = new List<DebugOption>();
		private static readonly Queue<string> Logs = new Queue<string>();
		private static RuntimeDebugPanel _instance;
		private readonly Dictionary<DebugOption, string> _inputs = new Dictionary<DebugOption, string>();
		private Vector2 _scroll;
		private Rect _window = new Rect(20, 60, 640, 640);
		private bool _visible;
		private string _status;
		private bool _showLogs;
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
#if DEBUG
			Options.Clear();
			Logs.Clear();
			_instance = null;
#endif
		}

		public static void Initialize()
		{
#if DEBUG
			if (_instance != null) return;
			var owner = new GameObject("Development Diagnostics");
			DontDestroyOnLoad(owner);
			_instance = owner.AddComponent<RuntimeDebugPanel>();
#endif
		}

		public static void AddOptions(IEnumerable<DebugOption> options)
		{
#if DEBUG
			Options.AddRange(options);
#endif
		}

		public static void RegisterOptions(object owner)
		{
#if DEBUG
			foreach (var member in owner.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
			{
				var category = member.GetCustomAttribute<CategoryAttribute>();
				if (category == null) continue;
				if (member is PropertyInfo property && property.CanRead && property.GetIndexParameters().Length == 0)
					Options.Add(DebugOption.Property(owner, property, category.Category));
				else if (member is MethodInfo method && method.GetParameters().Length == 0)
					Options.Add(DebugOption.Action(member.Name, () => method.Invoke(method.IsStatic ? null : owner, null), category.Category, member.GetCustomAttribute<DebugOptions.SortAttribute>()?.Priority ?? 0));
			}
#endif
		}

#if DEBUG
		private void OnEnable() => Application.logMessageReceived += CaptureLog;
		private void OnDisable() => Application.logMessageReceived -= CaptureLog;

		private static void CaptureLog(string message, string stackTrace, LogType type)
		{
			if (Logs.Count == 200) Logs.Dequeue();
			Logs.Enqueue(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + " [" + type + "] " + message + "\n" + stackTrace);
		}
#endif

		public static void SaveReport(string description)
		{
#if DEBUG
			var directory = Path.Combine(Application.persistentDataPath, "Diagnostics");
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, "report-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + ".txt");
			var report = new StringBuilder(description);
			report.AppendLine("\nClient version: " + VersionUtils.VersionInternal);
			report.AppendLine("Client commit: " + VersionUtils.Commit);
			report.AppendLine("Server build: " + VersionUtils.ServerBuildNumber);
			report.AppendLine("OS: " + SystemInfo.operatingSystem);
			report.AppendLine("Device: " + SystemInfo.deviceModel);
			foreach (var log in Logs.ToArray()) report.AppendLine(log);
			File.WriteAllText(path, report.ToString());
			if (_instance != null) _instance._status = "Saved report: " + path;
#endif
		}

#if DEBUG
		private void OnGUI()
		{
			if (GUI.Button(new Rect(10, 10, 130, 38), _visible ? "Hide diagnostics" : "Diagnostics")) _visible = !_visible;
			if (!_visible) return;
			_window.width = Mathf.Min(640, Screen.width - 40);
			_window.height = Mathf.Min(640, Screen.height - 80);
			_window = GUILayout.Window(GetEntityId().GetHashCode(), _window, DrawWindow, "Development Diagnostics");
		}

		private void DrawWindow(int windowId)
		{
			GUILayout.Label("Build: " + VersionUtils.VersionInternal + "   FPS: " + Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f)));
			GUILayout.BeginHorizontal();
			_showLogs = GUILayout.Toggle(_showLogs, "Console");
			if (GUILayout.Button("Save local report")) SaveReport("Manual development report");
			if (GUILayout.Button("Close")) _visible = false;
			GUILayout.EndHorizontal();
			if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);
			_scroll = GUILayout.BeginScrollView(_scroll);
			if (_showLogs)
			{
				foreach (var line in Logs.ToArray()) GUILayout.Label(line);
			}
			else
			{
				foreach (var group in Options.GroupBy(option => option.Category).OrderBy(group => group.Key))
				{
					GUILayout.Label(group.Key);
					foreach (var option in group.OrderBy(option => option.Priority)) DrawOption(option);
				}
			}
			GUILayout.EndScrollView();
			GUI.DragWindow(new Rect(0, 0, 640, 20));
		}

		private void DrawOption(DebugOption option)
		{
			GUILayout.BeginHorizontal();
			try
			{
				if (option.Invoke != null)
				{
					if (GUILayout.Button(option.Name)) option.Invoke();
				}
				else
				{
					var value = option.Read();
					GUILayout.Label(option.Name, GUILayout.Width(210));
					if (option.Write == null) GUILayout.Label(Convert.ToString(value, CultureInfo.InvariantCulture));
					else if (option.ValueType == typeof(bool))
					{
						var next = GUILayout.Toggle((bool) value, "");
						if (next != (bool) value) option.Write(next);
					}
					else
					{
						if (!_inputs.TryGetValue(option, out var input)) input = Convert.ToString(value, CultureInfo.InvariantCulture);
						_inputs[option] = GUILayout.TextField(input ?? "");
						if (GUILayout.Button("Set", GUILayout.Width(50)))
							option.Write(option.ValueType.IsEnum ? Enum.Parse(option.ValueType, _inputs[option], true) : Convert.ChangeType(_inputs[option], option.ValueType, CultureInfo.InvariantCulture));
					}
				}
			}
			catch (Exception exception)
			{
				_status = (exception.InnerException ?? exception).Message;
			}
			GUILayout.EndHorizontal();
		}
#endif
	}
}
