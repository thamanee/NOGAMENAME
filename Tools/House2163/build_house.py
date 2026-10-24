"""Rebuild the unfurnished reference-plan shell using Blender's bundled Python.
Blender: --background --python Tools/House2163/build_house.py
Coordinates below are metres: x=right, y=back, z=up; front entrance is y=0.
"""
import bpy, math, json, uuid
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/House2163'
SOURCE = ROOT / 'ArtSource/House2163'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
palette = {
    'Plaster': (.79,.77,.69,1), 'Concrete': (.43,.46,.47,1),
    'Floor': (.65,.60,.49,1), 'UpperFloor': (.44,.32,.21,1),
    'Trim': (.085,.11,.12,1), 'WetFloor': (.43,.52,.54,1),
    'Roof': (.22,.27,.28,1), 'Paving': (.48,.48,.45,1),
}
mats = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = .8
    mats[name] = mat
groups = {}
for name in ['00_Site','01_GroundFloor','02_Staircase','03_UpperFloor','04_Roof']:
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    groups[name] = c
parts = []
group = '01_GroundFloor'
def box(name, x0,x1,y0,y1,z0,z1, material='Plaster'):
    assert x1>x0 and y1>y0 and z1>z0, name
    center=((x0+x1)/2,(y0+y1)/2,(z0+z1)/2)
    size=(x1-x0,y1-y0,z1-z0)
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    o=bpy.context.object
    o.name=name
    o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection): c.objects.unlink(o)
    groups[group].objects.link(o)
    o.data.materials.append(mats[material])
    parts.append(dict(name=name,group=group,center=center,size=size,material=material))
    return o

def wall(name,axis,at,a,b,base,openings=(),height=3.0,thick=.15):
    # Opening tuples: start, end, sill, head, relative to finished floor.
    def segment(label,start,end,low,high,mat='Plaster',t=thick):
        if end-start<.001 or high-low<.001:return
        if axis=='x':box(label,start,end,at-t/2,at+t/2,base+low,base+high,mat)
        else:box(label,at-t/2,at+t/2,start,end,base+low,base+high,mat)
    cursor=a
    for i,(s,e,sill,head) in enumerate(sorted(openings)):
        assert a<=s<e<=b and sill<head<=height, name
        segment(name+'_Pier'+str(i),cursor,s,0,height)
        segment(name+'_Sill'+str(i),s,e,0,sill)
        segment(name+'_Lintel'+str(i),s,e,head,height)
        # Frames border clear openings; no panels or furniture.
        segment(name+'_FrameL'+str(i),s-.04,s, max(0,sill),head,'Trim',thick+.025)
        segment(name+'_FrameR'+str(i),e,e+.04,max(0,sill),head,'Trim',thick+.025)
        segment(name+'_FrameTop'+str(i),s,e,head,head+.04,'Trim',thick+.025)
        if sill>0:segment(name+'_FrameBottom'+str(i),s,e,sill-.04,sill,'Trim',thick+.025)
        cursor=e
    segment(name+'_End',cursor,b,0,height)

G=.2
U=3.4
group='00_Site'
box('Parking_5.25x5.5',1.8,7.05,-6.5,-1,-.08,0,'Paving')
box('Entry_Terrace',2,7.6,-.85,0,0,G,'Floor')
box('Entry_Step',2.8,4.2,-1.15,-.85,0,.1,'Concrete')
group='01_GroundFloor'
box('Ground_Slab',0,7.6,0,12,0,G,'Floor')
box('Bathroom_1_Tile',2,3.6,9.3,12,G,G+.012,'WetFloor')
box('Washing_Tile',0,2,9.3,12,G,G+.012,'WetFloor')
wall('Front','x',0,0,7.6,G,[(2.6,3.65,0,2.35),(4.5,6.8,.75,2.5)],thick=.2)
wall('Rear','x',12,0,7.6,G,[(.3,1.5,0,2.3),(2.35,3.15,1.65,2.35),(4.1,6.9,.8,2.5)],thick=.2)
wall('Left','y',0,0,12,G,[(4.6,5.8,.9,2.4),(7.35,8.5,1.1,2.4),(10,11.4,1,2.4)],thick=.2)
wall('RightFront','y',7.6,0,4,G,[(.9,3.1,.75,2.5)],thick=.2)
wall('RightBedroom','y',7.6,8,12,G,[(8.8,11,.8,2.5)],thick=.2)
wall('Bedroom1_Front','x',8,3.6,7.6,G,[(3.85,4.85,0,2.3)])
wall('Bedroom1_Left','y',3.6,8,12,G)
wall('Bathroom1_Left','y',2,9.3,12,G)
wall('Bathroom1_Front','x',9.3,2,3.6,G,[(2.55,3.45,0,2.25)])
wall('Kitchen_LeftZone','y',2,6.8,9.3,G,[(7.8,8.85,0,2.3)])
wall('Kitchen_Pantry','x',6.8,0,2,G)
wall('Kitchen_Washing','x',9.3,0,2,G,[(.65,1.6,0,2.3)])
wall('Dining_Terrace','y',6.1,4,8,G,[(4.55,6.95,0,2.5)])
wall('Terrace_FrontReturn','x',4,6.1,7.6,G)
# Terrace stays open at the outer edge, with a low parapet.
box('Side_Terrace_Paving',6.2,7.6,4.1,7.9,G,G+.015,'Paving')
box('Side_Terrace_Parapet',7.5,7.65,4,8,G,G+.8,'Plaster')
for x in [.04,7.56]:
    for y in [0,4,8,12]:box('Ground_Column',x-.13,x+.13,y-.13,y+.13,G,U,'Concrete')
