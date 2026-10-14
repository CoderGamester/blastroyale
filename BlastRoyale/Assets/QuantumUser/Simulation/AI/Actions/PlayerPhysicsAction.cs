using System;
using Photon.Deterministic;

namespace Quantum
{
	/// <summary>
	/// Calls Move on the <see cref="CharacterController3D"/>, to force
	/// physics calculations. THis has to be running for physics (e.g. gravity) to work.
	/// </summary>
	[Serializable]
	public unsafe class PlayerPhysicsAction : AIAction
	{
		/// <inheritdoc />
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
		}
	}
}
