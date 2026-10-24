import bpy,sys,importlib,math,json,struct
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtSource/Player_Rig'
sys.path.insert(0,str(ROOT/'Tools/SISRig/vendor'))
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
arm=bpy.data.objects['Player_Rig'];face=bpy.data.objects['Face'];scene=bpy.context.scene
spring=arm.data.vrm_addon_extension.spring_bone1;spring.enable_animation=False
handler=importlib.import_module('VRM_Addon_for_Blender-release.editor.spring_bone1.handler')
report={}
channels=json.loads((OUT/'rig_channels.json').read_text())
def update():arm.update_tag();bpy.context.view_layer.update()
def reset():
    for p in arm.pose.bones:p.matrix_basis=Matrix.Identity(4)
    for k in channels:arm[k]=0.0
    for k in ['Eye_Yaw','Eye_Pitch','IK_Hand_L','IK_Hand_R','IK_Foot_L','IK_Foot_R']:arm[k]=0.0
    update()
reset()
face_results={}
for label,keyname in channels.items():
    arm[label]=.75;update()
    key=face.data.shape_keys.key_blocks[keyname]
    delta=max((v.co-b.co).length for v,b in zip(key.data,face.data.shape_keys.key_blocks[0].data))
    assert abs(key.value-.75)<.001 and delta>1e-6,(label,key.value,delta)
    face_results[label]={'driven_value':round(key.value,3),'max_authored_delta_m':delta}
    arm[label]=0.;update()
report['facial_channels']=face_results
arm['Eye_Yaw']=.8;arm['Eye_Pitch']=.5;update()
assert all(arm.pose.bones['J_Adj_'+s+'_FaceEye'].rotation_euler.to_quaternion().angle>.05 for s in ['L','R'])
reset()

# IK: calibrate pole angle to bend toward the pole and validate target tracking.
ik_results={}
for side in ['L','R']:
    for limb,lower,upper,end in [('Hand','LowerArm','UpperArm','Hand'),('Foot','LowerLeg','UpperLeg','Foot')]:
        label='IK_'+limb+'_'+side;target=arm.pose.bones[label]
        arm[label]=1.
        rest=target.bone.matrix_local.copy()
        target.matrix=Matrix.Translation(Vector((-.07 if side=='L' else .07,-.06,.04)) if limb=='Hand' else Vector((0,-.06,.09)))@rest
        lowerbone=arm.pose.bones['CTRL_'+side+'_'+lower];upperbone=arm.pose.bones['CTRL_'+side+'_'+upper]
        if not lowerbone.constraints.get('IK bend seed'):
            seed=lowerbone.constraints.new('LIMIT_ROTATION');seed.name='IK bend seed'
            seed.owner_space='LOCAL';seed.use_limit_z=True;seed.min_z=.08;seed.max_z=math.pi
            f=seed.driver_add('influence');v=f.driver.variables.new();v.name='v';v.type='SINGLE_PROP'
            v.targets[0].id=arm;v.targets[0].data_path='["'+label+'"]';f.driver.expression='v'
            lowerbone.constraints.move(len(lowerbone.constraints)-1,0)
        c=lowerbone.constraints[label];best=(-1,None)
        for i in range(72):
            c.pole_angle=-math.pi+i*math.tau/72;update()
            root=upperbone.head;axis=(target.head-root).normalized()
            actual=lowerbone.head-root;actual-=axis*actual.dot(axis)
            wanted=arm.pose.bones['POLE_'+limb+'_'+side].head-root;wanted-=axis*wanted.dot(axis)
            score=actual.normalized().dot(wanted.normalized())
            if score>best[0]:best=(score,c.pole_angle)
        c.pole_angle=best[1];update()
        error=(lowerbone.tail-target.head).length
        print('IK_DEBUG',label,'error',error,'influence',c.influence,'target',list(target.head),'tail',list(lowerbone.tail),'upper',list(upperbone.head),'pole',list(arm.pose.bones['POLE_'+limb+'_'+side].head),flush=True)
        ik_results[label]={'endpoint_error_m':error,'pole_alignment':best[0],'pole_angle':best[1]}
        assert error<.012,(label,error)
        reset()
report['ik']=ik_results
spring.enable_animation=True
handler.reset_state(bpy.context)
families={};physics_states=[]
for frame in range(1,91):
    t=(frame-1)/30
    arm.pose.bones['CTRL_C_Hips'].location.x=.035*math.sin(t*math.tau)
    arm.pose.bones['CTRL_C_Head'].rotation_euler.z=.2*math.sin(t*math.tau*1.1)
    for side,sign in [('L',1),('R',-1)]:
        arm.pose.bones['CTRL_'+side+'_UpperArm'].rotation_euler.z=sign*.18*math.sin(t*math.tau*.8)
        arm.pose.bones['CTRL_'+side+'_UpperLeg'].rotation_euler.x=sign*.12*math.sin(t*math.tau*.8)
    update();handler.update_pose_bone_rotations(bpy.context,1/60);update();handler.update_pose_bone_rotations(bpy.context,1/60);update()
    for s in spring.springs:
        family=s.vrm_name.split('__')[0]
        for j in list(s.joints)[:-1]:
            p=arm.pose.bones[j.node.bone_name]
            angle=p.matrix_basis.to_quaternion().angle
            assert math.isfinite(angle)
            families[family]=max(families.get(family,0),angle)
    physics_states.append({p.name:list(p.matrix_basis.to_quaternion()) for p in arm.pose.bones if p.name.startswith('J_Sec_')})
    if frame%30==0:print('PHYSICS_FRAME',frame,flush=True)
for family in ['Hair','TopsUpperArm']:
    assert families[family]>.003,(family,families)
report['physics_max_rotation_degrees']={k:math.degrees(v) for k,v in families.items()}
(OUT/'physics_samples.json').write_text(json.dumps(physics_states),encoding='utf-8')
spring.enable_animation=False;reset();handler.reset_state(bpy.context)
for s in spring.springs:
    for j in s.joints:j.animation_state.initialized_as_tail=False
spring.enable_animation=True
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Player_Rigged.blend'))

# Portable VRM structure and preservation checks.
raw=(OUT/'Player_Rigged_Physics.vrm').read_bytes();size=struct.unpack_from('<I',raw,12)[0];d=json.loads(raw[20:20+size]);ex=d['extensions']
source=json.loads((OUT/'source_gltf.json').read_text(encoding='utf-8'))
source_humanoid=source['extensions']['VRMC_vrm']['humanoid']['humanBones']
assert set(ex['VRMC_vrm']['humanoid']['humanBones'])==set(source_humanoid)
assert len(ex['VRMC_springBone']['springs'])==22
assert len(ex['VRMC_vrm']['expressions']['custom'])==14
for s in ex['VRMC_springBone']['springs']:
    for j in s['joints']:assert j['node']<len(d['nodes'])
    for group in s.get('colliderGroups',[]):assert group<len(ex['VRMC_springBone']['colliderGroups'])
report['vrm']={'humanoid_bones':len(source_humanoid),'spring_chains':22,'custom_expressions':14,'colliders':len(ex['VRMC_springBone']['colliders'])}
report['packed_images']=sum(bool(i.packed_file) for i in bpy.data.images)
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VALIDATION_COMPLETE',json.dumps({k:v for k,v in report.items() if k!='facial_channels'}),flush=True)
