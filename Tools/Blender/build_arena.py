"""
원형 투기장 한 곳을 코드로 짓는다: 모래 바닥, 돌판 고리, 낮은 벽과 두 문, 기둥과 화로, 판도라 색 깃발, 계단식 관중석.

Blender MCP에서 exec(open(path).read(), {"__file__": path})로 돌리거나, 명령줄에서
    blender --background --python build_arena.py
로 돌리면 Assets/MiniPandoras/Art/Models/Arena.fbx를 다시 쓴다.

색 차이는 재질 칸으로 낸다(돌판마다 세 가지 돌 재질 중 하나). 유니티에서 같은 이름의 툰 재질로 바꿔 끼운다.
"""

import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPORT_PATH = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Models", "Arena.fbx")

PREVIEW_COLORS = {
    "Sand": "E8C996",
    "SandDark": "D9B47C",
    "Stone": "B8B0A3",
    "StoneLight": "CCC5B8",
    "StoneDark": "9F978B",
    "Grout": "6E665E",
    "Wall": "A39A8E",
    "Coping": "D6CFC2",
    "Wood": "8E5B3B",
    "Metal": "4B4652",
    "BannerPhase": "7D6CFF",
    "BannerDemon": "B8306F",
    "BannerFlame": "FF6A1E",
    "Cloth": "F2EBDD",
}

FLOOR_RADIUS = 8.6       # 모래 바닥
RING_OUTER = 11.0        # 돌판 고리 바깥 = 벽 안쪽
WALL_HEIGHT = 1.35
WALL_THICKNESS = 0.6
GATE_ANGLES = (90.0, 270.0)   # 문이 있는 방향(도). 0도가 +X, 90도가 +Y(유니티의 앞쪽 반대)
GATE_HALF_WIDTH = 1.3
PILLAR_COUNT = 8
UV_TILE = 5.0            # 모래 텍스처 한 장이 덮는 길이(m)


# ---------------------------------------------------------------- 공통

