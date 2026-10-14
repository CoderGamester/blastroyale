using System;

namespace Quantum
{
	/// <summary>
	/// This action changes the <see cref="PlayerCharacter"/> current weapon to the Melee weapon
	/// </summary>
	[Serializable]

	public unsafe class PlayerChangeDefaultWeapon : AIAction
	{
		/// <inheritdoc />
		public override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			f.Unsafe.GetPointer<PlayerCharacter>(e)->EquipSlotWeapon(f, e, 0);
		}
	}
}
