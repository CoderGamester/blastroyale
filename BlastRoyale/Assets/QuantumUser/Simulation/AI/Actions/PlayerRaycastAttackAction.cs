using System;
using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	/// <remarks>
	/// Use <see cref="PlayerProjectileAttackAction"/>
	/// </remarks>
	[Serializable]

	public unsafe class PlayerRaycastAttackAction : AIAction
	{
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			if (!ReviveSystem.IsKnockedOut(f, e))
			{
				ProjectileSystem.Shoot(f, e);
			}
		}
	}
}
