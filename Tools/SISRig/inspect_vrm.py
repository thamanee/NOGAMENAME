import json,struct
from pathlib import Path
p=Path('E:/NONAMEGAME MODEL/SIS.vrm')
b=p.read_bytes(); n,t=struct.unpack_from('<II',b,12); d=json.loads(b[20:20+n])
out=Path('ArtSource/SIS_Rig');out.mkdir(parents=True,exist_ok=True)
(out/'source_gltf.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('EXTENSIONS',d.get('extensionsUsed'))
for i,m in enumerate(d.get('meshes',[])):
 print('MESH',i,m.get('name'), 'primitives',len(m['primitives']),'shapes',m.get('extras',{}).get('targetNames'))
print('NODES',[(i,n.get('name')) for i,n in enumerate(d['nodes'])])
for name,e in d.get('extensions',{}).items():
 if name=='VRM':print('VRM0',json.dumps(e.get('secondaryAnimation')),json.dumps(e.get('blendShapeMaster')))
 elif name=='VRMC_vrm':print('VRM1',json.dumps(e.get('expressions')),json.dumps(e.get('humanoid')))
 elif name=='VRMC_springBone':print('SPRING',json.dumps(e))
