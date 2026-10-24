import bpy, sys, importlib, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'ArtSource/Player_Rig'
sys.path.insert(0,str(ROOT/'Tools/SISRig/vendor'))
import addon_utils
addon_utils.enable('VRM_Addon_for_Blender-release',default_set=True,persistent=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'Player_imported.blend'))
scene=bpy.context.scene
arm=bpy.data.objects['Player_Rig']; face=bpy.data.objects['Face']
for obj in list(bpy.data.objects):
    if obj.type in {'CAMERA','LIGHT'} or obj.name=='Cube':bpy.data.objects.remove(obj,do_unlink=True)
ext=arm.data.vrm_addon_extension; spring=ext.spring_bone1
spring.enable_animation=False

# Keep weighted original chains; tune actual VRM spring simulation, not cloth modifiers
# that would be discarded when exported to a game runtime.
for s in spring.springs:
    family=s.vrm_name
    root=s.joints[0].node.bone_name
    s.vrm_name=family+'__'+root.replace('J_Sec_','')
    for j in list(s.joints)[:-1]:
        if family=='Hair':
            j.stiffness=max(.35,min(.75,j.stiffness*.9));j.drag_force=.38
            j.gravity_power=.025;j.hit_radius=max(.003,j.hit_radius)
        elif family=='Skirt':j.stiffness=.85;j.drag_force=.32;j.gravity_power=.04
        elif family=='TopsUpperLeg':j.stiffness=.8;j.drag_force=.35;j.gravity_power=.025
        elif family=='TopsUpperArm':j.stiffness=.9;j.drag_force=.35;j.gravity_power=.02
    if family=='TopsUpperArm':
        side='L' if '_L_' in root else 'R'
        bone=arm.data.bones['J_Bip_'+side+'_UpperArm']
        c=spring.add_collider(bpy.context,arm)
        c.node.bone_name=bone.name
        c.shape_type='Capsule'
        c.reset_bpy_object(bpy.context,arm)
        c.shape.capsule.offset=(0,.025,0)
        c.shape.capsule.tail=(0,bone.length*.85,0)
        c.shape.capsule.radius=.023
        cg=spring.add_collider_group();cg.vrm_name='Sleeve_'+root
        cg.add_collider().collider_uuid=c.uuid
        ref=s.collider_groups.add();ref.collider_group_uuid=cg.uuid

keys=face.data.shape_keys.key_blocks
channels={
'Blink_L':'Fcl_EYE_Close_L','Blink_R':'Fcl_EYE_Close_R',
'Mouth_A':'Fcl_MTH_A','Mouth_I':'Fcl_MTH_I','Mouth_U':'Fcl_MTH_U','Mouth_E':'Fcl_MTH_E','Mouth_O':'Fcl_MTH_O',
}
# Split existing authored deltas at the facial midline with a 4mm feather.
# Preserve the existing UVs, shape keys, expressions, and all source weights.
for label,source in [('Brow_Up','Fcl_BRW_Surprised'),('Brow_Down','Fcl_BRW_Angry'),('Brow_Sad','Fcl_BRW_Sorrow'),('Brow_Happy','Fcl_BRW_Joy'),('Mouth_Smile','Fcl_MTH_Joy'),('Mouth_Frown','Fcl_MTH_Sorrow'),('Eye_Wide','Fcl_EYE_Surprised')]:
    for side,sign in [('L',1),('R',-1)]:
        name=label+'_'+side; key=face.shape_key_add(name='SIS_'+name)
        for v,base,src in zip(key.data,keys[0].data,keys[source].data):
            w=min(1,max(0,.5+sign*base.co.x/.004))
            v.co=base.co+(src.co-base.co)*w
        channels[name]=key.name
        expression=ext.vrm1.expressions.custom.add();expression.custom_name=name
        bind=expression.morph_target_binds.add();bind.node.mesh_object_name=face.name;bind.index=key.name;bind.weight=1

# Export the portable deform rig BEFORE adding Blender-only animation controls.
for obj in bpy.context.selected_objects:obj.select_set(False)
for obj in [arm,face,bpy.data.objects['Body'],bpy.data.objects['Hair']]:obj.select_set(True)
bpy.context.view_layer.objects.active=arm
result=bpy.ops.export_scene.vrm(filepath=str(OUT/'Player_Rigged_Physics.vrm'),armature_object_name=arm.name,use_addon_preferences=False,export_only_selections=True,export_gltf_animations=False,ignore_warning=True)
print('VRM_EXPORT',result,flush=True)
assert result=={'FINISHED'}

