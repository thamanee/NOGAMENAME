"""Author reusable SIS game actions; export only the original humanoid skeleton."""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Quaternion, Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/SIS_Animations'; OUT.mkdir(exist_ok=True)
DEST=ROOT/'Assets/SISAnimations'; DEST.mkdir(exist_ok=True)
sys.path.insert(0,str(Path(__file__).parent/'vendor'))
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/SIS_Rig/SIS_Rigged.blend'))
arm=bpy.data.objects['SIS_Rig']; scene=bpy.context.scene
arm.data.vrm_addon_extension.spring_bone1.enable_animation=False
arm.animation_data_clear()
for p in arm.pose.bones:
    for c in list(p.constraints): p.constraints.remove(c)
    p.location=(0,0,0); p.rotation_mode='QUATERNION'; p.rotation_quaternion=(1,0,0,0)
    p.scale=(1,1,1)
# Animations target deform bones directly; helper controls are removed from this copy.
bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='EDIT')
for b in list(arm.data.edit_bones):
    if b.name.startswith(('CTRL_','IK_','POLE_')): arm.data.edit_bones.remove(b)
bpy.ops.object.mode_set(mode='OBJECT')
for c in arm.data.collections: c.is_visible=True
for p in arm.pose.bones: p.custom_shape=None
bones=[p for p in arm.pose.bones if p.name.startswith('J_Bip_')]
rest={p.name:p.bone.matrix_local.to_quaternion() for p in bones}
hip=arm.pose.bones['J_Bip_C_Hips']; hip_z=hip.bone.head_local.z
def smooth(t):
    t=max(0,min(1,t)); return t*t*(3-2*t)
def mix(a,b,t): return a+(b-a)*smooth(t)
def pose():
    d={'height':0.,'C_Hips':(0,0,0),'C_Spine':(0,0,0),'C_Chest':(0,0,0),'C_Head':(0,0,0)}
    for s,sg in [('L',1),('R',-1)]:
        d[s+'_UpperArm']=(0,sg*78,0); d[s+'_LowerArm']=(-8,0,0)
        d[s+'_UpperLeg']=(0,0,0);d[s+'_LowerLeg']=(0,0,0);d[s+'_Foot']=(0,0,0)
        for finger in ['Index','Middle','Ring','Little']:
            for segment in ['Proximal','Intermediate','Distal']:
                d[s+'_'+finger+segment]=(0,sg*10,0)
    return d
def sitting():
    d=pose();d['height']=-.37
    for s,sg in [('L',1),('R',-1)]:
        d[s+'_UpperLeg']=(-88,0,sg*3);d[s+'_LowerLeg']=(88,0,0)
        d[s+'_UpperArm']=(-18,sg*74,0);d[s+'_LowerArm']=(-62,0,0)
    return d
def lying():
    d=pose();d['height']=.13-hip_z;d['C_Hips']=(-90,0,0)
    for s,sg in [('L',1),('R',-1)]: d[s+'_UpperArm']=(0,sg*83,0)
    return d
def blend(a,b,t):
    t=smooth(t);d={}
    for k in a.keys()|b.keys():
        va=a.get(k,(0,0,0));vb=b.get(k,(0,0,0))
        d[k]=va+(vb-va)*t if isinstance(va,(int,float)) else tuple(x+(y-x)*t for x,y in zip(va,vb))
    return d
