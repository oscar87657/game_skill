# GOLDEN STANDARD
# 목적: CC0 VRoid 샘플을 Unity 기본 FBX/Humanoid 경로로 변환한다.
# 책임: 모델·기본색 텍스처·본 매핑을 추출하며 VRM 런타임 패키지는 추가하지 않는다.
# 실행: Blender --background --python Tools/convert_shino.py -- source.glb output-directory
import hashlib
import json
import math
from pathlib import Path
import struct
import sys

import bpy

source, destination = map(Path, sys.argv[sys.argv.index('--') + 1:])
destination.mkdir(parents=True, exist_ok=True)
(destination / 'Textures').mkdir(exist_ok=True)
raw = source.read_bytes()
# 고정한 배포본만 변환해 외부 모델의 경로·본·라이선스 데이터를 무검증으로 쓰지 않는다.
if hashlib.sha256(raw).hexdigest() != "1e177c1a7b14f783a9c48395831db8616260d3bddd4154cb2784b779adca49b5":
    raise ValueError("Expected the documented Sendagaya Shino source revision")
assert raw[:4] == b'glTF', 'VRM/glTF binary required'
json_size = struct.unpack_from('<I', raw, 12)[0]
data = json.loads(raw[20:20 + json_size])
binary = raw[28 + json_size:]
vrm = data['extensions']['VRM']
assert vrm['meta']['licenseName'] == 'CC0', 'Review model license before conversion'

# Unity HumanTrait는 공백을 포함한 손가락 이름을 사용하므로 원본 본 이름과 함께 보존한다.
bones = []
for bone in vrm['humanoid']['humanBones']:
    name = bone['bone'][0].upper() + bone['bone'][1:]
    for side in ('Left', 'Right'):
        for finger in ('Thumb', 'Index', 'Middle', 'Ring', 'Little'):
            prefix = side + finger
            if name.startswith(prefix):
                name = side + ' ' + finger + ' ' + name[len(prefix):]
    bones.append({'human': name, 'bone': data['nodes'][bone['node']]['name']})

materials = []
for material in data['materials']:
    pbr = material['pbrMetallicRoughness']
    image_index = data['textures'][pbr['baseColorTexture']['index']]['source']
    image = data['images'][image_index]
    assert image['mimeType'] == 'image/png'
    view = data['bufferViews'][image['bufferView']]
    filename = f'Textures/{image_index:02d}_{image["name"]}.png'
    start = view.get('byteOffset', 0)
    (destination / filename).write_bytes(binary[start:start + view['byteLength']])
    materials.append({'name': material['name'], 'texture': filename,
                      'color': pbr.get('baseColorFactor', [1, 1, 1, 1]),
                      'alpha': material.get('alphaMode', 'OPAQUE') != 'OPAQUE'})
(destination / 'ImportMap.json').write_text(json.dumps({'bones': bones, 'materials': materials}, indent=2))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
# ponytail: Spring Bone은 FBX로 옮기지 않는다. 머리카락·의상 물리가 필요하면 VRM importer로 전환한다.
bpy.ops.import_scene.gltf(filepath=str(source))
# VRM 0.x는 glTF 안에서 뒤를 바라보므로 루트 방향을 Unity의 전방에 맞춘다.
for obj in bpy.context.scene.objects:
    if obj.parent is None:
        obj.rotation_euler.z += math.pi
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(destination / 'SendagayaShino.fbx'),
    use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
    bake_anim=False, path_mode='STRIP', use_mesh_modifiers=False,
    axis_forward='-Z', axis_up='Y')
print('Converted CC0 Sendagaya Shino:', destination)
