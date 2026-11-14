using MiniPandoras.Fighters;
using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>
    /// 명령을 이 PC의 인물과 스킬에 넘긴다. 차원이동으로 숨은 동안은 이동이 「나갈 곳」 표시를 옮기고,
    /// 공격·스킬 버튼은 나오는 데 쓴다. 이 PC의 사람(<see cref="PlayerControls"/>)과 호스트에 온 참가자의 명령이 함께 쓴다.
    /// </summary>
    public sealed class FighterCommands : IFighterCommands
    {
        private readonly Fighter fighter;
        private readonly SkillCaster skills;

        public FighterCommands(Fighter fighter, SkillCaster skills)
        {
            this.fighter = fighter;
            this.skills = skills;
        }

        public void Move(Vector3 direction)
        {
            SkillGate gate = skills.ActiveGate;
            if (gate != null)
            {
                gate.Steer(direction, Time.deltaTime);
                return;
            }

            fighter.Move(direction);
        }

        public void Attack()
        {
            if (!LeaveGate())
            {
                fighter.Attack();
            }
        }

        public void UseSkill(int slot)
        {
            if (!LeaveGate())
            {
                skills.Use(slot);
            }
        }

        /// <summary>숨어 있으면 표시가 있는 곳으로 나온다. 나왔으면 true(그 버튼은 나오는 데 쓴다).</summary>
        private bool LeaveGate()
        {
            SkillGate gate = skills.ActiveGate;
            if (gate == null)
            {
                return false;
            }

            gate.Leave();
            return true;
        }
    }
}
