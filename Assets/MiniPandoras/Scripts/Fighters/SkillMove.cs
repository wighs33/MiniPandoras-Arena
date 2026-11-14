using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>스킬을 쓰는 몸짓. Art/Animation/Fighter.controller의 Skill 상태들과 맞춘다.</summary>
    public enum SkillPose
    {
        /// <summary>앞으로 던지거나 쏜다.</summary>
        Shoot,
        /// <summary>땅을 내리쳐 둘레에 퍼뜨린다.</summary>
        Slam,
        /// <summary>뛰어올라 내리꽂는다.</summary>
        Leap,
        /// <summary>낮게 내달리며 벤다.</summary>
        Dash,
        /// <summary>한 번 크게 벤다.</summary>
        Slash,
        /// <summary>팔을 모았다가 활짝 펼친다(궁극기).</summary>
        Burst,
        /// <summary>두 팔을 얼굴 앞에 엇갈려 모은다.</summary>
        Guard,
    }

    /// <summary>
    /// 스킬을 쓰는 동안 몸이 하는 일: 그 시간 동안 다른 것을 하지 않고, 그 몸짓을 하고, (있으면) 그만큼 미끄러지고, 그동안 맞지 않는다.
    /// 무엇이 나가고 무엇을 맞히는지는 스킬 쪽(MiniPandoras.Skills)이 정한다.
    /// </summary>
    public readonly struct SkillMove
    {
        public SkillMove(SkillPose pose, float duration, Vector3 facing, Vector3 travel = default, float travelTime = 0f, float invulnerable = 0f,
            bool passThrough = false)
        {
            Pose = pose;
            Duration = duration;
            Facing = facing;
            Travel = travel;
            TravelTime = travelTime;
            Invulnerable = invulnerable;
            PassThrough = passThrough;
        }

        public SkillPose Pose { get; }

        /// <summary>다시 움직일 수 있게 되기까지(초).</summary>
        public float Duration { get; }

        /// <summary>쓰기 전에 돌아설 바닥 방향. 0이면 돌지 않는다.</summary>
        public Vector3 Facing { get; }

        /// <summary>미끄러지는 바닥 방향과 거리(m).</summary>
        public Vector3 Travel { get; }

        public float TravelTime { get; }

        /// <summary>쓰기 시작한 뒤 맞지 않는 시간(초).</summary>
        public float Invulnerable { get; }

        /// <summary>미끄러지는 동안 상대 몸을 뚫고 지나가는지(공간도약).</summary>
        public bool PassThrough { get; }
    }
}
