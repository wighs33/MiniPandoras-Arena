using System;
using System.IO;
using MiniPandoras.Combat;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 피해 공식이 2D판(웹게임)과 같은 값을 내는지 본다. 기대값은 2D판의 Tools/WebGame/export_battle.mjs가
    /// 웹게임 코드로 직접 계산해 둔 것을 그대로 옮겼다(2D판 Tests/EditMode/Fixtures/battle_formulas.json의 damage).
    /// </summary>
    public sealed class DamageFormulaTests
    {
        private const string FixturePath = "Assets/MiniPandoras/Tests/EditMode/Fixtures/damage_cases.json";

        [Test]
        public void DamageMatchesThe2DGameForTheSameRoll()
        {
            Fixture expected = JsonUtility.FromJson<Fixture>(File.ReadAllText(FixturePath));

            Assert.That(expected.damage, Is.Not.Empty);
            foreach (DamageCase sample in expected.damage)
            {
                int damage = DamageFormula.Compute(sample.atk, sample.def, sample.power, sample.roll);
                Assert.That(damage, Is.EqualTo(sample.result), $"공격 {sample.atk}, 방어 {sample.def}, 위력 {sample.power}, 난수 {sample.roll}");
            }
        }

        [Test]
        public void CriticalRoundsHalvesUpLikeJavaScript()
        {
            Assert.That(DamageFormula.Critical(3), Is.EqualTo(5), "4.5는 5로 올린다(C#의 기본 반올림은 짝수 쪽인 4)");
            Assert.That(DamageFormula.Critical(4), Is.EqualTo(6));
        }

        [Serializable]
        private sealed class Fixture
        {
            public DamageCase[] damage;
        }

        [Serializable]
        private sealed class DamageCase
        {
            public int atk;
            public int def;
            public float power;
            public double roll;
            public int result;
        }
    }
}
