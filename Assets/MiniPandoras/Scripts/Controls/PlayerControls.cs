using MiniPandoras.Cameras;
using MiniPandoras.Fighters;
using MiniPandoras.Input;
using MiniPandoras.Skills;
using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>
    /// 사람이 조작하는 인물. 조이스틱을 화면 기준의 바닥 방향으로 돌리고, 공격·스킬 버튼과 함께 명령(<see cref="IFighterCommands"/>)으로 넘긴다.
    /// 혼자 할 때와 호스트는 이 PC의 인물에 바로 넘기고, 온라인 참가자는 호스트로 보낸다(<see cref="SendCommandsTo"/>).
    /// AI(<see cref="RivalAi"/>)는 같은 Fighter·SkillCaster API를 직접 부른다.
    /// </summary>
    // 인물이 같은 프레임의 입력으로 움직이도록 Fighter보다 먼저 돈다.
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerControls : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Fighter fighter;
        [SerializeField] private SkillCaster skills;
        [SerializeField] private QuarterViewCamera view;

        private IFighterCommands localCommands;
        private IFighterCommands commands;

        /// <summary>명령을 보낼 곳을 바꾼다. null이면 다시 이 PC의 인물에 넘긴다.</summary>
        public void SendCommandsTo(IFighterCommands target)
        {
            commands = target ?? localCommands;
        }

        private void Awake()
        {
            localCommands = new FighterCommands(fighter, skills);
            commands = localCommands;
        }

        private void OnEnable()
        {
            input.AttackPressed += OnAttackPressed;
            input.SkillPressed += OnSkillPressed;
        }

        // 판이 끝나 조작을 끄면 마지막 조이스틱 방향으로 계속 걷지 않게 멈춰 둔다.
        private void OnDisable()
        {
            input.AttackPressed -= OnAttackPressed;
            input.SkillPressed -= OnSkillPressed;
            commands.Move(Vector3.zero);
        }

        private void Update()
        {
            Vector2 stick = input.Move;
            commands.Move(view.GroundRotation * new Vector3(stick.x, 0f, stick.y));
        }

        private void OnAttackPressed()
        {
            commands.Attack();
        }

        private void OnSkillPressed(int slot)
        {
            commands.UseSkill(slot);
        }
    }
}
