using FirstLight.Inspector;
using FirstLight.Game.Utils;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component spawns a <see cref="Weapon"/> on the it's current position
	/// </summary>
	public class FtueWeaponSpawnerMonoComponent : MonoBehaviour
	{
		[SerializeField, InspectorField(Required = true)] private PlayableDirector _timeline;
		[SerializeField] private GameId _weapon;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnPlayerWeaponCollected);
		}

		private void Start()
		{
			QuantumRunner.Default.Game.SendCommand(new CheatWeaponSpawnCommand
			{
				Position = transform.position.ToFPVector2(),
				Weapon = _weapon
			});
		}

		private void OnPlayerWeaponCollected(EventOnLocalPlayerWeaponChanged callback)
		{
			if (callback.WeaponSlot.Weapon.GameId == _weapon)
			{
				_timeline.playableGraph.PlayTimeline();
			}
		}
	}
}
