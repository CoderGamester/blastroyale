using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Sends an event when the player has landed on the ground.
	/// </summary>
	[Serializable]
	public unsafe class SkydiveStopAction : AIAction
	{
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);

			f.Events.OnLocalPlayerSkydiveLand(player->Player, e);
			f.Events.OnPlayerSkydiveLand(player->Player, e);
		}
	}
}
