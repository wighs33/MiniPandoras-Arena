using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// Art/Models의 모델(Tools/Blender의 스크립트가 짓거나 내보낸 것)을 들여오는 규칙.
    /// 인물(Luna)은 휴머노이드 아바타로 만들어 Quaternius 모션을 리타깃하고, 모든 메시는 외곽선용 부드러운 법선을 접선 칸에 굽는다.
    /// 재질은 FBX의 재질 이름과 같은 이름의 툰 재질(Art/Materials)로 바꿔 끼운다(.meta의 재질 연결에 저장된다).
    /// </summary>
    public sealed class ModelImportRules : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/MiniPandoras/Art/Models/";
        private const string CharacterModel = ModelsFolder + "Luna.fbx";
        private const string BonePrefix = "CC_Base_";

        // Luna의 뼈대는 Character Creator(CC_Base) 것이다. 자동 연결에 맡기지 않고 표로 잇는다:
        // 비틀림 뼈(…Twist01)가 팔다리 자리에 잘못 들어가거나 척추 마디가 한 칸씩 밀리는 일을 막는다.
        private static readonly (string Human, string Bone)[] Spine =
        {
            ("Hips", "Hip"), ("Spine", "Waist"), ("Chest", "Spine01"), ("UpperChest", "Spine02"),
            ("Neck", "NeckTwist01"), ("Head", "Head"),
        };

        // 왼쪽·오른쪽에 같은 이름으로 있는 뼈. 유니티는 Left/Right를 앞에, CC는 L_/R_를 가운데에 붙인다.
        private static readonly (string Human, string Bone)[] Limbs =
        {
            ("Shoulder", "Clavicle"), ("UpperArm", "Upperarm"), ("LowerArm", "Forearm"), ("Hand", "Hand"),
            ("UpperLeg", "Thigh"), ("LowerLeg", "Calf"), ("Foot", "Foot"), ("Toes", "ToeBase"),
        };

        private static readonly (string Human, string Bone)[] Fingers =
        {
            ("Thumb", "Thumb"), ("Index", "Index"), ("Middle", "Mid"), ("Ring", "Ring"), ("Little", "Pinky"),
        };

        private static readonly string[] FingerJoints = { "Proximal", "Intermediate", "Distal" };

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            // 툰 셰이더가 노멀 맵을 쓰지 않으므로 접선 칸은 외곽선 방향으로 쓴다(OnPostprocessModel에서 덮어쓴다).
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = false;
            if (assetPath == CharacterModel)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.humanDescription = CharacterAvatar(importer.humanDescription);
                // 표정(눈 깜빡임, 입 모양)은 셰이프 키다. 법선은 다시 계산하지 않고 원래 것을 쓴다(얼굴에 이음매가 생긴다).
                importer.importBlendShapes = true;
                importer.importBlendShapeNormals = ModelImporterNormals.None;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importBlendShapes = false;
            }
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(ModelsFolder))
            {
                return;
            }

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                OutlineNormalBaker.Bake(filter.sharedMesh);
            }

            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                OutlineNormalBaker.Bake(skinned.sharedMesh);
            }
        }

        private static HumanDescription CharacterAvatar(HumanDescription description)
        {
            var bones = new List<HumanBone>();
            foreach ((string human, string bone) in Spine)
            {
                bones.Add(Bone(human, BonePrefix + bone));
            }

            foreach ((string side, string letter) in new[] { ("Left", "L"), ("Right", "R") })
            {
                foreach ((string human, string bone) in Limbs)
                {
                    bones.Add(Bone(side + human, $"{BonePrefix}{letter}_{bone}"));
                }

                foreach ((string human, string bone) in Fingers)
                {
                    for (int joint = 0; joint < FingerJoints.Length; joint++)
                    {
                        bones.Add(Bone($"{side} {human} {FingerJoints[joint]}", $"{BonePrefix}{letter}_{bone}{joint + 1}"));
                    }
                }
            }

            description.human = bones.ToArray();
            return description;
        }

        private static HumanBone Bone(string humanName, string boneName)
        {
            var bone = new HumanBone { humanName = humanName, boneName = boneName };
            bone.limit.useDefaultValues = true;
            return bone;
        }
    }
}
