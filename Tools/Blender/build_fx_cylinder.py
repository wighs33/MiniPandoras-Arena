"""
스킬 효과의 원기둥(불꽃 고리의 불벽, 검은 날개의 기운 벽, 충격파)을 코드로 짓는다. 위아래가 뚫린 원기둥이다.

    blender --background --python Tools/Blender/build_fx_cylinder.py

반지름 1m, 높이 1m, 밑이 원점이다. UV는 가로가 둘레(두 바퀴 반복), 세로가 높이(아래 0, 위 1)라서
셰이더(ToonFx)가 노이즈를 위로 흘리면 벽이 타오르듯 보인다. 유니티에서 크기로 반지름과 높이를 정한다.
결과는 Assets/MiniPandoras/Art/Models/FxCylinder.fbx.
"""

import math
import os

import bmesh
import bpy

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPORT_PATH = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Models", "FxCylinder.fbx")

SIDES = 40
AROUND_REPEAT = 2.0


def build():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    bottom, top = [], []
    for i in range(SIDES + 1):
        angle = 2 * math.pi * i / SIDES
        x, y = math.cos(angle), math.sin(angle)
        bottom.append(bm.verts.new((x, y, 0.0)))
        top.append(bm.verts.new((x, y, 1.0)))
    for i in range(SIDES):
        face = bm.faces.new((bottom[i], bottom[i + 1], top[i + 1], top[i]))
        u0, u1 = AROUND_REPEAT * i / SIDES, AROUND_REPEAT * (i + 1) / SIDES
        for loop, uv in zip(face.loops, ((u0, 0.0), (u1, 0.0), (u1, 1.0), (u0, 1.0))):
            loop[uv_layer].uv = uv
    # 이음매의 겹친 꼭짓점은 UV 때문에 남겨 둔다(합치면 둘레 UV가 끊긴다).
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("FxCylinder")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(bpy.data.materials.get("FxCylinder") or bpy.data.materials.new("FxCylinder"))
    mesh.shade_smooth()
    obj = bpy.data.objects.new("FxCylinder", mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def export(obj):
    obj.select_set(True)
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
