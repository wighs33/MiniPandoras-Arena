using System.Collections.Generic;

namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 3연격의 순서와 시각만 다룬다(그림과 이동은 모른다).
    /// 이어지는 시각과 때리는 시각을 애니메이션 이벤트가 아니라 데이터의 시각으로 정한다. 온라인 대전에서 호스트가 그림 없이도
    /// 같은 시각으로 판정할 수 있게 하려는 것이다.
    /// </summary>
    public sealed class ComboChain
    {
        private readonly IReadOnlyList<ComboStep> steps;
        private bool nextQueued;
        private bool struck;

        public ComboChain(IReadOnlyList<ComboStep> steps)
        {
            this.steps = steps;
            Step = -1;
        }

        /// <summary>한 번의 Tick에서 일어난 일. 한 번에 하나만 알리고, 때리기가 다음 타보다 먼저다.</summary>
        public enum Change
        {
            None,
            /// <summary>지금 타가 때리는 시각에 이르렀다.</summary>
            Struck,
            /// <summary>다음 타가 시작됐다.</summary>
            StepStarted,
            /// <summary>공격이 끝나 다시 움직일 수 있다.</summary>
            Finished,
        }

        public bool IsAttacking => Step >= 0;

        /// <summary>지금 치는 타(0부터). 치지 않으면 -1.</summary>
        public int Step { get; private set; }

        /// <summary>지금 타를 시작한 뒤 흐른 시간(초).</summary>
        public float Elapsed { get; private set; }

        public ComboStep Current => steps[Step];

        /// <summary>마지막 타인지(맞히면 크게 밀어낸다).</summary>
        public bool IsFinisher => Step == steps.Count - 1;

        /// <summary>공격 버튼을 눌렀다. 쉬고 있었으면 첫 타가 바로 시작되고 true를 돌려준다.</summary>
        public bool Press()
        {
            if (!IsAttacking)
            {
                Begin(0);
                return true;
            }

            if (Step < steps.Count - 1)
            {
                nextQueued = true;
            }

            return false;
        }

        /// <summary>맞아서 공격이 끊겼다. 미리 누른 타도 잊는다.</summary>
        public void Cancel()
        {
            Step = -1;
            nextQueued = false;
        }

        public Change Tick(float deltaTime)
        {
            if (!IsAttacking)
            {
                return Change.None;
            }

            Elapsed += deltaTime;
            if (!struck && Elapsed >= Current.HitAt)
            {
                struck = true;
                return Change.Struck;
            }

            if (nextQueued && struck && Elapsed >= Current.ChainAt)
            {
                Begin(Step + 1);
                return Change.StepStarted;
            }

            if (Elapsed >= Current.Duration)
            {
                Step = -1;
                return Change.Finished;
            }

            return Change.None;
        }

        private void Begin(int step)
        {
            Step = step;
            Elapsed = 0f;
            nextQueued = false;
            struck = false;
        }
    }
}
