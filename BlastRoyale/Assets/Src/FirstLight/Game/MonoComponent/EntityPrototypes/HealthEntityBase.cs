using FirstLight.Inspector;
using FirstLight.Game.Messages;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <inheritdoc />
	/// <remarks>
	/// Implement this class for entity prototypes with <seeref name="Health"/> during the game simulations
	/// </remarks>
	public abstract class HealthEntityBase : EntityBase
	{
		[SerializeField, InspectorField(Required = true)] private Transform _healthBarAnchor;

		/// <summary>
		/// The <see cref="Transform"/> anchor values to attach the avatars health bar
		/// </summary>
		public Transform HealthBarAnchor => _healthBarAnchor;
	}
}