for x in [2.1,7.5]:box('Front_Upper_Support',x-.13,x+.13,-3.55,-3.29,0,U,'Concrete')
# Small storage enclosure below the low front landing, without furniture.
box('UnderStair_Storage_Back',1.2,2.35,.13,.23,G,G+1.25)

group='02_Staircase'
# U stair: enter the right flight from y=4; turn on the front landing;
# emerge upstairs on the left at y=4.0. Rise 3.2m / 20 = 0.16m.
rise=(U-G)/20
for i in range(9):
    y1=4-i*.3
    box('Flight_A_%02d'%(i+1),1.32,2.42,y1-.3,y1,G,G+(i+1)*rise,'Floor')
box('Half_Landing',.12,2.42,.15,1.3,G,G+10*rise,'Floor')
for i in range(10):
    y0=1.3+i*.27
    box('Flight_B_%02d'%(i+1),.12,1.22,y0,y0+.27,G+10*rise,G+(11+i)*rise,'Floor')
box('Stair_Center_Divider',1.23,1.31,1.3,4,G,U+.9,'Plaster')
for i in range(10):
    y=1.3+i*.27
    top=G+(11+i)*rise
    box('Flight_A_OuterPost_%02d'%i,2.39,2.43,y,y+.035,G+(9-i)*rise,G+(9-i)*rise+.95,'Trim')
# Continuous handrail along the outer edge of the entry flight.
rail_angle=math.atan2(-9*rise,2.7)
rail_length=math.hypot(2.7,9*rise)
handrail=box('Flight_A_Handrail',2.38,2.44,2.65-rail_length/2,2.65+rail_length/2,G+4.5*rise+.92,G+4.5*rise+.98,'Trim')
handrail.rotation_euler.x=rail_angle
parts[-1]['rotation_x']=rail_angle
# Upper stair opening has a guard along its hallway edge and front.
box('Upper_Stair_Guard',2.42,2.5,.15,4,U,U+1.0)

group='03_UpperFloor'
# Separate slab rectangles preserve the full stairwell opening.
box('Upper_Main_Slab',2.5,7.6,0,12,U-.2,U,'UpperFloor')
box('Upper_Rear_Slab',0,2.5,4,12,U-.2,U,'UpperFloor')
box('Master_Slab',2,7.6,-3.6,0,U-.2,U,'UpperFloor')
box('Balcony_Slab',2,7.6,-4.6,-3.6,U-.2,U,'Concrete')
wall('Upper_Left','y',0,0,12,U,[(.6,3,1.3,2.5),(4.7,6.5,.9,2.5),(8.8,11,.8,2.5)],thick=.2)
wall('Upper_Rear','x',12,0,7.6,U,[(.65,3.1,.8,2.5),(4.45,6.95,.8,2.5)],thick=.2)
wall('Upper_Right','y',7.6,-3.6,12,U,[(-2.9,-.8,.8,2.5),(2.8,3.7,1.6,2.4),(6.5,7.4,1.65,2.4),(8.8,11,.8,2.5)],thick=.2)
wall('Upper_FrontLeft','x',0,0,2,U,thick=.2)
wall('Master_Left','y',2,-3.6,0,U,[(-2.9,-.7,.8,2.5)],thick=.2)
wall('Master_Balcony','x',-3.6,2,7.6,U,[(3.1,5.5,0,2.5)],thick=.2)
wall('Bedroom23_Divider','y',3.8,8,12,U)
wall('Bedroom2_Front','x',8,0,3.8,U,[(2.55,3.6,0,2.3)])
wall('Bedroom3_Front','x',8,3.8,7.6,U,[(4,5.05,0,2.3)])
wall('AC_Closet_Front','x',6.3,0,1.4,U,[(.25,1.15,0,2.2)])
wall('AC_Closet_Right','y',1.4,6.3,8,U)
wall('WC3_Front','x',6.2,4.9,7.6,U)
wall('WC3_Left','y',4.9,6.2,8,U,[(6.7,7.6,0,2.25)])
wall('WC2_Left','y',3.8,4.2,6.2,U,[(4.45,5.35,0,2.25)])
wall('WC2_Front','x',4.2,3.8,6.6,U)
wall('WC2_Rear','x',6.2,3.8,4.9,U)
wall('WC2_Right','y',6.6,4.2,6.2,U)
wall('Onsen_Left','y',4.9,2.5,4.2,U,[(2.85,3.75,0,2.25)])
wall('Onsen_Front','x',2.5,4.9,7.6,U)
wall('Onsen_RearReturn','x',4.2,6.6,7.6,U)
wall('Master_WIC','x',0,2,7.6,U,[(2.65,3.7,0,2.3)])
for name,x0,x1,y0,y1 in [('WC2',3.9,6.5,4.3,6.1),('WC3',5,7.5,6.3,7.9),('Onsen',5,7.5,2.6,4.1)]:
    box(name+'_Tile',x0,x1,y0,y1,U,U+.012,'WetFloor')
