using System;

namespace Quantum
{
	/// <summary>
	/// This action processes when the <see cref="PlayerCharacter"/> begins a reload with thier  <see cref="Weapon"/>
	/// </summary>
	[Serializable]

	public class PlayerStartReloadAction : AIAction
	{
		/// <inheritdoc />
		public unsafe override void Execute(Frame f, EntityRef e, ref AIContext aiContext)
		{
			var player = f.Unsafe.GetPointer<PlayerCharacter>(e);
			f.Events.OnPlayerReloadStart(player->Player, e, player->CurrentWeapon);
		}
	}
}
