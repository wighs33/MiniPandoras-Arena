using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// 판도라 스킬 몸짓 클립(Art/Animation/SkillPoses)을 짓는다. 모션 라이브러리에 「땅 짚기」, 「팔 펼치기」 같은 시전 동작이 없어서
    /// 열쇠 자세를 휴머노이드 근육 값으로 적고 그 사이를 부드럽게 잇는다. 근육 값은 뼈대와 상관없이 「왼팔 들기」 같은 뜻이라
    /// Luna에도 그대로 맞는다.
    /// 윗몸은 싸움 자세(Sword_Idle)에서 시작해 적은 근육만 바꾼다. 다리·몸 높이·발 자리는 라이브러리 클립의 한 순간에서 함께 빌린다.
    /// 발 IK 목표가 몸을 기준으로 적혀 있어서, 몸 높이와 발 자리를 같은 순간에서 가져와야 발이 땅에 붙는다.
    /// 클립을 손으로 고치지 않고 이 파일을 고쳐 메뉴 「MiniPandoras/스킬 몸짓 만들기」로 다시 짓는다.
    /// </summary>
    public static class SkillPoseBuilder
    {
        private const string OutputFolder = "Assets/MiniPandoras/Art/Animation/SkillPoses/";

        private static readonly string[] Libraries =
        {
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx",
            "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary2/UAL2_Standard.fbx",
        };

        /// <summary>다리·몸 높이·발 자리를 빌려 오는 라이브러리 클립의 한 순간.</summary>
        private readonly struct Stance
        {
            public Stance(string clip, float time)
            {
                Clip = clip;
                Time = time;
            }

            public string Clip { get; }
            public float Time { get; }
        }

        /// <summary>열쇠 자세: 그 시각에 어느 다리로 서서, 윗몸 근육을 얼마로 두는지(적지 않은 근육은 싸움 자세 그대로).</summary>
        private readonly struct Key
        {
            public Key(float time, Stance stance, params (string Muscle, float Value)[] upperBody)
            {
                Time = time;
                Stance = stance;
                UpperBody = upperBody;
            }

            public float Time { get; }
            public Stance Stance { get; }
            public (string Muscle, float Value)[] UpperBody { get; }
        }

        // 3연격과 같은 싸움 자세. 윗몸의 바탕이기도 하다.
        private static readonly Stance Ready = new Stance("Sword_Idle", 0f);
        // 뛰었다 내려앉아 가장 낮게 웅크린 순간.
        private static readonly Stance Crouch = new Stance("Jump_Land", 0.2f);
        // 내려앉았다가 반쯤 일어선 순간(힘을 모으는 자세).
        private static readonly Stance Low = new Stance("Jump_Land", 0.45f);
        // 똑바로 선 자세. 몸이 앞을 본다(싸움 자세는 몸이 옆으로 틀어져 있다).
        private static readonly Stance Upright = new Stance("Idle_Loop", 0f);

        // 근육 값의 방향(Luna에 근육 하나씩 넣어 손·머리가 가는 곳을 잰 것): 가슴 Front-Back은 음수가 앞으로 숙이기,
        // 팔 Down-Up은 양수가 들기, 팔 Front-Back은 0이 앞 아래쪽이고 양수가 바깥·뒤로 벌리기, 음수가 몸 앞으로 모으기,
        // 아래팔 Stretch는 1이 곧게 펴기, 고개 Nod는 양수가 들기.

        private static readonly (string, float)[] HandOnGround =
        {
            ("Left Shoulder Down-Up", -0.3f), ("Left Arm Down-Up", -0.5f), ("Left Arm Front-Back", -0.1f), ("Left Forearm Stretch", 1f),
            ("Chest Front-Back", -0.9f), ("UpperChest Front-Back", -1.4f), ("Head Nod Down-Up", 0.4f),
        };

        /// <summary>땅 짚기(불꽃 고리, 가시 숲): 왼손을 높이 들었다가 웅크리며 땅을 내리친다. 내리치는 0.25초가 기술이 나가는 시각이다.</summary>
        private static readonly Key[] Slam =
        {
            new Key(0f, Ready),
            new Key(0.12f, Ready,
                ("Left Shoulder Down-Up", 0.8f), ("Left Arm Down-Up", 1.4f), ("Left Arm Front-Back", 0f), ("Left Forearm Stretch", 0.6f),
                ("UpperChest Front-Back", -0.2f), ("Head Nod Down-Up", 0.2f)),
            new Key(0.25f, Crouch, HandOnGround),
            new Key(0.6f, Crouch, HandOnGround),
        };

        // 팔 펼치기의 두 자세.
        private static readonly (string, float)[] ArmsGathered =
        {
            ("Left Arm Down-Up", 0.1f), ("Left Arm Front-Back", -0.8f), ("Left Forearm Stretch", 0.2f),
            ("Right Arm Down-Up", 0.1f), ("Right Arm Front-Back", -0.8f), ("Right Forearm Stretch", 0.2f),
            ("Chest Front-Back", -0.9f), ("UpperChest Front-Back", -1.4f), ("Head Nod Down-Up", -0.4f),
        };

        // 팔을 머리 위가 아니라 양옆 위로(V자) 벌린다. 머리 위로 모으면 방망이를 들어 올리는 것처럼 보였다.
        private static readonly (string, float)[] ArmsFlung =
        {
            ("Left Shoulder Down-Up", 0.5f), ("Left Arm Down-Up", 0.75f), ("Left Arm Front-Back", 0.9f), ("Left Forearm Stretch", 1f),
            ("Right Shoulder Down-Up", 0.5f), ("Right Arm Down-Up", 0.75f), ("Right Arm Front-Back", 0.9f), ("Right Forearm Stretch", 1f),
            ("Chest Front-Back", 0.3f), ("UpperChest Front-Back", 0.5f), ("Head Nod Down-Up", 0.5f),
            ("Chest Twist Left-Right", 0f), ("UpperChest Twist Left-Right", 0f), ("Neck Turn Left-Right", 0f), ("Head Turn Left-Right", 0f),
        };

        /// <summary>팔 펼치기(궁극기: 불바다, 검은 날개, 차원이동): 팔을 가슴 앞에 모았다가 위로 활짝 펼친다. 펼치는 0.27초에 기술이 나간다.</summary>
        private static readonly Key[] Burst =
        {
            new Key(0f, Ready),
            new Key(0.14f, Low, ArmsGathered),
            new Key(0.27f, Upright, ArmsFlung),
            new Key(0.6f, Upright, ArmsFlung),
        };

        private static readonly (string, float)[] ArmsCrossed =
        {
            ("Left Arm Down-Up", 0.6f), ("Left Arm Front-Back", -0.9f), ("Left Forearm Stretch", -0.2f),
            ("Right Arm Down-Up", 0.6f), ("Right Arm Front-Back", -0.9f), ("Right Forearm Stretch", -0.2f),
            ("Chest Front-Back", -0.6f), ("UpperChest Front-Back", -0.8f), ("Head Nod Down-Up", -0.4f),
        };

        /// <summary>막기(위상전이): 두 팔을 얼굴 앞에 엇갈려 모은다. 기술은 곧바로 나가고 몸짓은 0.1초에 다 모인다.</summary>
        private static readonly Key[] Guard =
        {
            new Key(0f, Ready),
            new Key(0.1f, Low, ArmsCrossed),
            new Key(0.3f, Low, ArmsCrossed),
        };

        [MenuItem("MiniPandoras/스킬 몸짓 만들기")]
        public static void BuildAll()
        {
            Dictionary<string, AnimationClip> library = LoadLibrary();
            Build(library, "Skill_Slam", Slam);
            Build(library, "Skill_Burst", Burst);
            Build(library, "Skill_Guard", Guard);
            AssetDatabase.SaveAssets();
        }

        private static void Build(Dictionary<string, AnimationClip> library, string name, Key[] keys)
        {
            AnimationClip clip = LoadOrCreate(name);
            clip.ClearCurves();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(library[Ready.Clip]))
            {
                var curve = new AnimationCurve();
                foreach (Key key in keys)
                {
                    curve.AddKey(key.Time, ValueAt(library, binding, key));
                }

                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }

                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            // 한 번만 틀고, 몸의 자리·방향은 인물을 옮기지 않고 자세에 남긴다(움직임은 FighterMotor가 맡는다).
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static float ValueAt(Dictionary<string, AnimationClip> library, EditorCurveBinding binding, Key key)
        {
            foreach ((string muscle, float value) in key.UpperBody)
            {
                if (muscle == binding.propertyName)
                {
                    return value;
                }
            }

            Stance source = IsLowerBody(binding.propertyName) ? key.Stance : Ready;
            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(library[source.Clip], binding);
            return sourceCurve.Evaluate(source.Time);
        }

        /// <summary>몸 자리·방향(Root), 발 IK 목표, 다리 근육.</summary>
        private static bool IsLowerBody(string property)
        {
            return property.StartsWith("Root") || property.Contains("Foot") || property.Contains("Leg") || property.Contains("Toes");
        }

        private static AnimationClip LoadOrCreate(string name)
        {
            string path = OutputFolder + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                return clip;
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/MiniPandoras/Art/Animation", "SkillPoses");
            }

            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Dictionary<string, AnimationClip> LoadLibrary()
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string path in Libraries)
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                    {
                        clips[clip.name] = clip;
                    }
                }
            }

            return clips;
        }
    }
}