# Open-sided balcony with substantial parapets.
box('Balcony_Front_Parapet',2,7.6,-4.6,-4.47,U,U+1.05)
box('Balcony_Left_Parapet',2,2.13,-4.6,-3.6,U,U+1.05)
box('Balcony_Right_Parapet',7.47,7.6,-4.6,-3.6,U,U+1.05)

group='04_Roof'
# Reference has no roof/elevation. A removable flat roof is a deliberate assumption.
box('Roof_Main',-.2,7.8,-.2,12.2,U+3,U+3.22,'Roof')
box('Roof_Master',1.8,7.8,-3.8,-.2,U+3,U+3.22,'Roof')
for name,axis,at,a,b in [('RoofRear','x',12.1,-.1,7.7),('RoofLeft','y',-.1,-.1,12.1),('RoofRight','y',7.7,-3.7,12.1),('RoofFront','x',-3.7,1.9,7.7),('RoofInset','y',1.9,-3.7,-.1),('RoofReturn','x',-.1,-.1,1.9)]:
    wall(name,axis,at,a,b,U+3.22,height=.35,thick=.12)

# Native Unity prefab uses shared built-in cube meshes and individual BoxColliders.
# This avoids reliance on an importer running before the deliverable exists.
def guid(s):return uuid.uuid5(uuid.NAMESPACE_URL,'House2163/'+s).hex
def meta(path,kind,extra=''):
    Path(str(path)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(str(path.relative_to(OUT)))+'\n'+kind+':\n'+extra,encoding='utf-8')
