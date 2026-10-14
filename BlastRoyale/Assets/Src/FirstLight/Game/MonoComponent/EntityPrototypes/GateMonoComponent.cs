using FirstLight.Inspector;
using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="Gate"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class GateMonoComponent : EntityBase
	{
		[SerializeField, InspectorField(Required = true)] private GateBarrierViewMonoComponent _gateView;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_gateView.SetEntityView(game, EntityView);
		}
	}
}
