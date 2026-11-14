"""
악마 판도라의 가시(가시 덫, 가시 숲)를 코드로 짓는다. 다섯 면의 로우폴리 결정 하나다.

    blender --background --python Tools/Blender/build_spike.py

밑면 가운데가 원점이고 끝이 위(유니티의 +Y)로 솟는다. 높이 1m라서 유니티에서 크기로 키를 정한다.
면이 각지게 보이도록 평평한 음영으로 내보낸다. 재질 이름(Spike)은 유니티의 같은 이름 툰 재질로 바뀐다.
결과는 Assets/MiniPandoras/Art/Models/Spike.fbx.
"""

import math
import os

import bmesh
import bpy

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPORT_PATH = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Models", "Spike.fbx")

SIDES = 5
# (높이, 반지름, 비틀림 각도) — 밑에서 조금 부풀었다가 끝으로 가늘어지는 결정.
RINGS = [(0.0, 0.15, 0.0), (0.18, 0.17, 8.0), (0.55, 0.10, 20.0)]
TIP_HEIGHT = 1.0


def build():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bm = bmesh.new()
    rings = []
    for height, radius, twist in RINGS:
        ring = []
        for i in range(SIDES):
            angle = 2 * math.pi * i / SIDES + math.radians(twist)
            # 블렌더는 Z가 위다. 내보낼 때 유니티의 Y로 바뀐다.
            ring.append(bm.verts.new((math.cos(angle) * radius, math.sin(angle) * radius, height)))
        rings.append(ring)
    for lower, upper in zip(rings, rings[1:]):
        for i in range(SIDES):
            bm.faces.new((lower[i], lower[(i + 1) % SIDES], upper[(i + 1) % SIDES], upper[i]))
    tip = bm.verts.new((0.0, 0.0, TIP_HEIGHT))
    top = rings[-1]
    for i in range(SIDES):
        bm.faces.new((top[i], top[(i + 1) % SIDES], tip))
    bm.faces.new(list(reversed(rings[0])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("Spike")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(bpy.data.materials.get("Spike") or bpy.data.materials.new("Spike"))
    mesh.shade_flat()
    spike = bpy.data.objects.new("Spike", mesh)
    bpy.context.scene.collection.objects.link(spike)
    return spike


def export(spike):
    spike.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=EXPORT_PATH,
        use_selection=True,
        object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        bake_anim=False,
    )
    print("exported", EXPORT_PATH)


export(build())
