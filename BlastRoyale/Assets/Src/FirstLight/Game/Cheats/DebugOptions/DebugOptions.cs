using System.ComponentModel;
using FirstLight.Game.Cheats;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public partial class DebugOptions : INotifyPropertyChanged
{
	public static DebugOptions Current { get; private set; }
	public event PropertyChangedEventHandler PropertyChanged;

#if DEBUG
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void OnStartup()
	{
		Current = new DebugOptions();
		RuntimeDebugPanel.Initialize();
		RuntimeDebugPanel.RegisterOptions(Current);
		AddCurrencyCheats();
		AddQuantumCheats();
	}
#endif

	public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public void SendQuietBugReport(string description) => RuntimeDebugPanel.SaveReport(description);

	[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Method)]
	public sealed class SortAttribute : System.Attribute
	{
		public int Priority { get; }
		public SortAttribute(int priority) => Priority = priority;
	}
}