def sample(name,t,duration):
    u=t/duration; w=math.tau*u; d=pose()
    if name in ['Idle','Idle_LookAround','Talk','Wave','Cheer','Interact','HitReact','TurnLeft','TurnRight']:
        d['C_Chest']=(.8*math.sin(w),0,0);d['height']=.002*math.sin(w)
    if name=='Idle_LookAround':d['C_Head']=(2*math.sin(w),0,24*math.sin(w))
    if name in ['Walk','Run','WalkBackward','StrafeLeft','StrafeRight','CrouchWalk']:
        run=name=='Run'; crouch=name=='CrouchWalk'; amp=38 if run else 24
        d['height']=(-.19 if crouch else -.025)+(.035 if run else .013)*math.cos(2*w)
        d['C_Spine']=(12 if run else 16 if crouch else 3,0,0)
        d['C_Hips']=(0,0,3*math.sin(w));d['C_Chest']=(0,0,-5*math.sin(w))
        for s,sg in [('L',1),('R',-1)]:
            v=w+(0 if sg==1 else math.pi); stride=math.sin(v)
            if name=='WalkBackward':stride=-stride
            d[s+'_UpperLeg']=(-amp*stride-(20 if crouch else 8),0,0)
            d[s+'_LowerLeg']=((68 if run else 40)*max(0,-stride)+(35 if crouch else 8),0,0)
            d[s+'_Foot']=(-12*max(0,-stride),0,0)
            d[s+'_UpperArm']=(amp*.7*stride,sg*76,0);d[s+'_LowerArm']=(-70 if run else -18,0,0)
            if name.startswith('Strafe'):
                lateral=1 if name=='StrafeLeft' else -1
                d[s+'_UpperLeg']=(-5-12*max(0,-stride),lateral*18*stride,0)
    if name in ['Sit','SitDown','StandUp']:
        d=sitting() if name=='Sit' else blend(pose(),sitting(),u if name=='SitDown' else 1-u)
        if name=='Sit':d['C_Chest']=(math.sin(w),0,0)
        else:d['C_Spine']=(18*math.sin(math.pi*u),0,0)
    if name in ['LayDown','LieIdle','Sleep','GetUp']:
        if name in ['LieIdle','Sleep']:d=lying();d['C_Chest']=(.7*math.sin(w),0,0)
        else:
            v=u if name=='LayDown' else 1-u
            seated=sitting();seated['height']=.18-hip_z
            for s in ['L','R']: seated[s+'_LowerLeg']=(18,0,0)
            d=blend(pose(),seated,v/.55) if v<.55 else blend(seated,lying(),(v-.55)/.45)
    if name in ['CrouchIdle','CrouchDown','CrouchUp']:
        c=pose();c['height']=-.24;c['C_Spine']=(20,0,0)
        for s in ['L','R']:c[s+'_UpperLeg']=(-48,0,0);c[s+'_LowerLeg']=(78,0,0);c[s+'_Foot']=(-30,0,0)
        d=c if name=='CrouchIdle' else blend(pose(),c,u if name=='CrouchDown' else 1-u)
    if name in ['Jump','Fall','Land']:
        if name=='Jump':
            lift=math.sin(math.pi*u);d['height']=.32*lift-.08*math.sin(2*math.pi*u)
            for s,sg in [('L',1),('R',-1)]:
                d[s+'_UpperLeg']=(-20*lift,0,0);d[s+'_LowerLeg']=(35*lift,0,0)
                d[s+'_UpperArm']=(-60*lift,sg*(78-30*lift),0)
        elif name=='Fall':
            for s,sg in [('L',1),('R',-1)]:d[s+'_UpperArm']=(-20,sg*35,0);d[s+'_LowerLeg']=(18,0,0)
        else:
            k=math.sin(math.pi*u);d['height']=-.18*k;d['C_Spine']=(20*k,0,0)
            for s in ['L','R']:d[s+'_UpperLeg']=(-35*k,0,0);d[s+'_LowerLeg']=(60*k,0,0);d[s+'_Foot']=(-25*k,0,0)
    envelope=math.sin(math.pi*u)**2
    if name=='Wave':
        d['R_UpperArm']=(-35*envelope,-78+110*envelope,0)
        d['R_LowerArm']=(-90*envelope,0,22*math.sin(w*3)*envelope)
        d['C_Head']=(0,5*envelope,-6*envelope)
    if name=='Cheer':
        for s,sg in [('L',1),('R',-1)]:d[s+'_UpperArm']=(0,sg*(78-125*envelope),0);d[s+'_LowerArm']=(-20*envelope,0,0)
        d['height']=.03*envelope*math.sin(w*2)**2
    if name=='Talk':
        for s,sg in [('L',1),('R',-1)]:d[s+'_UpperArm']=(-12*envelope,sg*70,0);d[s+'_LowerArm']=(-45*envelope*(1+.3*math.sin(w*2+sg)),0,0)
        d['C_Head']=(4*math.sin(w*2)*envelope,0,5*math.sin(w)*envelope)
    if name=='Interact':d['R_UpperArm']=(-65*envelope,-78+45*envelope,0);d['R_LowerArm']=(-18,0,0)
    if name=='PickUp':
        d['height']=-.22*envelope;d['C_Spine']=(45*envelope,0,0)
        for s in ['L','R']:d[s+'_UpperLeg']=(-30*envelope,0,0);d[s+'_LowerLeg']=(65*envelope,0,0);d[s+'_Foot']=(-25*envelope,0,0)
        d['R_UpperArm']=(25*envelope,-78,0)
    if name=='HitReact':d['C_Spine']=(-18*envelope,0,8*envelope);d['C_Head']=(-12*envelope,0,0)
    if name in ['TurnLeft','TurnRight']:d['C_Hips']=(0,0,(12 if name=='TurnLeft' else -12)*envelope)
    return d
