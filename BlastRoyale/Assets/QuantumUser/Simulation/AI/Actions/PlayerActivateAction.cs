using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Teleports the player and spawns them
	/// </summary>
	[Serializable]

	public unsafe class PlayerActivateAction : AIAction
	{
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			player->Activate(f, e);
		}
	}
}
