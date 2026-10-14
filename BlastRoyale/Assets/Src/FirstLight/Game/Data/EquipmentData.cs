using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Holds the equipment inventory owned by a player.
	/// </summary>
	[Serializable]
	public class EquipmentData
	{
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		public ulong LastUpdateTimestamp;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + LastUpdateTimestamp.GetHashCode();
			foreach (var item in Inventory.Values)
			{
				hash = hash * 23 + GetItemHash(item);
			}

			return hash;
		}

		public int GetItemHash(Equipment e)
		{
			return e.GetServerHashCode();
		}
	}
}
