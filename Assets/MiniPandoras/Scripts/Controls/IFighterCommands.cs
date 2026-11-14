using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>
    /// 조작하는 쪽이 인물에게 내리는 명령. 이 PC의 인물에 바로 넘기거나(<see cref="FighterCommands"/>),
    /// 온라인 참가자라면 호스트로 보낸다(호스트가 판정한다).
    /// </summary>
    public interface IFighterCommands
    {
        /// <summary>가고 싶은 바닥 방향(월드, 길이 0~1). 매 프레임 부른다.</summary>
        void Move(Vector3 direction);

        void Attack();

        void UseSkill(int slot);
    }
}