material_dir=OUT/'Materials'
material_dir.mkdir(exist_ok=True)
for name,col in palette.items():
    p=material_dir/(name+'.mat')
    color='{r: %s, g: %s, b: %s, a: 1}'%col[:3]
    p.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: '''+name+'''
  m_Shader: {fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 1
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Smoothness: 0.15
    - _Metallic: 0
    - _Surface: 0
    - _Cull: 2
    m_Colors:
    - _BaseColor: '''+color+'\n    - _Color: '+color+'\n',encoding='utf-8')
    meta(p,'NativeFormatImporter','  externalObjects: {}\n  mainObjectFileID: 2100000\n')

lines=['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:']
def vec(v):return '{x: %.6f, y: %.6f, z: %.6f}'%tuple(v)
def node(i,name,parent,children,center=(0,0,0),size=(1,1,1),material=None):
    go=i; tr=i+1
    comp=[tr]+([i+2,i+3,i+4] if material else [])
    lines.extend([f'--- !u!1 &{go}','GameObject:','  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}','  m_PrefabInstance: {fileID: 0}','  m_PrefabAsset: {fileID: 0}','  serializedVersion: 6','  m_Component:']+[f'  - component: {{fileID: {j}}}' for j in comp]+['  m_Layer: 0',f'  m_Name: {name}','  m_TagString: Untagged','  m_Icon: {fileID: 0}','  m_NavMeshLayer: 0','  m_StaticEditorFlags: 0','  m_IsActive: 1',f'--- !u!4 &{tr}','Transform:','  m_ObjectHideFlags: 0',f'  m_GameObject: {{fileID: {go}}}','  serializedVersion: 2','  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}','  m_LocalPosition: '+vec(center),'  m_LocalScale: '+vec(size),'  m_ConstrainProportionsScale: 0','  m_Children:'+('' if children else ' []')]+[f'  - {{fileID: {j+1}}}' for j in children]+[f'  m_Father: {{fileID: {parent+1 if parent else 0}}}','  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}'])
    if material:
        mg=guid('Materials/'+material+'.mat')
        lines.extend([f'--- !u!33 &{i+2}','MeshFilter:',f'  m_GameObject: {{fileID: {go}}}','  m_Mesh: {fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}',f'--- !u!23 &{i+3}','MeshRenderer:',f'  m_GameObject: {{fileID: {go}}}','  m_Enabled: 1','  m_CastShadows: 1','  m_ReceiveShadows: 1','  m_Materials:',f'  - {{fileID: 2100000, guid: {mg}, type: 2}}','  m_LightProbeUsage: 1','  m_ReflectionProbeUsage: 1','  m_RenderingLayerMask: 1','  m_ScaleInLightmap: 1',f'--- !u!65 &{i+4}','BoxCollider:',f'  m_GameObject: {{fileID: {go}}}','  m_Material: {fileID: 0}','  m_IsTrigger: 0','  m_Enabled: 1','  serializedVersion: 3','  m_Size: {x: 1, y: 1, z: 1}','  m_Center: {x: 0, y: 0, z: 0}'])
group_ids={name:100+idx*10 for idx,name in enumerate(groups)}
node(1,'House2163_Shell',0,list(group_ids.values()))
for name,ident in group_ids.items():
    node(ident,name,1,[1000+j*10 for j,p in enumerate(parts) if p['group']==name])
for j,p in enumerate(parts):
    x,y,z=p['center'];sx,sy,sz=p['size']
    node(1000+j*10,p['name'],group_ids[p['group']],[],(x,z,y),(sx,sz,sy),p['material'])
    if 'rotation_x' in p:
        # Swapping Blender Y/Z changes handedness, so negate rotation about X.
        for idx in range(len(lines)-1,-1,-1):
            if lines[idx].startswith('  m_LocalRotation:'):
                a=-p['rotation_x']/2
                lines[idx]='  m_LocalRotation: {x: %.8f, y: 0, z: 0, w: %.8f}'%(math.sin(a),math.cos(a))
                break
prefab=OUT/'House2163_Shell.prefab'
prefab.write_text('\n'.join(lines)+'\n',encoding='utf-8')
meta(prefab,'PrefabImporter','  externalObjects: {}\n')
(SOURCE/'geometry.json').write_text(json.dumps(parts,indent=2),encoding='utf-8')

bpy.ops.object.select_all(action='DESELECT')
for c in groups.values():
    for o in c.objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'House2163_Shell.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False)

# Render-only presentation scene; camera and lights are excluded from FBX/prefab.
world=scene.world or bpy.data.worlds.new('World')
scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.38,.45,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.65
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
bpy.ops.object.light_add(type='AREA',location=(2,-4,18))
bpy.context.object.data.energy=2300;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=12
bpy.ops.object.light_add(type='SUN',location=(0,0,15))
bpy.context.object.rotation_euler=(.35,-.4,-.4);bpy.context.object.data.energy=2
bpy.ops.object.camera_add(location=(21,-26,24))
camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO';camera.data.ortho_scale=25
def aim(position,target,scale):
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
aim((21,-26,24),(3.8,3,2.2),25)
scene['Reference']='02.Plan-RH-MD.2163_.jpg; approximate traced floor plan, not construction drawings.'
scene['Dimensions']='Metres; main footprint 7.6 x 12; storey rise 3.2; exterior walls 0.2; interior walls 0.15.'
scene['Assumptions']='Flat removable roof; inferred elevations and window sizes; no furniture or sanitary fittings.'
reference=Path('C:/Users/apich/Downloads/02.Plan-RH-MD.2163_.jpg')
if reference.exists():
    reference_image=bpy.data.images.load(str(reference),check_existing=True)
    reference_image.use_fake_user=True
    reference_image.pack()
bpy.ops.object.select_all(action='DESELECT')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=27
            area.spaces.active.region_3d.view_location=(3.8,3,2.2)
            area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
            area.spaces.active.shading.color_type='MATERIAL'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'House2163_Shell.blend'))
scene.render.filepath=str(SOURCE/'Exterior.png');bpy.ops.render.render(write_still=True)
groups['04_Roof'].hide_render=True
aim((18,-20,31),(3.8,3.4,2.7),28)
scene.render.filepath=str(SOURCE/'Upper_Cutaway.png');bpy.ops.render.render(write_still=True)
groups['03_UpperFloor'].hide_render=True
aim((16,-18,29),(3.8,3,1.2),26)
scene.render.filepath=str(SOURCE/'Ground_Cutaway.png');bpy.ops.render.render(write_still=True)
print('HOUSE_COMPLETE',len(parts),'boxes;',len(parts)*12,'triangles')
