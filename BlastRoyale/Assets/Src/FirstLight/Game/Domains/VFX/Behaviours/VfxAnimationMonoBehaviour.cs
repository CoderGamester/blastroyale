using FirstLight.Inspector;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using GameLovers.Services;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx auto despawns after the defined <see cref="Animation"/> is completed
	/// </remarks>
	public class VfxAnimationMonoBehaviour : VfxMonoBehaviour
	{
		[SerializeField, InspectorField(Required = true)] private Animation _animation;

		private void OnValidate()
		{
			_animation = _animation ? _animation : GetComponent<Animation>();
		}

		protected override void OnSpawned()
		{
			Despawner(_animation.clip.length).Forget();
		}
	}
}
