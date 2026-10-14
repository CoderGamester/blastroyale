#!/usr/bin/env python3
"""Restore authored component script identity after the real pinned Q3 generator runs.

No SDK or game source is generated/replaced. Exact generated source hashes and class
names guard the 22 metadata-only edits. Check is the default; --apply writes metas
and keeps a content-addressed recovery copy of each original metadata file.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    project = args.project.resolve()
    manifest = json.loads(Path(__file__).with_name('quantum-generated-guids.json').read_text())
    entries = manifest['entries']
    if len(entries) != 22 or len({x['guid'] for x in entries}) != 22:
        raise SystemExit('Expected exactly 22 unique reviewed component GUIDs.')
    pending = []
    for entry in entries:
        source = (project / entry['path']).resolve()
        if not source.is_relative_to(project) or not source.is_file():
            raise SystemExit(f'Missing or unsafe generated source: {entry["path"]}')
        data = source.read_bytes()
        if hashlib.sha256(data).hexdigest() != entry['source_sha256']:
            raise SystemExit(f'Generated source checksum changed; review required: {entry["path"]}')
        class_name = entry['type'].split('.')[-1]
        if not re.search(r'\bclass\s+' + re.escape(class_name) + r'\b', data.decode('utf-8-sig')):
            raise SystemExit(f'Actual generated component identity differs: {entry["path"]}')
        meta = Path(str(source) + '.meta')
        if not meta.is_file():
            raise SystemExit(f'Import real generated scripts first; metadata absent: {entry["path"]}')
        original = meta.read_bytes()
        text = original.decode('utf-8')
        matches = list(re.finditer(r'^guid: ([0-9a-f]{32})(?=\r?$)', text, re.MULTILINE))
        if len(matches) != 1:
            raise SystemExit(f'Expected one valid Unity GUID: {entry["path"]}')
        current = matches[0].group(1)
        if current != entry['guid']:
            updated = text[:matches[0].start(1)] + entry['guid'] + text[matches[0].end(1):]
            pending.append((meta, original, updated.encode('utf-8')))
    if args.apply:
        backups = project / '.modernization-guid-backups'
        backups.mkdir(exist_ok=True)
        for meta, original, updated in pending:
            digest = hashlib.sha256(original).hexdigest()
            backup = backups / (digest + '.meta')
            if not backup.exists():
                backup.write_bytes(original)
            meta.write_bytes(updated)
    print(f'{"Applied" if args.apply else "Validated"} 22 exact generated component sources; '
          f'{len(pending)} metadata GUID changes {"written" if args.apply else "needed"}.')


if __name__ == '__main__':
    main()
