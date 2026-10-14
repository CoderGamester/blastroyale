using Quantum;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// A debug window that displays the current stat total for each stat during play mode
	///
	/// NOTE: Only works in Play mode.
	/// </summary>
	public class StatDebuggerWindow : EditorWindow
	{
		private PlayerRef _LocalPlayer;
		private bool _Initialized = false;

		[Header("Defence Stats")]
		public float Health = 0;
		public float ShieldCapacity = 0;
		public float MaxShieldCapacity = 0;
		public float Armor = 0;

		[Header("Offence Stats")]
		public float Power = 0;
		public float AttackRange = 0;

		[Header("Utility Stats")]
		public float PickupSpeed = 0;
		public float Speed = 0;
		public float AmmoCapacity = 0;

		[MenuItem("FLG/Stat Debugger")]
		private static void OpenWindow()
		{
			GetWindow<StatDebuggerWindow>("Stat Debugger").Show();
		}



		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Live stats update from the local player's Quantum events while in Play Mode.", MessageType.Info);
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.LabelField("Defence Stats", EditorStyles.boldLabel);
				EditorGUILayout.FloatField("Health", Health);
				EditorGUILayout.FloatField("Shield Capacity", ShieldCapacity);
				EditorGUILayout.FloatField("Max Shield Capacity", MaxShieldCapacity);
				EditorGUILayout.FloatField("Armor", Armor);
				EditorGUILayout.LabelField("Offence Stats", EditorStyles.boldLabel);
				EditorGUILayout.FloatField("Power", Power);
				EditorGUILayout.FloatField("Attack Range", AttackRange);
				EditorGUILayout.LabelField("Utility Stats", EditorStyles.boldLabel);
				EditorGUILayout.FloatField("Pickup Speed", PickupSpeed);
				EditorGUILayout.FloatField("Speed", Speed);
				EditorGUILayout.FloatField("Ammo Capacity", AmmoCapacity);
			}

			if (Application.isPlaying && !_Initialized)
			{
				QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, GetLocalPlayer);
				QuantumEvent.Subscribe<EventOnPlayerEquipmentStatsChanged>(this, UpdateStats);
				_Initialized = true;
			}
			if (!Application.isPlaying && _Initialized)
			{
				QuantumEvent.UnsubscribeListener(this);
				_Initialized = false;
			}
		}

		private void OnDisable()
		{
			QuantumEvent.UnsubscribeListener(this);
			_Initialized = false;
		}

		public void GetLocalPlayer(EventOnLocalPlayerSpawned callback)
		{
			_LocalPlayer = callback.Player;
		}

		public void UpdateStats(EventOnPlayerEquipmentStatsChanged callback)
		{
			if (callback.Player != _LocalPlayer)
				return;

			Health = callback.CurrentStats.GetStatData(StatType.Health).StatValue.AsFloat;
			ShieldCapacity = callback.CurrentStats.GetStatData(StatType.Shield).StatValue.AsFloat;
			MaxShieldCapacity = callback.CurrentStats.GetStatData(StatType.Shield).BaseValue.AsFloat;
			Armor = callback.CurrentStats.GetStatData(StatType.Armour).StatValue.AsFloat;

			AttackRange = callback.CurrentStats.GetStatData(StatType.AttackRange).StatValue.AsFloat;
			Power = callback.CurrentStats.GetStatData(StatType.Power).StatValue.AsFloat;

			Speed = callback.CurrentStats.GetStatData(StatType.Speed).StatValue.AsFloat;
			PickupSpeed = callback.CurrentStats.GetStatData(StatType.PickupSpeed).StatValue.AsFloat;
			AmmoCapacity = callback.CurrentStats.GetStatData(StatType.AmmoCapacity).StatValue.AsFloat;

			Repaint();
		}

	}
}
