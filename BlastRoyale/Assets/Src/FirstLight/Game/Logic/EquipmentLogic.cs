using ConfigsProvider = FirstLight.Server.SDK.Modules.GameConfiguration.ConfigsProvider;
using IDataProvider = FirstLight.Server.SDK.Models.IDataProvider;
using GameLovers.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using GameLovers.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Logic
{
	public enum EquipmentFilter
	{
		All,
		Broken,
		Unbroken
	}

	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's equipment
	/// </summary>
	public interface IEquipmentDataProvider
	{
		/// <summary>
		/// Requests the player's loadout.
		/// </summary>
		IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout { get; }

		/// <summary>
		/// Requests the player's inventory.
		/// </summary>
		IObservableDictionaryReader<UniqueId, Equipment> Inventory { get; }

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> scrapping reward
		/// </summary>
		Pair<GameId, uint> GetScrappingReward(Equipment equipment);

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> upgrade cost for 1 level
		/// </summary>
		Pair<GameId, uint> GetUpgradeCost(Equipment equipment);

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> fusion cost for 1 upgrade
		/// </summary>
		Pair<GameId, uint>[] GetFuseCost(Equipment equipment);

		/// <summary>
		/// Requests the given's <paramref name="equipment"/> repair cost
		/// </summary>
		Pair<GameId, uint> GetRepairCost(Equipment equipment);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="id"/> of an
		/// inventory item.
		/// </summary>
		EquipmentInfo GetInfo(UniqueId id);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for the given <paramref name="equipment"/>
		/// </summary>
		EquipmentInfo GetInfo(Equipment equipment);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the loadout with the given <paramref name="filter"/>
		/// </summary>
		List<EquipmentInfo> GetLoadoutEquipmentInfo(EquipmentFilter filter);

		/// <summary>
		/// Requests the <see cref="EquipmentInfo"/> for all the inventory with the given <paramref name="filter"/>
		/// </summary>
		List<EquipmentInfo> GetInventoryEquipmentInfo(EquipmentFilter filter);

		/// <summary>
		/// Gets the count of equipment with a filter.
		/// </summary>
		int GetInventoryEquipmentCount(EquipmentFilter filter);

		/// <summary>
		/// Generates a new unique piece of equipment from battle pass reward configs
		/// </summary>
		Equipment GenerateEquipmentFromConfig(EquipmentRewardConfig config);

		/// <summary>
		/// Returns the desired max level of a given equipment
		/// </summary>
		int GetMaxLevel(Equipment equipment);

		/// <summary>
		/// Obtains the correct manufacturer for the given equipment.
		/// </summary>
		EquipmentManufacturer GetManufacturer(Equipment equipment);
	}

	/// <inheritdoc />
	public interface IEquipmentLogic : IEquipmentDataProvider
	{
		/// <summary>
		/// Adds an item to the inventory and assigns it a new UniqueId.
		/// </summary>
		UniqueId AddToInventory(Equipment equipment);

		/// <summary>
		/// Removes a given equipment from inventory.
		/// </summary>
		void RemoveFromInventory(UniqueId id);

		/// <summary>
		/// Removes all entry equipment from Loadout.
		/// </summary>
		void RemoveAllFromLoadout();

		/// <summary>
		/// Sets the loadout for each slot in given <paramref name="newLoadout"/>
		/// </summary>
		void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout);

		/// <summary>
		/// Equips the given <paramref name="itemId"/> to the player's Equipment slot.
		/// </summary>
		void Equip(UniqueId itemId);

		/// <summary>
		/// Unequips the given <paramref name="itemId"/> from the player's Equipment slot.
		/// </summary>
		void Unequip(UniqueId itemId);

		/// <summary>
		/// Scraps the equipment of the given <paramref name="itemId"/> and returns the reward of scrapping the item
		/// </summary>
		Pair<GameId, uint> Scrap(UniqueId itemId);

		/// <summary>
		/// Upgrades the equipment of the given <paramref name="itemId"/> by one level
		/// </summary>
		void Upgrade(UniqueId itemId);

		/// <summary>
		/// Fuses the equipment of the given <paramref name="itemId"/> increasing it's rarity by 1 step
		/// </summary>
		void Fuse(UniqueId itemId);

		/// <summary>
		/// Repairs the equipment of the given <paramref name="itemId"/> to full durability
		/// </summary>
		void Repair(UniqueId itemId);

	}

	/// <inheritdoc cref="IEquipmentLogic"/>
	public class EquipmentLogic : AbstractBaseLogic<EquipmentData>, IEquipmentLogic, IGameLogicInitializer
	{
		private ObservableDictionary<GameIdGroup, UniqueId> _loadout;
		private ObservableDictionary<UniqueId, Equipment> _inventory;
		public IObservableDictionaryReader<GameIdGroup, UniqueId> Loadout => _loadout;
		public IObservableDictionaryReader<UniqueId, Equipment> Inventory => _inventory;

		public EquipmentLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_loadout = new ObservableDictionary<GameIdGroup, UniqueId>(DataProvider.GetData<PlayerData>().Equipped) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
			_inventory = new ObservableDictionary<UniqueId, Equipment>(Data.Inventory) {ObservableUpdateFlag = ObservableUpdateFlag.Both};
		}

		public void ReInit()
		{
			{
				_loadout.Rebind(DataProvider.GetData<PlayerData>().Equipped);
			}

			{
				_inventory.Rebind(Data.Inventory);
			}

			_loadout.NotifyAll();
			_inventory.NotifyAll();
		}

		public Pair<GameId, uint> GetScrappingReward(Equipment equipment)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<ScrapConfig>((int) equipment.Rarity);
			return new Pair<GameId, uint>(GameId.COIN, config.CoinReward);
		}

		public Pair<GameId, uint> GetUpgradeCost(Equipment equipment)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<UpgradeDataConfig>((int) equipment.Level);
			return new Pair<GameId, uint>(GameId.COIN,config.CoinCost);
		}

		public Pair<GameId, uint>[] GetFuseCost(Equipment equipment)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<FuseConfig>((int) equipment.Rarity);

			return new Pair<GameId, uint>[]{
				new (GameId.COIN, config.CoinCost),
				new (GameId.Fragments, config.FragmentCost)
			};
		}

		public Pair<GameId, uint> GetRepairCost(Equipment equipment)
		{
			return new Pair<GameId, uint>(GameId.COIN, 10);
		}

		public EquipmentInfo GetInfo(UniqueId id)
		{
			var info = GetInfo(_inventory[id]);
			info.Id = id;
			info.IsEquipped = _loadout.TryGetValue(info.Equipment.GameId.GetSlot(), out var equipId) && equipId == id;
			return info;
		}

		public EquipmentInfo GetInfo(Equipment equipment)
		{
			var nextEquipment = equipment;
			nextEquipment.Level++;

			var nextRarityEquipment = equipment;
			nextRarityEquipment.Rarity++;

			var durability =
				equipment.GetCurrentDurability(GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>(),
					GameLogic.TimeService.DateTimeUtcNow.Ticks);

			return new EquipmentInfo
			{
				Equipment = equipment,
				ScrappingValue = GetScrappingReward(equipment),
				UpgradeCost = GetUpgradeCost(equipment),
				RepairCost = GetRepairCost(equipment),
				FuseCost = GetFuseCost(equipment),
				CurrentDurability = durability,
				MaxLevel = GetMaxLevel(equipment),
				Manufacturer = GetManufacturer(equipment),
				Stats = equipment.GetStats(GameLogic.ConfigsProvider)
			};
		}

		public List<EquipmentInfo> GetLoadoutEquipmentInfo(EquipmentFilter filter)
		{
			var ret = new List<EquipmentInfo>();
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var timestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			foreach (var (slot, id) in _loadout)
			{

				var durability = _inventory[id].GetCurrentDurability(config, timestamp);

				if (filter == EquipmentFilter.Broken && durability > 0 ||
				    filter == EquipmentFilter.Unbroken && durability == 0)
				{
					continue;
				}

				ret.Add(GetInfo(id));
			}

			return ret;
		}

		public List<EquipmentInfo> GetInventoryEquipmentInfo(EquipmentFilter filter)
		{
			var ret = new List<EquipmentInfo>();
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var timestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			foreach (var (id, equipment) in _inventory)
			{

				var durability = _inventory[id].GetCurrentDurability(config, timestamp);

				if (filter == EquipmentFilter.Broken && durability > 0 ||
				    filter == EquipmentFilter.Unbroken && durability == 0)
				{
					continue;
				}

				ret.Add(GetInfo(id));
			}

			return ret;
		}

		public int GetInventoryEquipmentCount(EquipmentFilter filter)
		{
			var count = 0;
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var timestamp = GameLogic.TimeService.DateTimeUtcNow.Ticks;

			foreach (var (id, _) in _inventory)
			{

				var durability = _inventory[id].GetCurrentDurability(config, timestamp);

				if (filter == EquipmentFilter.Broken && durability > 0 ||
					filter == EquipmentFilter.Unbroken && durability == 0)
				{
					continue;
				}

				count++;
			}

			return count;
		}

		public Equipment GenerateEquipmentFromConfig(EquipmentRewardConfig config)
		{
			if (config.Level < 1)
			{
				throw new LogicException("Invalid equipment reward configuration: level 0 for id "+config.Id+" - "+config.GameId.ToString());
			}
			var gameId = config.GameId;

			if (gameId.IsInGroup(GameIdGroup.Core))
			{
				var equipmentConfigs = GameLogic.ConfigsProvider.GetConfigsList<QuantumBaseEquipmentStatConfig>();
				var equipmentCategory = config.EquipmentCategory.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.EquipmentCategory));
				var matchingEquipment = equipmentConfigs
					.Where(x => x.Id.IsInGroup(equipmentCategory)
					            && x.Id != GameId.Hammer
					            && !x.Id.IsInGroup(GameIdGroup.Deprecated)).ToList();

				gameId = matchingEquipment[GameLogic.RngLogic.Range(0, matchingEquipment.Count)].Id;
			}

			var rarity = config.Rarity.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Rarity));
			var grade = config.Grade.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Grade));
			var adjective = config.Adjective.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Adjective));
			var faction = config.Faction.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Faction));
			var material = config.Material.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Material));
			var edition = config.Edition.Keys.ElementAt(GetWeightedRandomDictionaryIndex(config.Edition));
			var maxDurability = (uint) GameLogic.RngLogic.Range(config.MaxDurability.Key, config.MaxDurability.Value);

			return new Equipment(gameId: gameId,
			                     rarity: rarity,
			                     adjective: adjective,
			                     grade: grade,
			                     faction: faction,
			                     material: material,
			                     edition: edition,
			                     maxDurability: maxDurability,
			                     level: config.Level,
			                     generation: config.Generation,
			                     tuning: config.Tuning,
			                     initialReplicationCounter: config.InitialReplicationCounter,
			                     replicationCounter: config.InitialReplicationCounter,
			                     lastRepairTimestamp: DateTime.UtcNow.Ticks);
		}

		public UniqueId AddToInventory(Equipment equipment)
		{
			if (!equipment.GameId.IsInGroup(GameIdGroup.Equipment))
			{
				throw new LogicException($"The given '{equipment.GameId.ToString()}' id is not of " +
				                         $"'{GameIdGroup.Equipment.ToString()}' game id group");
			}

			var id = GameLogic.UniqueIdLogic.GenerateNewUniqueId(equipment.GameId);

			_inventory.Add(id, equipment);

			return id;
		}

		public void SetLoadout(IDictionary<GameIdGroup, UniqueId> newLoadout)
		{
			var slots = Equipment.EquipmentSlots;

			foreach (var slot in slots)
			{
				var isEquippingSlot = newLoadout.TryGetValue(slot, out var equipAddToSlot);
				if (isEquippingSlot)
				{
					if (!_loadout.TryGetValue(slot, out var equippedId) || equipAddToSlot != equippedId)
					{
						Equip(equipAddToSlot);
					}
				}
				else if(_loadout.ContainsKey(slot))
				{
					Unequip(_loadout[slot]);
				}
			}
		}

		public void Equip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();
			var config = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

			if (!Inventory.TryGetValue(itemId, out var equipment))
			{
				throw new LogicException($"The player does not own item {itemId} - {equipment.GameId.ToString()}");
			}

			if (equipment.GetCurrentDurability(config, GameLogic.TimeService.DateTimeUtcNow.Ticks) == 0)
			{
				throw new LogicException($"Item {itemId} - {equipment.GameId.ToString()} is broken");
			}

			if (_loadout.TryGetValue(slot, out var equippedId))
			{
				if (equippedId == itemId)
				{
					throw new LogicException($"The player already has the given item Id " +
					                         $"{itemId} - {equipment.GameId.ToString()} equipped");
				}

				_loadout[slot] = itemId;
			}
			else
			{
				_loadout.Add(slot, itemId);
			}
		}

		public void Unequip(UniqueId itemId)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[itemId];
			var slot = gameId.GetSlot();

			if (!_loadout.TryGetValue(slot, out var equippedId) || equippedId != itemId)
			{
				throw new
					LogicException($"The player does not have the given '{gameId.ToString()}' item equipped to be unequipped");
			}

			_loadout.Remove(slot);
		}

		public int GetMaxLevel(Equipment equip)
		{
			var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityDataConfig>((int) equip.Rarity);
			return rarityConfig.MaxLevel;
		}

		public EquipmentManufacturer GetManufacturer(Equipment equipment)
		{
			var equipmentDataConfig =
				GameLogic.ConfigsProvider.GetConfig<QuantumBaseEquipmentStatConfig>((int) equipment.GameId);
			return equipmentDataConfig.Manufacturer;
		}

		public Pair<GameId, uint> Scrap(UniqueId itemId)
		{
			var equipment = _inventory[itemId];
			var reward = GetScrappingReward(equipment);

			RemoveFromInventory(itemId);

			return reward;
		}

		public void Upgrade(UniqueId itemId)
		{
			var equipment = _inventory[itemId];

			if (GetMaxLevel(equipment) == equipment.Level)
			{
				throw new LogicException($"Item {itemId} - {equipment.GameId.ToString()} is already at max level " +
				                         $"{equipment.Level} and cannot be upgraded further");
			}

			equipment.Level++;

			_inventory[itemId] = equipment;
		}

		public void Fuse(UniqueId itemId)
		{
			var equipment = _inventory[itemId];

			if (equipment.Rarity == EquipmentRarity.TOTAL -1)
			{
				throw new LogicException($"Item {itemId} - {equipment.GameId.ToString()} is already at max rarity " +
										 $"{equipment.Level} and cannot be fused further");
			}

			equipment.Rarity++;

			_inventory[itemId] = equipment;
		}

		public void Repair(UniqueId itemId)
		{
			// clank clank clank
		}

		private int GetWeightedRandomDictionaryIndex<TKey, TValue>(SerializedDictionary<TKey, TValue> dictionary)
		{
			var rangeDictionary = dictionary as Dictionary<TKey, FP>;
			var indexRanges = new List<Tuple<FP, FP>>();
			var currentRangeMax = FP._0;

			foreach (var valueMax in rangeDictionary.Values)
			{
				var min = currentRangeMax;
				var max = min + valueMax;
				indexRanges.Add(new Tuple<FP, FP>(min,max));

				currentRangeMax = max;
			}

			var rand = GameLogic.RngLogic.NextFp * currentRangeMax;

			foreach (var range in indexRanges)
			{
				if (rand >= range.Item1 && rand < range.Item2)
				{
					return indexRanges.IndexOf(range);
				}
			}

			throw new LogicException("Dictionary weighted random could not return a valid index.");
		}

		public void RemoveAllFromLoadout()
		{
			_loadout.Clear();
		}

		public void RemoveFromInventory(UniqueId equipment)
		{
			var gameId = GameLogic.UniqueIdLogic.Ids[equipment];
			var slot = gameId.GetSlot();
			if (_loadout.TryGetValue(slot, out var equippedId) && equippedId == equipment)
			{
				Unequip(equippedId);
			}
			_inventory.Remove(equipment);
			GameLogic.UniqueIdLogic.RemoveId(equipment);
		}
	}
}
