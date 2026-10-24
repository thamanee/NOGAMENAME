import bpy,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Player_Rig'
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.preferences.addon_install(filepath=str(ROOT/'Tools/PlayerRig/player_rig_tools.py'))
bpy.ops.preferences.addon_enable(module='player_rig_tools')
bpy.ops.wm.save_userpref()
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
arm=bpy.data.objects['Player_Rig'];bpy.context.view_layer.objects.active=arm
for old,new in [('SIS_RIG_VERSION','PLAYER_RIG_VERSION'),('SIS_FACE_CHANNELS','PLAYER_FACE_CHANNELS')]:
    if old in arm:arm[new]=arm[old];del arm[old]
assert hasattr(bpy.types,'PLAYER_PT_controls')
arm['Blink_L']=1.;bpy.ops.player.neutral();assert arm['Blink_L']==0
arm.pose.bones['CTRL_C_Hips'].location.x=.1;bpy.ops.player.pose_reset()
assert arm.pose.bones['CTRL_C_Hips'].location.length<1e-6
bpy.ops.player.physics_reset()
arm['SIS_INFO']='Player Rig: select armature, N sidebar > Player Rig. FK/IK body, face sliders, spring physics.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
bone_count=len(arm.data.bones)
bpy.ops.wm.read_factory_settings(use_empty=True)
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
assert bpy.ops.import_scene.vrm(filepath=str(OUT/'Player_Rigged_Physics.vrm'))=={'FINISHED'}
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
ext=arm.data.vrm_addon_extension
assert len(ext.spring_bone1.springs)==22
assert len(ext.vrm1.expressions.custom)==14
face=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.shape_keys)
assert len(face.data.shape_keys.key_blocks)==72
report={'helper_addon_installed':True,'reset_operators_pass':True,'blender_bones':bone_count,'vrm_roundtrip_import':True,'spring_chains':22,'custom_expressions':14,'shape_keys_including_basis':72}
(OUT/'final_check.json').write_text(json.dumps(report,indent=2))
print('PLAYER_FINAL_CHECK_PASS',json.dumps(report),flush=True)
