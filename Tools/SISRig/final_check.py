import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/SIS_Rig'
bpy.ops.preferences.addon_install(filepath=str(ROOT/'Tools/SISRig/sis_rig_tools.py'))
bpy.ops.preferences.addon_enable(module='sis_rig_tools')
bpy.ops.wm.save_userpref()
bpy.ops.wm.open_mainfile(filepath=str(OUT/'SIS_Rigged.blend'))
arm=bpy.data.objects['SIS_Rig'];bpy.context.view_layer.objects.active=arm
assert hasattr(bpy.types,'SIS_PT_controls')
arm['Blink_L']=1.;bpy.ops.sis.neutral();assert arm['Blink_L']==0
arm.pose.bones['CTRL_C_Hips'].location.x=.1
bpy.ops.sis.pose_reset();assert arm.pose.bones['CTRL_C_Hips'].location.length<1e-6
bpy.ops.sis.physics_reset()
assert len(arm.data.bones)==244
bpy.ops.wm.open_mainfile(filepath=str(OUT/'SIS_Physics_Demo.blend'))
arm=bpy.data.objects['SIS_Rig'];bpy.context.scene.frame_set(45)
assert arm.animation_data.action and not arm.data.vrm_addon_extension.spring_bone1.enable_animation
assert arm['Blink_L']>.99
# New empty test scene in a separate background Blender process, preserving deliverables.
bpy.ops.wm.read_factory_settings(use_empty=True)
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
result=bpy.ops.import_scene.vrm(filepath=str(OUT/'SIS_Rigged_Physics.vrm'))
assert result=={'FINISHED'}
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
e=arm.data.vrm_addon_extension
assert len(e.spring_bone1.springs)==37 and len(e.vrm1.expressions.custom)==14
faces=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.shape_keys]
assert len(faces)==1 and len(faces[0].data.shape_keys.key_blocks)==72
report={'helper_addon_installed':True,'helper_reset_operators_pass':True,'main_blend_bones':244,'demo_playback_pass':True,'vrm_roundtrip_import':'FINISHED','roundtrip_springs':37,'roundtrip_custom_expressions':14,'roundtrip_shape_keys_including_basis':72}
(OUT/'final_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FINAL_CHECK_PASS',json.dumps(report),flush=True)
