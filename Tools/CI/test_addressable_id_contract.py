"""Mutation checks for serialized-ID corruption and upstream positional regeneration."""
import json
import re
import unittest

import validate_addressable_ids as guard


class AddressableIdContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = guard.SOURCE.read_text()
        cls.contract = json.loads(guard.CONTRACT.read_text())

    def test_current_generated_contract_is_valid(self):
        guard.validate(self.source, self.contract)

    def test_deleting_the_retired_enum_member_would_renumber_existing_assets(self):
        changed = self.source.replace("\t\tConfigs_Settings_AssetResources,\n", "")
        self.assertNotEqual(changed, self.source)
        with self.assertRaisesRegex(ValueError, "Persisted ID changed"):
            guard.validate(changed, self.contract)

    def test_reusing_a_retired_integer_for_a_new_member_is_rejected(self):
        changed = self.source.replace("Configs_Settings_AssetResources,", "NewAsset = 58,")
        with self.assertRaises(ValueError):
            guard.validate(changed, self.contract)

    def test_runner_configuration_cannot_be_rebound_to_simulation(self):
        changed = self.source.replace('new AddressableConfig(61, "Configs/Settings/QuantumRunnerConfigs.asset",',
                                      'new AddressableConfig(61, "Configs/Settings/SimulationConfig.asset",')
        self.assertNotEqual(changed, self.source)
        with self.assertRaises(ValueError):
            guard.validate(changed, self.contract)

    def test_expression_cannot_hide_a_retired_configuration_id(self):
        table = re.search(r"_addressableConfigs\s*=\s*new List<AddressableConfig>\s*\{", self.source)
        row = '\nnew AddressableConfig(58 + 0, "Retired/Another.asset", "Assets/Retired.asset", typeof(UnityEngine.Object), new[] {"GenerateIds"}),'
        changed = self.source[:table.end()] + row + self.source[table.end():]
        with self.assertRaisesRegex(ValueError, "Unsupported global configuration constructor"):
            guard.validate(changed, self.contract)

    def test_duplicate_global_id_is_invalid_even_when_address_matches(self):
        table = re.search(r"_addressableConfigs\s*=\s*new List<AddressableConfig>\s*\{", self.source)
        row = '\nnew AddressableConfig(0, "Collections/Flags/FlagSkinConfigs.asset", "Assets/Another.asset", typeof(UnityEngine.Object), new[] {"GenerateIds"}),'
        changed = self.source[:table.end()] + row + self.source[table.end():]
        with self.assertRaisesRegex(ValueError, "Duplicate global configuration ID"):
            guard.validate(changed, self.contract)

    def test_upstream_generated_positional_lookup_is_rejected(self):
        changed = self.source.replace("foreach (var config in _addressableConfigs)",
                                      "return _addressableConfigs[(int) addressable];\n\t\t\tforeach (var config in _addressableConfigs)")
        with self.assertRaisesRegex(ValueError, "list position"):
            guard.validate(changed, self.contract)


if __name__ == "__main__":
    unittest.main()