SPECS=[('Idle',3,True),('Idle_LookAround',4,True),('Walk',1,True),('Run',.7,True),('WalkBackward',1.2,True),('StrafeLeft',1.1,True),('StrafeRight',1.1,True),('SitDown',1.5,False),('Sit',3,True),('StandUp',1.4,False),('LayDown',3,False),('LieIdle',3,True),('Sleep',4,True),('GetUp',3,False),('CrouchDown',.8,False),('CrouchIdle',2,True),('CrouchWalk',1.2,True),('CrouchUp',.8,False),('Jump',1,False),('Fall',1,True),('Land',.65,False),('Wave',2.5,False),('Talk',3,True),('Cheer',2,False),('Interact',1.5,False),('PickUp',2,False),('HitReact',.6,False)]
scene.render.fps=30; manifest=[]
for name,duration,loop in SPECS:
    action=bpy.data.actions.new(name);action.use_fake_user=True
    arm.animation_data_create();arm.animation_data.action=action
    frames=round(duration*30); duration=frames/30
    for f in range(frames+1):
        d=sample(name,f/30,duration)
        for p in bones:
            angles=d.get(p.name[6:],(0,0,0))
            if p.name.endswith('_LowerArm'):
                # The rest forearm points sideways; elbow flexion is around Z.
                angles=(0,angles[1],angles[2]+angles[0]*(1 if '_L_' in p.name else -1))
            q=Quaternion((0,0,1),math.radians(angles[2])) @ Quaternion((1,0,0),math.radians(angles[0])) @ Quaternion((0,1,0),math.radians(angles[1]))
            p.rotation_quaternion=rest[p.name].inverted() @ q @ rest[p.name]
            p.keyframe_insert('rotation_quaternion',frame=f+1,group=p.name)
        hip.location=rest[hip.name].inverted() @ Vector((0,0,d['height']))
        # Keep the ankle above the reference sole height during deep bends.
        # Compute FK without evaluating meshes on every authored frame.
        matrices={}
        def world_pose(p):
            if p.name not in matrices:
                matrices[p.name]=(world_pose(p.parent) @ p.parent.bone.matrix_local.inverted() @ p.bone.matrix_local @ p.matrix_basis) if p.parent else p.bone.matrix_local @ p.matrix_basis
            return matrices[p.name]
        ankle=min((arm.matrix_world @ world_pose(arm.pose.bones['J_Bip_'+s+'_Foot']).translation).z for s in ['L','R'])
        hip.location+=rest[hip.name].inverted() @ Vector((0,0,max(0,.085-ankle)))
        hip.keyframe_insert('location',frame=f+1,group=hip.name)
    manifest.append({'name':name,'duration':duration,'loop':loop,'frames':frames+1})
    print('ACTION',name,frames+1,flush=True)
arm.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
scene['Instructions']='Action Editor: choose one of 27 SIS actions. Original humanoid bones keyed directly. Spring physics is disabled for deterministic editing. Locomotion is in-place; floor/chair alignment is approximate.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SIS_GameAnimations.blend'))
# Skeleton-only FBX, no secondary motion keys: Unity VRM controls hair and clothes.
for o in bpy.context.selected_objects:o.select_set(False)
arm.select_set(True);bpy.context.view_layer.objects.active=arm
original_data=arm.data
keep=set()
for value in original_data.vrm_addon_extension.vrm1.humanoid.human_bones.human_bone_name_to_human_bone().values():
    b=original_data.bones.get(value.node.bone_name)
    while b:
        keep.add(b.name); b=b.parent
arm.data=original_data.copy();export_data=arm.data
bpy.ops.object.mode_set(mode='EDIT')
for b in list(arm.data.edit_bones):
    if b.name not in keep: arm.data.edit_bones.remove(b)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.fbx(filepath=str(DEST/'SIS_GameAnimations.fbx'),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_bones=False,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
arm.data=original_data;bpy.data.armatures.remove(export_data)
(DEST/'clips.json').write_text(json.dumps({'clips':manifest},indent=2))
human=[]
for key,value in arm.data.vrm_addon_extension.vrm1.humanoid.human_bones.human_bone_name_to_human_bone().items():
    if not value.node.bone_name: continue
    human.append({'human':key.value[0].upper()+key.value[1:],'bone':value.node.bone_name})
(DEST/'humanoid.json').write_text(json.dumps({'bones':human},indent=2))
# Low-resolution contact sheet sources, rendering the actual deformed model.
scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.resolution_x=420;scene.render.resolution_y=420
scene.camera=bpy.data.objects['SIS_FullBody'];scene.camera.data.ortho_scale=2.1
scene.camera.location=(2,-4,2.1)
scene.camera.rotation_euler=(Vector((0,.15,.75))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
for name,frac in [('Idle',0),('Walk',.25),('Run',.25),('Sit',0),('Sleep',0),('CrouchIdle',0),('Wave',.5),('Jump',.5)]:
    arm.animation_data.action=bpy.data.actions[name]
    n=next(x for x in manifest if x['name']==name)
    scene.frame_set(1+round((n['frames']-1)*frac));bpy.context.view_layer.update()
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('SIS_ANIMATIONS_COMPLETE',flush=True)