# FK control hierarchy. Deform skeleton is preserved with pose-space constraints.
original=[b.name for b in arm.data.bones]
mapped={n:'CTRL_'+n.replace('J_Bip_','') for n in original if n.startswith('J_Bip_')}
mapped['Root']='CTRL_Root'
bpy.context.view_layer.objects.active=arm
bpy.ops.object.mode_set(mode='EDIT')
for orig,name in mapped.items():
    src=arm.data.edit_bones[orig];b=arm.data.edit_bones.new(name)
    b.head=src.head;b.tail=src.tail;b.roll=src.roll;b.use_deform=False
for orig,name in mapped.items():
    src=arm.data.edit_bones[orig];p=src.parent
    while p and p.name not in mapped:p=p.parent
    if p:arm.data.edit_bones[name].parent=arm.data.edit_bones[mapped[p.name]]
for side in ['L','R']:
    for limb,end,joint in [('Hand','Hand','LowerArm'),('Foot','Foot','LowerLeg')]:
        src=arm.data.edit_bones['J_Bip_'+side+'_'+end]
        b=arm.data.edit_bones.new('IK_'+limb+'_'+side)
        b.head=src.head;b.tail=src.tail;b.roll=src.roll;b.use_deform=False
        b.parent=arm.data.edit_bones['CTRL_Root']
        pole=arm.data.edit_bones.new('POLE_'+limb+'_'+side)
        pos=arm.data.edit_bones['J_Bip_'+side+'_'+joint].head.copy()
        pos.y+=.35 if limb=='Hand' else -.4
        pole.head=pos;pole.tail=pos+Vector((0,0,.065));pole.use_deform=False
        pole.parent=arm.data.edit_bones['CTRL_Root']
bpy.ops.object.mode_set(mode='OBJECT')
ctrl_collection=arm.data.collections.new('01_CONTROLS_FK')
ik_collection=arm.data.collections.new('02_CONTROLS_IK')
deform_collection=arm.data.collections.new('03_DEFORM_and_PHYSICS')
for bone in arm.data.bones:
    for collection in list(bone.collections):collection.unassign(bone)
    (ctrl_collection if bone.name.startswith('CTRL_') else ik_collection if bone.name.startswith(('IK_','POLE_')) else deform_collection).assign(bone)
deform_collection.is_visible=False
for orig,name in mapped.items():
    p=arm.pose.bones[orig];c=p.constraints.new('COPY_TRANSFORMS');c.name='SIS FK control'
    c.target=arm;c.subtarget=name;c.target_space='POSE';c.owner_space='POSE'
    arm.pose.bones[name].rotation_mode='XYZ'

def prop(name,desc,lo=0,hi=1,value=0):
    arm[name]=float(value);arm.id_properties_ui(name).update(min=lo,max=hi,soft_min=lo,soft_max=hi,description=desc)
def drive(owner,path,index,property_name,expression='v'):
    f=owner.driver_add(path) if index is None else owner.driver_add(path,index)
    d=f.driver;d.type='SCRIPTED';v=d.variables.new();v.name='v';v.type='SINGLE_PROP'
    v.targets[0].id=arm;v.targets[0].data_path='["'+property_name+'"]';d.expression=expression
    return f
for label,keyname in channels.items():
    prop(label,label.replace('_',' '));drive(keys[keyname],'value',None,label)
for label in ['Eye_Yaw','Eye_Pitch']:prop(label,label.replace('_',' '),-1,1)
for side in ['L','R']:
    p=arm.pose.bones['J_Adj_'+side+'_FaceEye'];p.rotation_mode='XYZ'
    inv=p.bone.matrix_local.to_3x3().inverted()
    yaw=inv@Vector((0,0,1));pitch=inv@Vector((1,0,0))
    for axis in range(3):
        f=drive(p,'rotation_euler',axis,'Eye_Yaw',str(yaw[axis]*.3)+'*v+'+str(pitch[axis]*.24)+'*p')
        v=f.driver.variables.new();v.name='p';v.type='SINGLE_PROP';v.targets[0].id=arm;v.targets[0].data_path='["Eye_Pitch"]'
for side in ['L','R']:
    for limb,lower,end in [('Hand','LowerArm','Hand'),('Foot','LowerLeg','Foot')]:
        label='IK_'+limb+'_'+side;prop(label,'0 = FK, 1 = IK; hand/foot target and pole control')
        p=arm.pose.bones['CTRL_'+side+'_'+lower]
        seed=p.constraints.new('LIMIT_ROTATION');seed.name='IK bend seed'
        seed.owner_space='LOCAL';seed.use_limit_z=True;seed.min_z=.08;seed.max_z=math.pi
        drive(seed,'influence',None,label)
        c=p.constraints.new('IK');c.name=label;c.target=arm;c.subtarget=label
        c.pole_target=arm;c.pole_subtarget='POLE_'+limb+'_'+side
        c.chain_count=2;c.use_stretch=False;c.iterations=64
        drive(c,'influence',None,label)
        c=arm.pose.bones['CTRL_'+side+'_'+end].constraints.new('COPY_ROTATION')
        c.target=arm;c.subtarget=label;c.target_space='POSE';c.owner_space='POSE'
        drive(c,'influence',None,label)

