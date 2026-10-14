#!/usr/bin/env python3
"""Reject changes that reinterpret persisted game AddressableId integers."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SOURCE = REPO / "BlastRoyale/Assets/Src/FirstLight/Game/Ids/AddressableId.cs"
CONTRACT = REPO / "Tools/CI/addressable-id-contract.json"


def enum_values(source: str, name: str) -> dict[str, int]:
    source = re.sub(r"/\*.*?\*/|//[^\n]*", "", source, flags=re.S)
    match = re.search(r"\benum\s+" + re.escape(name) + r"\s*\{([^}]*)\}", source)
    if not match:
        raise ValueError(f"Missing {name} enum")
    result: dict[str, int] = {}
    next_value = 0
    for entry in match.group(1).split(","):
        if not entry.strip():
            continue
        member = re.fullmatch(r"\s*([A-Za-z_]\w*)\s*(?:=\s*(\d+))?\s*", entry)
        if not member:
            raise ValueError(f"Unsupported enum member; use an explicit decimal integer: {entry.strip()}")
        member_name, explicit = member.groups()
        if member_name in result:
            raise ValueError(f"Duplicate enum member: {member_name}")
        value = next_value if explicit is None else int(explicit)
        if value in result.values():
            raise ValueError(f"AddressableId integer reused: {value}")
        result[member_name] = value
        next_value = value + 1
    return result


def config_addresses(source: str) -> dict[int, str]:
    result: dict[int, str] = {}
    table = re.search(r"_addressableConfigs\s*=\s*new List<AddressableConfig>\s*\{(.*?)\}\.AsReadOnly\(\)", source, re.S)
    if not table:
        raise ValueError("Missing global AddressableConfig table")
    rows = list(re.finditer(r'new\s+AddressableConfig\s*\(\s*(\d+)\s*,\s*"([^"\\]*)"\s*,', table[1]))
    constructors = re.findall(r"\bnew\s+(?:[A-Za-z_]\w*\.)*AddressableConfig\s*\(", table[1])
    if len(rows) != len(constructors):
        raise ValueError("Unsupported global configuration constructor; use a decimal integer ID and literal address")
    for row in rows:
        value, address = int(row[1]), row[2]
        if value in result:
            raise ValueError(f"Duplicate global configuration ID {value}: {result[value]} and {address}")
        result[value] = address
    if not result:
        raise ValueError("No generated AddressableConfig rows found")
    return result


def validate(source: str, contract: dict) -> None:
    actual = enum_values(source, "AddressableId")
    existing = contract["enumValues"]
    for name, value in existing.items():
        if actual.get(name) != value:
            raise ValueError(f"Persisted ID changed: {name} must remain {value}; actual {actual.get(name)}")
    old_max = max(existing.values())
    for name, value in actual.items():
        if name not in existing and value <= old_max:
            raise ValueError(f"New member {name} reuses a previously allocated integer {value}")
    addresses = config_addresses(source)
    for raw_value, address in contract["activeAddresses"].items():
        value = int(raw_value)
        if addresses.get(value) != address:
            raise ValueError(f"Active ID {value} must still resolve {address}; actual {addresses.get(value)}")
    for raw_value in contract["retiredIds"]:
        if int(raw_value) in addresses:
            raise ValueError(f"Retired ID {raw_value} cannot be reassigned to an active configuration")
    for value in addresses:
        if value not in actual.values():
            raise ValueError(f"Configuration ID {value} has no enum member")
    method = re.search(r"\bGetConfig\(this AddressableId addressable\)(.*?)(?=public static|$)", source, re.S)
    if not method or re.search(r"_addressableConfigs\s*\[", method[1]):
        raise ValueError("GetConfig must resolve by persisted Id, not configuration-list position")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=SOURCE)
    parser.add_argument("--contract", type=Path, default=CONTRACT)
    args = parser.parse_args()
    try:
        contract = json.loads(args.contract.read_text())
        validate(args.source.read_text(), contract)
    except (OSError, ValueError, KeyError) as error:
        parser.exit(1, f"AddressableId contract failed: {error}\n")
    print(f"AddressableId contract passed: {len(contract['enumValues'])} allocated integers, "
          f"{len(contract['activeAddresses'])} active addresses, {len(contract['retiredIds'])} retired integer(s).")


if __name__ == "__main__":
    main()
