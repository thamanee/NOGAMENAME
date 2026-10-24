import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/SIS_Animations/SIS_GameAnimations.blend'))
arm=bpy.data.objects['SIS_Rig'];scene=bpy.context.scene
specs=json.loads((root/'Assets/SISAnimations/clips.json').read_text())['clips']
report=[]
for spec in specs:
    arm.animation_data.action=bpy.data.actions[spec['name']]
    endposes=[];minfoot=100;maxfoot=-100
    for f in range(1,spec['frames']+1):
        scene.frame_set(f);bpy.context.view_layer.update()
        for p in arm.pose.bones:
            assert all(math.isfinite(x) for row in p.matrix for x in row),(spec['name'],f,p.name)
        feet=[(arm.matrix_world@arm.pose.bones['J_Bip_'+s+'_Foot'].head).z for s in ['L','R']]
        minfoot=min(minfoot,*feet);maxfoot=max(maxfoot,*feet)
        if f in [1,spec['frames']]:endposes.append({p.name:p.matrix.copy() for p in arm.pose.bones if p.name.startswith('J_Bip_')})
    error=max(abs(endposes[0][n][i][j]-endposes[1][n][i][j]) for n in endposes[0] for i in range(4) for j in range(4))
    if spec['loop']:assert error<.0001,(spec['name'],error)
    report.append({'name':spec['name'],'loop':spec['loop'],'loop_matrix_error':error,'min_ankle_z':minfoot,'max_ankle_z':maxfoot})
(root/'ArtSource/SIS_Animations/validation.json').write_text(json.dumps({'finite_matrices':True,'loops_match':True,'clips':report},indent=2))
print('VALIDATED',len(report),'actions',flush=True)