def hex_to_linear(hex_code):
    channels = [int(hex_code[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in channels] + [1.0]


def material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    if mat.node_tree is None:
        mat.use_nodes = True
    shader = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    shader.inputs["Base Color"].default_value = hex_to_linear(PREVIEW_COLORS[name])
    shader.inputs["Roughness"].default_value = 0.9
    mat.diffuse_color = hex_to_linear(PREVIEW_COLORS[name])
    return mat


class Builder:
    """한 오브젝트에 들어갈 면을 재질 칸과 함께 모은다."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.slots = []

    def slot(self, material_name):
        if material_name not in self.slots:
            self.slots.append(material_name)
        return self.slots.index(material_name)

    def face(self, points, material_name):
        verts = [self.bm.verts.new(p) for p in points]
        f = self.bm.faces.new(verts)
        f.material_index = self.slot(material_name)
        return f

    def prism(self, bottom, top, material_name, top_material=None, cap_bottom=False):
        """밑면과 윗면 다각형(같은 꼭짓점 수)을 이어 기둥을 만든다."""
        count = len(bottom)
        lower = [self.bm.verts.new(p) for p in bottom]
        upper = [self.bm.verts.new(p) for p in top]
        side = self.slot(material_name)
        for i in range(count):
            j = (i + 1) % count
            f = self.bm.faces.new((lower[i], lower[j], upper[j], upper[i]))
            f.material_index = side
        f = self.bm.faces.new(upper)
        f.material_index = self.slot(top_material or material_name)
        if cap_bottom:
            f = self.bm.faces.new(list(reversed(lower)))
            f.material_index = side

    def finish(self, smooth=False):
        # 면 방향은 꼭짓점 순서(위에서 보아 반시계)로 정한다. 열린 면이 많아 자동 계산은 믿을 수 없다.
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0005)
        # 바닥에서 내려다본 평면 투영. 모래 텍스처가 UV_TILE 미터마다 되풀이된다.
        uv_layer = self.bm.loops.layers.uv.new("UVMap")
        for f in self.bm.faces:
            for loop in f.loops:
                loop[uv_layer].uv = (loop.vert.co.x / UV_TILE, loop.vert.co.y / UV_TILE)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for name in self.slots:
            mesh.materials.append(material(name))
        if smooth:
            mesh.shade_smooth()
        else:
            mesh.shade_flat()
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        return obj


def polar(radius, degrees, z=0.0):
    a = math.radians(degrees)
    return Vector((math.cos(a) * radius, math.sin(a) * radius, z))


def in_gate(degrees, radius_extra=0.0, radius=RING_OUTER):
    """그 각도가 문 자리에 걸치는지. 문 폭은 벽 반지름에서 잰 호 길이다."""
    for gate in GATE_ANGLES:
        diff = (degrees - gate + 180.0) % 360.0 - 180.0
        if abs(math.radians(diff)) * radius < GATE_HALF_WIDTH + radius_extra:
            return True
    return False


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            block.remove(item)


# ---------------------------------------------------------------- 바닥

def build_floor(rng):
    """모래 원판(얼룩 두 가지)과 가운데 돌 고리, 바깥 돌판 고리. 돌판 사이 줄눈은 한 단 낮은 바닥이 보이게 한다."""
    b = Builder("Floor")
    # 모래: 고리마다 같은 수로 나눈 원판(T자 이음이 없어 틈이 보이지 않는다). 얼룩은 텍스처가 그린다.
    segments = 64
    rings = [0.0, 1.6, 3.2, 4.8, 6.4, 7.6, FLOOR_RADIUS + 0.1]
    for r0, r1 in zip(rings, rings[1:]):
        for s in range(segments):
            a0, a1 = 360.0 * s / segments, 360.0 * (s + 1) / segments
            if r0 == 0.0:
                b.face([Vector((0, 0, 0)), polar(r1, a0), polar(r1, a1)], "Sand")
            else:
                b.face([polar(r0, a0), polar(r1, a0), polar(r1, a1), polar(r0, a1)], "Sand")
    # 줄눈 바닥: 돌판 아래 깔린 어두운 고리.
    for s in range(segments):
        a0, a1 = 360.0 * s / segments, 360.0 * (s + 1) / segments
        b.face([polar(FLOOR_RADIUS + 0.1, a0, -0.02), polar(RING_OUTER + 0.4, a0, -0.02),
                polar(RING_OUTER + 0.4, a1, -0.02), polar(FLOOR_RADIUS + 0.1, a1, -0.02)], "Grout")
    floor = b.finish()

    slabs = Builder("FloorSlabs")
    gap = 0.045
    for r0, r1, count in ((FLOOR_RADIUS, 9.8, 44), (9.8, RING_OUTER + 0.3, 50)):
        offset = rng.uniform(0, 360.0 / count)
        for s in range(count):
            a0 = offset + 360.0 * s / count
            a1 = offset + 360.0 * (s + 1) / count
            # 줄눈 폭만큼 안으로 줄인다(각도는 반지름에 따라 다르다).
            g0, g1 = math.degrees(gap / r0), math.degrees(gap / r1)
            h = 0.035 + rng.uniform(-0.012, 0.012)
            ri, ro = r0 + gap, r1 - gap
            bottom = [polar(ri, a0 + g0, -0.02), polar(ro, a0 + g1, -0.02), polar(ro, a1 - g1, -0.02),
                      polar(ri, a1 - g0, -0.02)]
            top = [polar(ri + 0.02, a0 + g0 + 0.2, h), polar(ro - 0.02, a0 + g1 + 0.15, h),
                   polar(ro - 0.02, a1 - g1 - 0.15, h), polar(ri + 0.02, a1 - g0 - 0.2, h)]
            name = rng.choice(("Stone", "Stone", "StoneLight", "StoneDark"))
            slabs.prism(bottom, top, name)
    # 가운데 문양: 밝은 돌 원판에 판도라 셋(차원·악마·불꽃)의 색 마름모를 바깥으로 향하게 박는다.
    for s in range(24):
        a0, a1 = 15.0 * s, 15.0 * (s + 1)
        slabs.face([Vector((0, 0, 0.012)), polar(1.05, a0, 0.012), polar(1.05, a1, 0.012)], "StoneLight")
    colors = ("BannerPhase", "BannerDemon", "BannerFlame")
    for k, color in enumerate(colors):
        angle = 90.0 + 120.0 * k
        rot = Matrix.Rotation(math.radians(angle), 3, "Z")
        kite = [(0.22, 0.0), (0.58, -0.17), (0.94, 0.0), (0.58, 0.17)]
        slabs.face([rot @ Vector((x, y, 0.018)) for x, y in kite], color)
    for s in range(12):
        a0, a1 = 30.0 * s, 30.0 * (s + 1)
        slabs.face([Vector((0, 0, 0.018)), polar(0.16, a0, 0.018), polar(0.16, a1, 0.018)], "Coping")
    count = 24
    for s in range(count):
        a0, a1 = 360.0 * s / count, 360.0 * (s + 1) / count
        g = math.degrees(0.025 / 1.05)
        bottom = [polar(1.05, a0 + g, -0.02), polar(1.4, a0 + g, -0.02), polar(1.4, a1 - g, -0.02), polar(1.05, a1 - g, -0.02)]
        top = [polar(1.06, a0 + g + 0.4, 0.03), polar(1.39, a0 + g + 0.4, 0.03), polar(1.39, a1 - g - 0.4, 0.03),
               polar(1.06, a1 - g - 0.4, 0.03)]
        slabs.prism(bottom, top, "Coping")
    # 가운데 돌 고리: 대전의 시작 자리를 알린다.
    count = 28
    for s in range(count):
        a0, a1 = 360.0 * s / count, 360.0 * (s + 1) / count
        g = math.degrees(0.03 / 2.6)
        bottom = [polar(2.6, a0 + g, -0.02), polar(3.0, a0 + g, -0.02), polar(3.0, a1 - g, -0.02), polar(2.6, a1 - g, -0.02)]
        top = [polar(2.62, a0 + g + 0.4, 0.025), polar(2.98, a0 + g + 0.4, 0.025), polar(2.98, a1 - g - 0.4, 0.025),
               polar(2.62, a1 - g - 0.4, 0.025)]
        slabs.prism(bottom, top, "StoneLight" if s % 2 == 0 else "Stone")
    return floor, slabs.finish()


# ---------------------------------------------------------------- 벽

def build_wall(rng):
    """돌판 고리를 두르는 낮은 벽. 블록마다 높이가 조금씩 다르고 위에 밝은 갓돌을 얹는다. 문 자리는 비운다."""
    b = Builder("Wall")
    count = 64
    r0, r1 = RING_OUTER, RING_OUTER + WALL_THICKNESS
    for s in range(count):
        a0, a1 = 360.0 * s / count, 360.0 * (s + 1) / count
        middle = (a0 + a1) / 2
        if in_gate(middle):
            continue
        g = math.degrees(0.02 / r0)
        h = WALL_HEIGHT + rng.uniform(-0.04, 0.04)
        bottom = [polar(r0, a0 + g, -0.05), polar(r1, a0 + g, -0.05), polar(r1, a1 - g, -0.05), polar(r0, a1 - g, -0.05)]
        top = [polar(r0, a0 + g, h), polar(r1, a0 + g, h), polar(r1, a1 - g, h), polar(r0, a1 - g, h)]
        b.prism(bottom, top, rng.choice(("Wall", "Wall", "StoneDark")))
        # 갓돌: 벽보다 조금 넓고 안쪽으로 튀어나온다.
        cb = [polar(r0 - 0.08, a0, h), polar(r1 + 0.06, a0, h), polar(r1 + 0.06, a1, h), polar(r0 - 0.08, a1, h)]
        ct = [polar(r0 - 0.06, a0, h + 0.14), polar(r1 + 0.04, a0, h + 0.14), polar(r1 + 0.04, a1, h + 0.14),
              polar(r0 - 0.06, a1, h + 0.14)]
        b.prism(cb, ct, "Coping")
    return b.finish()


def build_gates():
    """문 두 곳: 돌 아치 기둥 둘과 들보, 닫힌 나무 문."""
    b = Builder("Gates")
    for gate in GATE_ANGLES:
        rot = Matrix.Rotation(math.radians(gate), 3, "Z")

        def box(center, size, name):
            cx, cy, cz = center
            sx, sy, sz = size
            corners = [Vector((cx + dx * sx / 2, cy + dy * sy / 2, cz)) for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            bottom = [rot @ c for c in corners]
            top = [rot @ (c + Vector((0, 0, sz))) for c in corners]
            b.prism(bottom, top, name, cap_bottom=True)

        depth = RING_OUTER + WALL_THICKNESS / 2
        for side in (-1, 1):
            box((depth, side * (GATE_HALF_WIDTH + 0.35), -0.05), (WALL_THICKNESS + 0.5, 0.7, 2.9), "Coping")
        box((depth, 0, 2.75), (WALL_THICKNESS + 0.6, GATE_HALF_WIDTH * 2 + 1.4, 0.55), "Coping")
        box((depth, 0, 3.3), (WALL_THICKNESS + 0.3, GATE_HALF_WIDTH * 2 + 0.6, 0.25), "Wall")
        # 나무 문: 널빤지 다섯 장.
        planks = 5
        width = GATE_HALF_WIDTH * 2 / planks
        for p in range(planks):
            y = -GATE_HALF_WIDTH + width * (p + 0.5)
            box((depth + 0.12, y, -0.05), (0.14, width - 0.03, 2.7 - abs(p - 2) * 0.04), "Wood")
        for z in (0.5, 1.9):
            box((depth - 0.0, 0, z), (0.08, GATE_HALF_WIDTH * 2, 0.16), "Metal")
    return b.finish()


def build_pillars():
    """벽 위로 솟은 팔각 기둥과 꼭대기 화로(불은 유니티 파티클)."""
    b = Builder("Pillars")
    radius = RING_OUTER + WALL_THICKNESS / 2
    for i in range(PILLAR_COUNT):
        angle = 360.0 * (i + 0.5) / PILLAR_COUNT
        center = polar(radius, angle)

        def octagon(r, z):
            return [center + Vector((math.cos(math.radians(22.5 + 45 * k)) * r, math.sin(math.radians(22.5 + 45 * k)) * r, z))
                    for k in range(8)]

        b.prism(octagon(0.62, -0.05), octagon(0.62, 0.35), "Coping")
        b.prism(octagon(0.48, 0.35), octagon(0.44, 2.75), "Wall")
        b.prism(octagon(0.60, 2.75), octagon(0.60, 3.0), "Coping")
        # 화로 그릇
        b.prism(octagon(0.16, 3.0), octagon(0.42, 3.38), "Metal", cap_bottom=True)
        b.prism(octagon(0.34, 3.38), octagon(0.34, 3.30), "Grout")
    return b.finish()


def build_banners():
    """기둥 사이 벽에 건 깃발. 판도라 셋(차원·악마·불꽃)의 색을 돌아가며 쓴다."""
    b = Builder("Banners")
    colors = ("BannerPhase", "BannerDemon", "BannerFlame")
    radius = RING_OUTER - 0.1
    k = 0
    for i in range(PILLAR_COUNT):
        angle = 360.0 * i / PILLAR_COUNT
        if in_gate(angle, radius_extra=0.6):
            continue
        color = colors[k % 3]
        k += 1
        rot = Matrix.Rotation(math.radians(angle), 3, "Z")
        width, top, bottom = 1.1, WALL_HEIGHT + 0.22, 0.2
        # 깃발 천: 아래가 뾰족한 오각형. 벽에서 조금 떨어뜨린다.
        outline = [(-width / 2, top), (width / 2, top), (width / 2, bottom + 0.25), (0, bottom), (-width / 2, bottom + 0.25)]
        # 꼭짓점 순서는 투기장 가운데에서 보아 반시계다(천이 안쪽을 본다).
        points = [rot @ Vector((radius, x, z)) for x, z in outline]
        b.face(points, color)
        # 가운데 무늬: 밝은 마름모
        diamond = [(0, top - 0.3), (0.22, top - 0.58), (0, top - 0.86), (-0.22, top - 0.58)]
        points = [rot @ Vector((radius - 0.012, x, z)) for x, z in diamond]
        b.face(points, "Cloth")
        # 깃대
        rod = [rot @ Vector((radius - 0.03, x, z)) for x, z in ((-0.65, top + 0.02), (0.65, top + 0.02), (0.65, top + 0.08),
                                                                 (-0.65, top + 0.08))]
        b.face(list(reversed(rod)), "Wood")
    return b.finish()


def build_stands(rng):
    """벽 바깥의 계단식 관중석 세 단과 바깥 담. 문 자리는 통로로 비운다."""
    b = Builder("Stands")
    steps = [(RING_OUTER + WALL_THICKNESS, 1.0), (13.4, 1.6), (14.6, 2.2), (15.8, 2.8)]
    count = 72
    for s in range(count):
        a0, a1 = 360.0 * s / count, 360.0 * (s + 1) / count
        if in_gate((a0 + a1) / 2, radius_extra=0.2, radius=13.0):
            continue
        for (r0, h), (r1, _) in zip(steps, steps[1:] + [(17.0, 0)]):
            if r0 >= 17.0:
                continue
            name = "Stone" if (s // 3 + int(h * 10)) % 2 == 0 else "StoneDark"
            bottom = [polar(r0, a0, -0.05), polar(r1, a0, -0.05), polar(r1, a1, -0.05), polar(r0, a1, -0.05)]
            top = [polar(r0, a0, h), polar(r1, a0, h), polar(r1, a1, h), polar(r0, a1, h)]
            b.prism(bottom, top, name, top_material="StoneLight" if h > 2.5 else None)
        # 바깥 담
        bottom = [polar(17.0, a0, -0.05), polar(17.6, a0, -0.05), polar(17.6, a1, -0.05), polar(17.0, a1, -0.05)]
        top = [polar(17.0, a0, 4.2), polar(17.6, a0, 4.2), polar(17.6, a1, 4.2), polar(17.0, a1, 4.2)]
        b.prism(bottom, top, "Wall", top_material="Coping")
    # 관중석 바깥 바닥(문 통로 포함)
    for s in range(count):
        a0, a1 = 360.0 * s / count, 360.0 * (s + 1) / count
        b.face([polar(RING_OUTER + 0.4, a0, -0.03), polar(17.6, a0, -0.03), polar(17.6, a1, -0.03),
                polar(RING_OUTER + 0.4, a1, -0.03)], "SandDark")
    return b.finish()


def build_props(rng):
    """돌판 고리 위의 소품: 무기 걸이, 나무 상자, 물통. 싸움을 막지 않게 벽에 붙인다."""
    b = Builder("Props")

    def crate(center, size, angle):
        rot = Matrix.Rotation(math.radians(angle), 3, "Z")
        cx, cy, cz = center
        corners = [Vector((dx * size / 2, dy * size / 2, 0)) for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        bottom = [Vector(center) + rot @ c for c in corners]
        top = [Vector(center) + rot @ (c + Vector((0, 0, size))) for c in corners]
        b.prism(bottom, top, "Wood", cap_bottom=True)
        # 테두리 띠
        for z in (0.08, size - 0.12):
            ring_b = [Vector(center) + rot @ (c * 1.04 + Vector((0, 0, z))) for c in corners]
            ring_t = [Vector(center) + rot @ (c * 1.04 + Vector((0, 0, z + 0.06))) for c in corners]
            b.prism(ring_b, ring_t, "Metal")

    def barrel(center):
        cx, cy, cz = center
        def ring(r, z):
            return [Vector((cx + math.cos(2 * math.pi * k / 10) * r, cy + math.sin(2 * math.pi * k / 10) * r, z)) for k in range(10)]
        b.prism(ring(0.30, 0.0), ring(0.36, 0.35), "Wood", cap_bottom=True)
        b.prism(ring(0.36, 0.35), ring(0.30, 0.75), "Wood", top_material="Grout")
        for z in (0.12, 0.6):
            b.prism(ring(0.345 if z < 0.3 else 0.33, z), ring(0.355 if z < 0.3 else 0.34, z + 0.05), "Metal")

    def rack(angle):
        rot = Matrix.Rotation(math.radians(angle), 3, "Z")
        base = Vector((RING_OUTER - 0.45, 0, 0))
        for y in (-0.7, 0.7):
            post = [rot @ (base + Vector((dx, y + dy, 0))) for dx, dy in ((-0.06, -0.06), (0.06, -0.06), (0.06, 0.06), (-0.06, 0.06))]
            b.prism(post, [p + Vector((0, 0, 1.1)) for p in post], "Wood", cap_bottom=True)
        for z in (0.45, 1.0):
            beam = [rot @ (base + Vector((dx, dy, z))) for dx, dy in ((-0.05, -0.8), (0.05, -0.8), (0.05, 0.8), (-0.05, 0.8))]
            b.prism(beam, [p + Vector((0, 0, 0.08)) for p in beam], "Wood", cap_bottom=True)
        # 기대어 둔 목검·방망이
        for k, y in enumerate((-0.45, -0.15, 0.15, 0.45)):
            tilt = Vector((-0.18, 0, 0))
            stick = [rot @ (base + Vector((dx + 0.12, y + dy, 0))) for dx, dy in ((-0.03, -0.03), (0.03, -0.03), (0.03, 0.03), (-0.03, 0.03))]
            b.prism(stick, [p + rot @ (tilt + Vector((0, 0, 1.15 - k % 2 * 0.15))) for p in stick],
                    "Wood" if k % 2 else "Coping", cap_bottom=True)

    rack(35)
    rack(215)
    for angle, offset in ((145, 0.0), (152, 0.7), (325, 0.0)):
        p = polar(RING_OUTER - 0.55 - offset * 0.3, angle + offset * 3)
        crate((p.x, p.y, 0.02), 0.62 - offset * 0.15, angle + rng.uniform(-12, 12))
    for angle in (160, 333, 338):
        p = polar(RING_OUTER - 0.5, angle)
        barrel((p.x, p.y, 0.02))
    return b.finish()


def build():
    clear_scene()
    rng = random.Random(11)
    objects = list(build_floor(rng))
    objects += [build_wall(rng), build_gates(), build_pillars(), build_banners(), build_stands(rng), build_props(rng)]
    return objects


def export(objects):
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    for obj in objects:
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


if __name__ == "__main__" or "MINIPANDORAS_BUILD" in globals():
    built = build()
    if globals().get("MINIPANDORAS_EXPORT", True):
        export(built)
