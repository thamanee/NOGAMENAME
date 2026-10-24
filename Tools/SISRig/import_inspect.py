import bpy, sys, importlib, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(Path(__file__).parent/'vendor'))
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
print('IMPORT',bpy.ops.import_scene.vrm(filepath='E:/NONAMEGAME MODEL/SIS.vrm'))
out=ROOT/'ArtSource/SIS_Rig'
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
arm.name='SIS_Rig'
print('ARMATURE',arm.name,len(arm.data.bones))
ext=arm.data.vrm_addon_extension
print('SPRINGS',[(s.vrm_name,len(s.joints),len(s.collider_groups)) for s in ext.spring_bone1.springs])
report={'bones':{},'meshes':{}}
for b in arm.data.bones:
 report['bones'][b.name]={'head':list(b.head_local),'tail':list(b.tail_local),'parent':b.parent.name if b.parent else None}
for o in bpy.data.objects:
 if o.type=='MESH':
  report['meshes'][o.name]={'vertices':len(o.data.vertices),'materials':[m.name if m else None for m in o.data.materials], 'shapes':list(o.data.shape_keys.key_blocks.keys()) if o.data.shape_keys else [],'groups':{g.name:sum(1 for v in o.data.vertices if any(w.group==g.index and w.weight>.001 for w in v.groups)) for g in o.vertex_groups}}
print('MESHES',[(n,d['vertices'],d['materials']) for n,d in report['meshes'].items()])
(out/'import_inspection.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'SIS_imported.blend'))
