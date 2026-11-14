"""
사용자가 만든 아바타 Luna(Documents/Untitled_refined.blend)를 유니티용 FBX와 텍스처로 내보낸다.

    blender --background "<문서 폴더>/Untitled_refined.blend" --python Tools/Blender/export_luna.py

블렌드 파일은 읽기만 한다(저장하지 않는다). 숨겨 둔 드레스 두 벌을 보이게 하거나 재질 이름을 바꾸는 것은 이 실행 안에서만 일어난다.
결과는 Assets/MiniPandoras/Art/Models/Luna.fbx와 Art/Textures/Luna/*.png다.
"""

import os
import shutil

import bpy

PROJECT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPORT_PATH = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Models", "Luna.fbx")
TEXTURE_FOLDER = os.path.join(PROJECT, "Assets", "MiniPandoras", "Art", "Textures", "Luna")

RIG = "Armature"
# 몸, 머리카락과 머리 장식, 갈아입는 옷: 원래 의상(조끼 Jacket + 반바지 Shorts)과 드레스 세 벌.
MESHES = ["Body", "Hair", "HairPin", "HairChain", "Jacket", "Shorts", "Dress_Angel", "Dress_Gothic", "Dress_Saint"]
# 파일의 기본 자세는 팔을 내린 A자세다. 유니티 휴머노이드 아바타는 T자세에서 만들어야 해서 이 액션의 자세로 내보낸다.
T_POSE_ACTION = "0_T-Pose.001"

# 유니티 쪽에서 알아보기 쉬운 재질 이름(ModelImportRules가 같은 이름의 툰 재질로 바꿔 끼운다).
MATERIAL_NAMES = {
    "MAT_Tripo_NoHair_Character_Body": "Luna_Body",
    "MAT_Tripo_Hair_Halo": "Luna_Hair",
    # 조끼와 반바지의 재질 이름은 생성 도구가 붙인 해시다.
    "e79be0b4_c946_4c4d_9c1c_ab84e4ca8a68.001": "Luna_Jacket",
    "df4bb28a_f1ac_4771_990e_9081015c43c2.002": "Luna_Shorts",
    "MAT_Dress_Paint": "Luna_Dress",
    "MAT_Ornament_Gold": "Luna_Ornament",
    "MAT_Ornament_GoldAntique": "Luna_OrnamentAntique",
    "MAT_Ornament_Pearl": "Luna_OrnamentPearl",
}


def show(obj):
    obj.hide_viewport = False
    obj.hide_set(False)
    obj.hide_select = False


def pose_as_t(rig):
    action = bpy.data.actions[T_POSE_ACTION]
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = action
    rig.data.pose_position = "POSE"
    bpy.context.scene.frame_set(int(action.frame_range[0]))
    bpy.context.view_layer.update()
    arm = rig.pose.bones["CC_Base_L_Upperarm"]
    direction = (rig.matrix_world @ arm.tail) - (rig.matrix_world @ arm.head)
    # 위팔이 거의 수평이어야 T자세다.
    assert abs(direction.normalized().z) < 0.2, "T자세가 아니다: %s" % direction


def copy_textures(meshes):
    """내보내는 메시가 쓰는 그림을 오브젝트 이름으로 복사한다(Body.png, Jacket.png …). 드레스와 장식은 꼭짓점 색이라 그림이 없다."""
    os.makedirs(TEXTURE_FOLDER, exist_ok=True)
    for obj in meshes:
        for material in obj.data.materials:
            for node in material.node_tree.nodes:
                if node.type != "TEX_IMAGE" or node.image is None:
                    continue
                source = bpy.path.abspath(node.image.filepath)
                assert node.image.packed_file is None and os.path.exists(source), "그림 파일이 없다: %s" % source
                target = os.path.join(TEXTURE_FOLDER, obj.name + os.path.splitext(source)[1])
                shutil.copyfile(source, target)
                print("texture", source, "->", target)


def export():
    rig = bpy.data.objects[RIG]
    meshes = [bpy.data.objects[name] for name in MESHES]
    for obj in [rig] + meshes:
        show(obj)
    pose_as_t(rig)
    copy_textures(meshes)
    for old, new in MATERIAL_NAMES.items():
        bpy.data.materials[old].name = new

    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for obj in [rig] + meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    os.makedirs(os.path.dirname(EXPORT_PATH), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=EXPORT_PATH,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        # 뼈대가 cm 단위(크기 0.01)다. 단위를 파일에 실어 보내 유니티에서 크기 1로 들어오게 한다.
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        # 몸에 손본 사용자 노멀이 있어서 그대로 보낸다.
        mesh_smooth_type="OFF",
        use_mesh_modifiers=False,
        # 드레스와 장식의 꼭짓점 색 「Paint」. sRGB 값으로 보내고 툰 셰이더가 선형으로 되돌린다.
        colors_type="SRGB",
        # 어깨 도우미 뼈(Shoulder_Lift·Swing)는 블렌더 제약용이라 보내지 않는다.
        use_armature_deform_only=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    print("exported", EXPORT_PATH)


export()