# Clean wire control shapes, stored outside the visible scene collection.
shapes=bpy.data.collections.new('SIS_Control_Shapes');scene.collection.children.link(shapes)
def widget(name,radius=.06):
    me=bpy.data.meshes.new(name)
    points=[(math.cos(i*math.tau/32)*radius,0,math.sin(i*math.tau/32)*radius) for i in range(32)]
    me.from_pydata(points,[(i,(i+1)%32) for i in range(32)],[])
    o=bpy.data.objects.new(name,me);shapes.objects.link(o);o.hide_render=True;o.hide_set(True)
    return o
small=widget('WGT_SIS_FK',.045);large=widget('WGT_SIS_IK',.075);rootshape=widget('WGT_SIS_Root',.3)
for p in arm.pose.bones:
    if p.name.startswith(('CTRL_','IK_','POLE_')):
        p.custom_shape=rootshape if p.name=='CTRL_Root' else large if p.name.startswith('IK_') else small
        p.use_custom_shape_bone_size=False
        p.color.palette='THEME04' if '_L' in p.name else 'THEME03' if '_R' in p.name else 'THEME02'
        if any(x in p.name for x in ['Index','Middle','Ring','Little','Thumb']):p.custom_shape_scale_xyz=(.3,.3,.3)
arm.show_in_front=True

# Render cameras and lighting, separate from the avatar export.
presentation=bpy.data.collections.new('SIS_Presentation');scene.collection.children.link(presentation)
def move_to_presentation(obj):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    presentation.objects.link(obj)
def camera(name,loc,target,scale):
    bpy.ops.object.camera_add(location=loc);o=bpy.context.object;o.name=name
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    o.data.type='ORTHO';o.data.ortho_scale=scale;move_to_presentation(o);return o
cam=camera('Player_FullBody',(2,-5,2.15),(0,0,.83),1.95)
facecam=camera('Player_FaceCamera',(0,-4,1.43),(0,0,1.43),.42)
for pos,power,size in [((1,-3,4),500,4),((-2,-1,2),250,3),((0,2,3),400,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object
    o.data.energy=power;o.data.shape='DISK';o.data.size=size
    o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler();move_to_presentation(o)
scene.world.color=(.18,.18,.18)
scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=900;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
scene.camera=cam;scene.view_settings.view_transform='Standard'
scene.render.fps=30;scene.frame_start=1;scene.frame_end=120
arm['PLAYER_RIG_VERSION']='1.0'
arm['PLAYER_FACE_CHANNELS']=json.dumps(list(channels))
arm['SIS_INFO']='Select rig > N sidebar > Player Rig. Original weights preserved. VRM spring-bone physics.'
for obj in bpy.context.selected_objects:obj.select_set(False)
arm.select_set(True);bpy.context.view_layer.objects.active=arm
for c in spring.colliders:
    if c.bpy_object:
        c.bpy_object.hide_set(True)
        for ch in c.bpy_object.children:ch.hide_set(True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            sp=area.spaces.active;sp.region_3d.view_location=(0,0,.85);sp.region_3d.view_distance=2.6
            sp.region_3d.view_rotation=facecam.rotation_euler.to_quaternion();sp.shading.type='MATERIAL'
            sp.show_region_ui=True
spring.enable_animation=True
scene['SIS_Instructions']='Hair/clothes: N > Player Rig > Enable Animation, play timeline. Use FK controls or IK switches; facial sliders in same panel.'
bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Player_Rigged.blend'))
(OUT/'rig_channels.json').write_text(json.dumps(channels,indent=2),encoding='utf-8')
print('RIG_SAVED',len(arm.data.bones),len(keys),flush=True)
spring.enable_animation=False
scene.render.filepath=str(OUT/'Player_FullBody.png');bpy.ops.render.render(write_still=True)
scene.camera=facecam;scene.render.resolution_x=800;scene.render.resolution_y=800
scene.render.filepath=str(OUT/'Player_Face_Neutral.png');bpy.ops.render.render(write_still=True)
for k,v in [('Blink_L',1),('Mouth_Smile_L',.7),('Mouth_Smile_R',.7),('Brow_Up_R',.8)]:arm[k]=v
arm.update_tag();bpy.context.view_layer.update()
scene.render.filepath=str(OUT/'Player_Face_Controls.png');bpy.ops.render.render(write_still=True)
print('BUILD_COMPLETE',flush=True)
