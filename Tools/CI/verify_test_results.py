#!/usr/bin/env python3
"""Fail a test job if NUnit results are missing, empty, all skipped, or failed."""
import argparse
import pathlib
import sys
import xml.etree.ElementTree as ET


def verify(directory):
    cases = []
    for path in directory.rglob("*.xml"):
        root = ET.parse(path).getroot()
        if root.tag in {"test-run", "test-results"}:
            cases.extend(root.iter("test-case"))
    if not cases:
        raise ValueError("No NUnit test cases ran. Restore/port the real fixtures; zero tests is not a pass.")
    failed = [case for case in cases if case.get("result", "").lower() in {"failed", "failure", "error", "cancelled"} or case.get("success", "").lower() == "false" and case.get("executed", "").lower() == "true"]
    passed = [case for case in cases if case.get("result", "").lower() in {"passed", "success"} or case.get("success", "").lower() == "true" and case.get("executed", "").lower() == "true"]
    if failed or not passed:
        raise ValueError(f"NUnit results: {len(passed)} passed, {len(failed)} failed; at least one real passing test and no failures are required")
    print(f"Verified NUnit results: {len(passed)} passed, {len(cases) - len(passed)} skipped")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=pathlib.Path)
    args = parser.parse_args()
    try:
        verify(args.directory)
    except (ValueError, ET.ParseError) as error:
        print(f"::error::{error}", file=sys.stderr)
        sys.exit(1)
