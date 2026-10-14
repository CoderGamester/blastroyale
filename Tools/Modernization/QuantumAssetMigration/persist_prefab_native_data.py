from pathlib import Path
import re,json,yaml,hashlib
import argparse
parser=argparse.ArgumentParser(description="Persist only proven original prefab native asset identifiers/references during isolated Q2->Q3 migration.")
parser.add_argument('--evidence',required=True,type=Path)
parser.add_argument('--project',required=True,type=Path)
parser.add_argument('--apply',action='store_true')
args=parser.parse_args()
base=args.evidence.resolve()
project=args.project.resolve()
assert project.name=='quantum3-source-project' and project.parent.name=='.local','Only the isolated migration project is writable'

rows=json.loads((base/'q3-prefab-script-persistence.json').read_text())
exports={}
for file in (base/'Q2AssetExport').glob('*.json'):
 data=json.loads(file.read_text()); key=data['Identifier']['Path'];exports[key]=(file,data)
assert len(rows)==40
changes=[]
for path in sorted({r['Path'] for r in rows}):
 source=base/'q3-before-transfer'/path
 target=project/path
 original=source.read_text();current=target.read_text()
 docs={}
 for block in re.split(r'(?=^--- !u!)',original,flags=re.M):
  h=re.match(r'--- !u!\d+ &(-?\d+)(?: stripped)?\n',block)
  if h:docs[h[1]]=yaml.safe_load(block[h.end():])
 localrows=[r for r in rows if r['Path']==path]
 assert len(localrows)==2
 view=next(r for r in localrows if r['QuantumPath'].endswith('|EntityView'))
 for row in localrows:
  doc=docs[row['LocalId']]['MonoBehaviour']
  parent=doc['Parent']['fileID'];assert parent
  parentdoc=docs[str(parent)]['MonoBehaviour']
  expectedparentguid='836dcb7e0f6259748b850a10912ce727' if row['QuantumPath'].endswith('|EntityPrototype') else '0008c51fe3af00b4da5d44f02d9b3dff'
  assert parentdoc['m_Script']['guid']==expectedparentguid,(path,row['LocalId'],'SDK component parent identity differs')
  file,data=exports[row['QuantumPath']]
  assert file.name==re.search(r'guid: ([0-9a-f]{32})',Path(str(source)+'.meta').read_text())[1]+'_'+row['LocalId']+'.json'
  pattern=r'(?m)(^--- !u!114 &'+re.escape(row['LocalId'])+r'(?: stripped)?\n)(.*?)(?=^--- !u!|\Z)'
  hits=list(re.finditer(pattern,current,re.S));assert len(hits)==1
  match=hits[0];body=match[2]
  assert '  m_Script: '+row['ActualSdkTargetScript'] in body
  marker=re.search(r'^  m_EditorClassIdentifier:.*\n',body,re.M);assert marker
  prefix=body[:marker.end()]
  ident=data['Identifier'];native='  Identifier:\n    Path: '+json.dumps(ident['Path'])+'\n    Guid:\n      Value: '+str(ident['Guid']['Value'])+'\n'
  if row['QuantumPath'].endswith('|EntityPrototype'):
   assert data['Container']=={}
   native+='  Container: {}\n  Parent: {fileID: '+str(parent)+'}\n  SelfViewAsset: {fileID: '+view['LocalId']+'}\n'
  else:
   gameobject=parentdoc['m_GameObject']['fileID'];assert gameobject and 'GameObject' in docs[str(gameobject)]
   native+='  Prefab: {fileID: '+str(gameobject)+'}\n' 
  replacement=match[1]+prefix+native
  current=current[:match.start()]+replacement+current[match.end():]
  changes.append({'Path':path,'LocalId':row['LocalId'],'QuantumIdentifier':ident,'ParentComponentFileId':parent,'VerifiedParentSdkGuid':expectedparentguid,'SourceExportSha256':hashlib.sha256(file.read_bytes()).hexdigest()})
 if args.apply:target.write_text(current)
if args.apply:(base/'q3-prefab-native-data-persistence.json').write_text(json.dumps(changes,indent=2))
print(('Persisted' if args.apply else 'Validated')+' all 40 exact exported native identifiers and proven SDK parent/view references; no component simulation payload changed.')
