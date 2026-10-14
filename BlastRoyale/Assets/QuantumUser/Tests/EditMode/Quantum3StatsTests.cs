using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

namespace FirstLight.Tests.EditorMode.Systems
{
	public unsafe class Quantum3StatsTests
	{
		[Test]
		public void ConstructorInitializesEveryGameplayStatAndCollectionHandle()
		{
			var stats = new Stats(100, 20, 3, 4, 75, 19, 6, 7, 8, 5);
			AssertStat(stats, StatType.Health, 100, 100);
			AssertStat(stats, StatType.Power, 20, 20);
			AssertStat(stats, StatType.Speed, 3, 3);
			AssertStat(stats, StatType.Armour, 4, 4);
			AssertStat(stats, StatType.Shield, 75, 19);
			AssertStat(stats, StatType.AttackRange, 6, 6);
			AssertStat(stats, StatType.PickupSpeed, 7, 7);
			AssertStat(stats, StatType.AmmoCapacity, 8, 8);
			Assert.That(stats.CurrentHealth, Is.EqualTo(100));
			Assert.That(stats.MinimumHealth, Is.EqualTo(5));
			Assert.That(stats.CurrentStatusModifierType, Is.EqualTo(StatusModifierType.None));
			Assert.That(stats.Modifiers, Is.EqualTo(default(QListPtr<Modifier>)));
			Assert.That(stats.SpellEffects, Is.EqualTo(default(QListPtr<EntityRef>)));
		}

		private static void AssertStat(Stats stats, StatType type, int baseValue, int currentValue)
		{
			var data = stats.GetStatData(type);
			Assert.That(data.Type, Is.EqualTo(type));
			Assert.That(data.BaseValue.RawValue, Is.EqualTo(((FP)baseValue).RawValue));
			Assert.That(data.StatValue.RawValue, Is.EqualTo(((FP)currentValue).RawValue));
		}
	}
}
