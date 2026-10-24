bl_info={'name':'Player Rig Controls','author':'Player project','version':(1,0,0),'blender':(4,2,0),'category':'Rigging','description':'Body, facial and VRM spring physics controls for PLAYER'}
import bpy,json
from mathutils import Matrix

def rig(context):
    o=context.object
    if o and o.type=='ARMATURE' and o.get('PLAYER_RIG_VERSION'):return o
    return next((o for o in context.scene.objects if o.type=='ARMATURE' and o.get('PLAYER_RIG_VERSION')),None)

class PLAYER_OT_neutral(bpy.types.Operator):
    bl_idname='player.neutral';bl_label='Reset face';bl_options={'REGISTER','UNDO'}
    def execute(self,context):
        o=rig(context)
        for k in json.loads(o.get('PLAYER_FACE_CHANNELS','[]'))+['Eye_Yaw','Eye_Pitch']:o[k]=0.0
        o.update_tag();context.view_layer.update();return {'FINISHED'}

class PLAYER_OT_pose_reset(bpy.types.Operator):
    bl_idname='player.pose_reset';bl_label='Reset body pose';bl_options={'REGISTER','UNDO'}
    def execute(self,context):
        o=rig(context)
        for p in o.pose.bones:
            if p.name.startswith(('CTRL_','IK_','POLE_')):p.matrix_basis=Matrix.Identity(4)
        for k in ['IK_Hand_L','IK_Hand_R','IK_Foot_L','IK_Foot_R']:o[k]=0.0
        o.update_tag();return {'FINISHED'}

class PLAYER_OT_physics_reset(bpy.types.Operator):
    bl_idname='player.physics_reset';bl_label='Reset spring simulation';bl_options={'REGISTER','UNDO'}
    def execute(self,context):
        o=rig(context);s=o.data.vrm_addon_extension.spring_bone1
        enabled=s.enable_animation;s.enable_animation=False
        for chain in s.springs:
            for j in chain.joints:
                p=o.pose.bones.get(j.node.bone_name)
                if p:p.matrix_basis=Matrix.Identity(4)
        s.enable_animation=enabled;return {'FINISHED'}

class PLAYER_PT_controls(bpy.types.Panel):
    bl_label='Player Rig';bl_idname='PLAYER_PT_controls';bl_space_type='VIEW_3D';bl_region_type='UI';bl_category='Player Rig'
    @classmethod
    def poll(cls,context):return rig(context) is not None
    def draw(self,context):
        o=rig(context);layout=self.layout
        layout.label(text='PLAYER • body / face / secondary motion')
        box=layout.box();box.label(text='Body: Pose Mode, select CTRL / IK bones')
        for k in ['IK_Hand_L','IK_Hand_R','IK_Foot_L','IK_Foot_R']:box.prop(o,'["'+k+'"]',text=k.replace('_',' '),slider=True)
        box.operator('player.pose_reset')
        box=layout.box();box.label(text='Hair and clothing physics')
        try:
            box.prop(o.data.vrm_addon_extension.spring_bone1,'enable_animation',text='Enable animation (play timeline)')
            box.operator('player.physics_reset')
            box.label(text='VRM > Spring Bone: per-chain settings')
        except AttributeError:box.label(text='Enable the VRM format add-on')
        keys=json.loads(o.get('PLAYER_FACE_CHANNELS','[]'))
        for title,prefixes in [('Eyes',['Blink','Eye_']),('Eyebrows',['Brow_']),('Mouth / vowels',['Mouth_'])]:
            box=layout.box();box.label(text=title)
            items=keys+(['Eye_Yaw','Eye_Pitch'] if title=='Eyes' else [])
            for k in items:
                if any(k.startswith(p) for p in prefixes):box.prop(o,'["'+k+'"]',text=k.replace('_',' '),slider=True)
        layout.operator('player.neutral')

CLASSES=(PLAYER_OT_neutral,PLAYER_OT_pose_reset,PLAYER_OT_physics_reset,PLAYER_PT_controls)
def register():
    for c in CLASSES:bpy.utils.register_class(c)
def unregister():
    for c in reversed(CLASSES):bpy.utils.unregister_class(c)
if __name__=='__main__':register()
