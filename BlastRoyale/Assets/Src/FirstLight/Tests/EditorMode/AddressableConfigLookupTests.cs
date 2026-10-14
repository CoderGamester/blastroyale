using System.Collections.Generic;
using FirstLight.Game.Ids;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode
{
	public class AddressableConfigLookupTests
	{
		[Test]
		public void ActiveIdsResolveTheirOriginalConfigurationAfterAnEntryIsRetired()
		{
			foreach (var config in AddressableConfigLookup.Configs)
				Assert.That(((AddressableId) config.Id).GetConfig(), Is.SameAs(config));
			Assert.That(AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address,
				Is.EqualTo("Configs/Settings/QuantumRunnerConfigs.asset"));
			Assert.Throws<KeyNotFoundException>(() => AddressableId.Configs_Settings_AssetResources.GetConfig());
		}
	}
}
