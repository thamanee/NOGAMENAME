from pathlib import Path
import json,struct
root=Path(__file__).resolve().parents[2]
dest=root/'Tools/PlayerRig';dest.mkdir(exist_ok=True)
out=root/'ArtSource/Player_Rig';out.mkdir(exist_ok=True,parents=True)
raw=Path('E:/NONAMEGAME MODEL/player.vrm').read_bytes()
data=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
(out/'source_gltf.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
for name in ['import_inspect.py','build_rig.py','validate_rig.py','make_demo.py']:
    text=(root/'Tools/SISRig'/name).read_text(encoding='utf-8')
    text=text.replace("str(Path(__file__).parent/'vendor')","str(ROOT/'Tools/SISRig/vendor')")
    text=text.replace('E:/NONAMEGAME MODEL/SIS.vrm','E:/NONAMEGAME MODEL/player.vrm')
    text=text.replace('ArtSource/SIS_Rig','ArtSource/Player_Rig')
    text=text.replace('SIS_Rig','Player_Rig').replace('SIS_imported','Player_imported')
    text=text.replace('SIS_RIG_VERSION','PLAYER_RIG_VERSION').replace('SIS_FACE_CHANNELS','PLAYER_FACE_CHANNELS').replace('SIS Rig','Player Rig')
    for part in ['FullBody','FaceCamera','Face_Neutral','Face_Controls','Physics_Demo','Physics_Pose','Demo_Face']:
        text=text.replace('SIS_'+part,'Player_'+part)
    if name=='import_inspect.py':
        text=text.replace("arm.name='Player_Rig'", "arm.name='Player_Rig'\nfor o in list(bpy.data.objects):\n if o.type=='MESH':\n  if o.data.shape_keys:o.name='Face'\n  elif o.name.lower().startswith('hair'):o.name='Hair'\n  elif o.name.lower().startswith('body'):o.name='Body'")
    if name=='validate_rig.py':
        text=text.replace("['Hair','Skirt','TopsUpperLeg','TopsUpperArm']","['Hair','TopsUpperArm']")
        text=text.replace("==37","==22").replace("'spring_chains':37","'spring_chains':22")
    (dest/name).write_text(text,encoding='utf-8')
helper=(root/'Tools/SISRig/sis_rig_tools.py').read_text(encoding='utf-8').replace('SIS','PLAYER').replace('sis.','player.').replace('PLAYER Rig','Player Rig').replace('PLAYER project','Player project')
(dest/'player_rig_tools.py').write_text(helper,encoding='utf-8')
print('Player rig pipeline prepared: 22 spring chains')
