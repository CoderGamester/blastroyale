using GameLovers.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;

namespace FirstLight.Game.Infos
{
	/// <summary>
	/// The different types of stats that a piece of equipment may have.
	/// TODO: This should be rethought.
	/// </summary>
	public enum EquipmentStatType
	{
		Power,
		PowerToDamageRatio,
		Hp,
		Speed,
		Armor,
		AttackCooldown,
		TargetRange,
		ProjectileSpeed,
		MaxCapacity,
		ReloadTime,
		MinAttackAngle,
		SplashDamageRadius,
		NumberOfShots,
		SpecialId0,
		SpecialId1,
		PickupSpeed,
		ShieldCapacity,
		MagazineSize,
		AmmoCapacityBonus,
		Damage,
	}

	public struct EquipmentInfo
	{
		public UniqueId Id;
		public Equipment Equipment;
		public EquipmentManufacturer Manufacturer;
		public Pair<GameId, uint> ScrappingValue;
		public Pair<GameId, uint> UpgradeCost;
		public Pair<GameId, uint>[] FuseCost;
		public Pair<GameId, uint> RepairCost;
		public uint CurrentDurability;
		public bool IsEquipped;
		public int MaxLevel;
		public Dictionary<EquipmentStatType, float> Stats;

		/// <summary>
		/// Check if the item is broken or not
		/// </summary>
		public bool IsBroken => CurrentDurability == 0;

		public override string ToString()
		{
			return $"{nameof(Id)}: {Id}, {nameof(Equipment)}: {Equipment}, {nameof(Manufacturer)}: {Manufacturer}, {nameof(ScrappingValue)}: {ScrappingValue}, {nameof(UpgradeCost)}: {UpgradeCost}, {nameof(FuseCost)}: {FuseCost}, {nameof(RepairCost)}: {RepairCost}, {nameof(CurrentDurability)}: {CurrentDurability}, {nameof(IsEquipped)}: {IsEquipped}, {nameof(MaxLevel)}: {MaxLevel}, {nameof(Stats)}: {Stats}, {nameof(IsBroken)}: {IsBroken}";
		}
	}

	/// <summary>
	/// Extension implementations for the <see cref="EquipmentInfo"/>
	/// </summary>
	public static class EquipmentInfoExtensions
	{

		/// <summary>
		/// Requests the durability states for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static uint GetAvgDurability(this List<EquipmentInfo> items, out uint maxDurability)
		{
			var total = 0u;

			maxDurability = 0u;

			foreach (var item in items)
			{
				total += item.CurrentDurability;
			}

			return total;
		}

		/// <summary>
		/// Requests a specified <paramref name="stat"/> for all the equipments in the given <paramref name="items"/>
		/// </summary>
		public static float GetTotalStat(this List<EquipmentInfo> items, EquipmentStatType stat)
		{
			var total = 0f;

			foreach (var item in items)
			{
				total += item.Stats[stat];
			}

			return total;
		}
	}
}
