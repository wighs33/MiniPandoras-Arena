using MiniPandoras.Controls;
using MiniPandoras.Fighters;
using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 호스트에서 네트워크 너머 참가자의 인물을 조작한다. 참가자가 보낸 명령을 이 PC의 인물에 넘긴다(<see cref="FighterCommands"/>).
    /// 이동 방향은 마지막으로 받은 것을 매 프레임 넘긴다(차원이동 표시는 프레임마다 옮겨야 한다).
    /// 싸우는 동안만 켜진다(DuelMatch가 켜고 끈다).
    /// </summary>
    // 이 PC의 사람(PlayerControls)처럼 인물보다 먼저 돈다.
    [DefaultExecutionOrder(-10)]
    public sealed class RemoteControls : MonoBehaviour
    {
        [SerializeField] private Fighter fighter;
        [SerializeField] private SkillCaster skills;

        private FighterCommands commands;
        private Vector3 direction;

        public void ReceiveMove(Vector3 newDirection)
        {
            direction = Vector3.ClampMagnitude(newDirection, 1f);
        }

        public void ReceiveButton(GuestButton button, int slot)
        {
            if (!enabled)
            {
                return;
            }

            switch (button)
            {
                case GuestButton.Attack:
                    commands.Attack();
                    break;
                case GuestButton.Skill:
                    commands.UseSkill(slot);
                    break;
                // 판도라 교체는 완성 뒤에 더한다(계획서 「완성 뒤 추가」).
            }
        }

        private void Awake()
        {
            commands = new FighterCommands(fighter, skills);
        }

        private void Update()
        {
            commands.Move(direction);
        }

        private void OnDisable()
        {
            direction = Vector3.zero;
            commands.Move(Vector3.zero);
        }
    }
}
