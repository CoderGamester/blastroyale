using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> <see cref="Weapon"/> is empty
	/// </summary>
	[Serializable]

	public class PlayerAmmoEmptyAction : AIAction
	{
		/// <inheritdoc />
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			// We should remove this from the circuit and just delete this in general
		}
	}
}
