using MiniPandoras.Controls;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 참가자의 명령: 이 PC의 내 인물에 바로 넘기고(예측: 기다리지 않고 움직인다), 호스트로도 보낸다(판정은 호스트가 한다).
    /// 버튼은 누를 때 바로 보내고(놓치면 안 되니 반드시 닿게), 이동 방향은 담아 두었다가 네트워크 틱마다 번호를 붙여 보낸다
    /// (<see cref="DuelNetwork"/>).
    /// </summary>
    public sealed class GuestCommands : IFighterCommands
    {
        private readonly DuelNetwork network;
        private readonly IFighterCommands local;

        public GuestCommands(DuelNetwork network, IFighterCommands local)
        {
            this.network = network;
            this.local = local;
        }

        /// <summary>마지막으로 받은 이동 방향. 다음 틱에 보낸다.</summary>
        public Vector3 Direction { get; private set; }

        public void Move(Vector3 direction)
        {
            Direction = direction;
            local.Move(direction);
        }

        public void Attack()
        {
            local.Attack();
            network.SendButton(GuestButton.Attack, 0);
        }

        public void UseSkill(int slot)
        {
            local.UseSkill(slot);
            network.SendButton(GuestButton.Skill, slot);
        }
    }
}
