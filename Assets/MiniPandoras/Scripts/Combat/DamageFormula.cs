using System;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 2D판(웹게임 battle/data.ts의 computeDamage)과 같은 피해 공식. 장면 없이 시험할 수 있게 MonoBehaviour 밖에 둔다.
    /// 피해 = (공격 × 위력 − 방어 × 0.55) × (0.9 ~ 1.1), 반올림하고 적어도 1. 치명타는 1.5배다.
    /// </summary>
    public static class DamageFormula
    {
        private const double CriticalMultiplier = 1.5;

        /// <param name="roll">0 이상 1 미만의 난수. 같은 값이면 2D판과 같은 피해가 나온다.</param>
        public static int Compute(int attack, int defense, float power, double roll)
        {
            // 2D판과 같은 값이 나오도록 JavaScript처럼 double로 계산한다.
            double baseDamage = attack * (double)power - defense * 0.55;
            double variance = 0.9 + roll * 0.2;
            return Math.Max(1, RoundLikeJavaScript(baseDamage * variance));
        }

        public static int Critical(int damage)
        {
            return RoundLikeJavaScript(damage * CriticalMultiplier);
        }

        // JavaScript의 Math.round는 .5를 늘 위로 올린다(C#의 Math.Round는 짝수 쪽으로 보낸다).
        private static int RoundLikeJavaScript(double value)
        {
            return (int)Math.Floor(value + 0.5);
        }
    }
}
