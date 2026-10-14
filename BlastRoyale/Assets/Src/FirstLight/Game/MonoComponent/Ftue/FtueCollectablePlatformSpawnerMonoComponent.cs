using FirstLight.Inspector;
using Quantum;
using Quantum.Commands;
using Quantum.Prototypes;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component spawns a <see cref="CollectablePlatformSpawner"/> on the it's current position
	/// </summary>
	public class FtueCollectablePlatformSpawnerMonoComponent : MonoBehaviour
	{
		[SerializeField, InspectorField(Required = true)] private CollectablePlatformSpawnerPrototype _collectable;

		private void Start()
		{
			QuantumRunner.Default.Game.SendCommand(new CheatCollectablePlatformSpawnCommand
			{
				Position = transform.position.ToFPVector3(),
				Collectable = _collectable.GameId,
				RespawnTimeInSec = _collectable.RespawnTimeInSec,
				InitialSpawnDelayInSec = _collectable.InitialSpawnDelayInSec
			});
		}
	}
}
