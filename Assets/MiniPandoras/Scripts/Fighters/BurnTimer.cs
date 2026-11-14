namespace MiniPandoras.Fighters
{
    /// <summary>
    /// 화상(2D판의 dot): 정해진 간격마다 같은 피해를 정해진 횟수만큼 준다. 시각만 센다(그림과 인물은 모른다).
    /// 다시 붙으면 처음부터 다시 센다.
    /// </summary>
    public sealed class BurnTimer
    {
        private int damagePerTick;
        private int ticksLeft;
        private float interval;
        private float wait;

        public bool IsBurning => ticksLeft > 0;

        public void Ignite(int damage, int ticks, float tickInterval)
        {
            damagePerTick = damage;
            ticksLeft = ticks;
            interval = tickInterval;
            wait = tickInterval;
        }

        public void Clear()
        {
            ticksLeft = 0;
        }

        /// <summary>시간을 흘리고, 이번에 줄 화상 피해를 돌려준다(줄 것이 없으면 0).</summary>
        public int Tick(float deltaTime)
        {
            if (ticksLeft == 0)
            {
                return 0;
            }

            wait -= deltaTime;
            if (wait > 0f)
            {
                return 0;
            }

            wait += interval;
            ticksLeft--;
            return damagePerTick;
        }
    }
}
