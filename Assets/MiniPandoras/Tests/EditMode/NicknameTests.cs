using MiniPandoras.Duel;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>닉네임 규칙: 앞뒤 빈칸을 지우고 10자까지 자르며, 남는 글이 없으면 받지 않는다(온라인 상대가 보낸 이름도 같다).</summary>
    public sealed class NicknameTests
    {
        [Test]
        public void TrimsSpacesAroundTheName()
        {
            Assert.AreEqual("루나", PlayerProfile.CleanNickname("  루나 "));
        }

        [Test]
        public void CutsLongNamesToTheLimit()
        {
            string cleaned = PlayerProfile.CleanNickname("가나다라마바사아자차카타");

            Assert.AreEqual(PlayerProfile.MaxNicknameLength, cleaned.Length);
            Assert.AreEqual("가나다라마바사아자차", cleaned);
        }

        [Test]
        public void DoesNotEndWithASpaceAfterCutting()
        {
            Assert.AreEqual("가나다라마바사아자", PlayerProfile.CleanNickname("가나다라마바사아자 차카"));
        }

        [Test]
        public void RefusesEmptyNames()
        {
            Assert.IsNull(PlayerProfile.CleanNickname("   "));
            Assert.IsNull(PlayerProfile.CleanNickname(""));
            Assert.IsNull(PlayerProfile.CleanNickname(null));
        }
    }
}
