using FirstLight.Inspector;
using FirstLight.Game.MonoComponent.EntityViews;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// Responsible for instantiating the correct consumable platform model as a child of the consumable spawner entity view.
	/// </summary>
	public class VisibilityAreaMonoComponent : EntityBase
	{
		[SerializeField, InspectorField(Required = true)] private VisibilityAreaViewMonoComponent _view;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			_view.SetEntityView(game, EntityView);
		}
	}
}
