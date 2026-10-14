using FirstLight.Inspector;
using System;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using GameLovers.Services;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after the defined <see cref="ParticleSystem"/> is completed
	/// </remarks>
	public class ParticleVfxMonoBehaviour : VfxMonoBehaviour
	{
		[SerializeField, InspectorField(Required = true)] private ParticleSystem _particle;
		private float _particleLifeTime;

		private void Awake()
		{
			var main = _particle.main;
			_particleLifeTime = main.startLifetime.constant * (1 / main.simulationSpeed);
		}

		private void OnValidate()
		{
			_particle = _particle ? _particle : GetComponent<ParticleSystem>();
		}

		protected override void OnSpawned()
		{
			Despawner(_particleLifeTime).Forget();
		}
	}
}
