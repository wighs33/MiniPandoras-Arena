"""
방망이(2D판 주인공의 무기)를 코드로 짓는다.

    blender --background --python Tools/Blender/build_bat.py

손잡이가 원점이고 끝이 유니티의 -Z로 뻗는다. 유니티에서 인물의 오른손 뼈 아래에 붙인다.
결과는 Assets/MiniPandoras/Art/Models/Bat.fbx. 재질 이름(Wood, Grip)은 유니티의 같은 이름 툰 재질로 바뀐다.
"""

import math
import os

import bmesh
import bpy

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPORT_PATH = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Models", "Bat.fbx")

# 손잡이 끝에서 잰 길이(m)와 그 자리의 반지름(m)
PROFILE = [(0.0, 0.020), (0.018, 0.024), (0.16, 0.019), (0.30, 0.028), (0.50, 0.037), (0.56, 0.034)]
SIDES = 8
# 손이 쥐는 자리(손잡이 끝에서 6cm)를 원점에 둔다.
GRIP_OFFSET = 0.06
# 손잡이 끝에서 이 길이까지는 감은 띠(Grip), 그 위는 나무(Wood)다.
GRIP_LENGTH = 0.17


def build():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bm = bmesh.new()
    rings = []
    for length, radius in PROFILE:
        rings.append([bm.verts.new((math.cos(2 * math.pi * i / SIDES) * radius, length - GRIP_OFFSET,
                                    math.sin(2 * math.pi * i / SIDES) * radius)) for i in range(SIDES)])
    for lower, upper in zip(rings, rings[1:]):
        for i in range(SIDES):
            bm.faces.new((lower[i], lower[(i + 1) % SIDES], upper[(i + 1) % SIDES], upper[i]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("Bat")
    bm.to_mesh(mesh)
    bm.free()
    for name in ("Wood", "Grip"):
        mesh.materials.append(bpy.data.materials.get(name) or bpy.data.materials.new(name))
    for poly in mesh.polygons:
        poly.material_index = 1 if poly.center.y < GRIP_LENGTH - GRIP_OFFSET else 0
    mesh.shade_smooth()
    bat = bpy.data.objects.new("Bat", mesh)
    bpy.context.scene.collection.objects.link(bat)
    return bat


def export(bat):
    bat.select_set(True)
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
