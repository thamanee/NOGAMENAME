import bpy,sys,json,math
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Player_Rig'
sys.path.insert(0,str(ROOT/'Tools/SISRig/vendor'))
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
arm=bpy.data.objects['Player_Rig'];scene=bpy.context.scene
cam=bpy.data.objects['Player_FaceCamera']
eye_z=(arm.matrix_world @ arm.pose.bones['J_Adj_L_FaceEye'].head).z
cam.location=(0,-4,eye_z)
cam.rotation_euler=(Vector((0,0,eye_z-.02))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.ortho_scale=.48
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
arm.data.vrm_addon_extension.spring_bone1.enable_animation=False
samples=json.loads((OUT/'physics_samples.json').read_text())
for frame,state in enumerate(samples,1):
    t=(frame-1)/30
    p=arm.pose.bones['CTRL_C_Hips'];p.location.x=.035*math.sin(t*math.tau);p.keyframe_insert('location',frame=frame,group='Body demo')
    p=arm.pose.bones['CTRL_C_Head'];p.rotation_euler.z=.2*math.sin(t*math.tau*1.1);p.keyframe_insert('rotation_euler',frame=frame,group='Body demo')
    for side,sign in [('L',1),('R',-1)]:
        for part,angle in [('UpperArm',sign*.18*math.sin(t*math.tau*.8)),('UpperLeg',sign*.12*math.sin(t*math.tau*.8))]:
            p=arm.pose.bones['CTRL_'+side+'_'+part]
            if part=='UpperArm':p.rotation_euler.z=angle
            else:p.rotation_euler.x=angle
            p.keyframe_insert('rotation_euler',frame=frame,group='Body demo')
    for name,q in state.items():
        p=arm.pose.bones[name];p.rotation_mode='QUATERNION';p.rotation_quaternion=Quaternion(q)
        p.keyframe_insert('rotation_quaternion',frame=frame,group='Baked spring physics')
for frame in [1,12,15,18,30,42,45,48,60,72,75,78,90]:
    arm['Blink_L']=1. if frame in [15,45,75] else 0.
    arm['Blink_R']=arm['Blink_L'] if frame!=45 else 0.
    arm['Mouth_A']=.5 if frame in [30,60] else 0.
    arm['Brow_Up_R']=.75 if frame in [42,45,48] else 0.
    for k in ['Blink_L','Blink_R','Mouth_A','Brow_Up_R']:arm.keyframe_insert('["'+k+'"]',frame=frame,group='Face demo')
arm.animation_data.action.name='SIS_Physics_and_Face_Demo_BAKED'
scene.frame_end=90
scene['SIS_DEMO']='Baked physical simulation, 3 seconds at 30fps. Live spring simulation disabled to avoid double transforms. Edit using Player_Rigged.blend.'
for f,name in [(1,'START: body + hair + clothes'),(30,'Mouth A'),(45,'Wink + raised brow'),(60,'Mouth A'),(90,'END')]:scene.timeline_markers.new(name,frame=f)
scene.frame_set(1)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Player_Physics_Demo.blend'))
scene.frame_set(40);bpy.context.view_layer.update()
scene.camera=bpy.data.objects['Player_FullBody'];scene.render.filepath=str(OUT/'Player_Physics_Pose.png')
bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['Player_FaceCamera'];scene.render.resolution_x=700;scene.render.resolution_y=700
scene.frame_set(45);bpy.context.view_layer.update();scene.render.filepath=str(OUT/'Player_Demo_Face.png')
bpy.ops.render.render(write_still=True)
print('DEMO_COMPLETE',flush=True)
