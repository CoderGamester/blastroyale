#!/usr/bin/env python3
"""Prepare an isolated genuine Unity/SDK host; no library, license payload or account IDs are published."""
from pathlib import Path
import argparse, json, re, shutil, subprocess

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--replace',action='store_true',help='Replace only the exact isolated validation project; stop its Unity process first.')
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[3]
    source=root/'BlastRoyale'; target=root/'.local/remaining-map-validation-project'
    if target.exists():
        if not args.replace: raise SystemExit('Isolated project exists; retain it or explicitly pass --replace after its Unity process exits.')
        if any(str(target) in line and 'Unity -' in line for line in subprocess.check_output(['ps','-axo','command'],text=True).splitlines()):raise SystemExit('Its Unity process is active.')
        shutil.rmtree(target)
    sdk=source/'Assets/Photon/Quantum/Assemblies/Quantum.Engine.dll'
    if not sdk.exists():raise SystemExit('Restore the real licensed Quantum3 SDK with the repository setup tools before preparing this validation host.')
    target.mkdir(parents=True)
    excludes=['LocalClientConfig.json','LocalClientConfig.json.meta','UnityPlayerAccountSettings.asset','UnityPlayerAccountSettings.asset.meta','PhotonServerSettings.asset','PhotonServerSettings.asset.meta','Library','Temp','Logs','UserSettings','*.pfx','*.p12','*.keystore','*.jks','*.mobileprovision']
    for name in ('Assets','Packages','ProjectSettings'):
        subprocess.run(['rsync','-a',*[part for excluded in excludes for part in ('--exclude',excluded)],str(source/name)+'/',str(target/name)+'/'],check=True)
    settings=target/'ProjectSettings/ProjectSettings.asset';text=settings.read_text()
    for key in ('cloudProjectId','organizationId','projectName'):text=re.sub(r'^(  '+re.escape(key)+r':).*$',r'\1',text,flags=re.M)
    settings.write_text(text)
    folder=target/'Assets/ModernizationValidation/Editor';folder.mkdir(parents=True)
    shutil.copy2(Path(__file__).with_name('ModernizationAcceptedMapsValidation.cs'),folder/'ModernizationAcceptedMapsValidation.cs')
    ledger=root/'Docs/Modernization/verification/native-map-entity-cache-ledger.json'
    shutil.copy2(ledger,target/'native-map-entity-cache-ledger.json')
    assert len(json.loads(ledger.read_text())['Entries'])==10
    print('Prepared genuine isolated validation host for all10 accepted maps; credentials and native project binding excluded.')

if __name__=='__main__':main()
