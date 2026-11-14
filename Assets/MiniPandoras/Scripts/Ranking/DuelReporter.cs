using System.Collections.Generic;
using MiniPandoras.Duel;
using MiniPandoras.Net;
using MiniPandoras.Skills;
using Unity.Services.CloudCode;
using UnityEngine;

namespace MiniPandoras.Ranking
{
    /// <summary>
    /// 온라인 판의 승패를 순위표에 보고한다(Cloud Code 「ReportDuel」, <c>Cloud/ReportDuel.js</c>).
    /// 두 사람이 각자 보고하고, 두 보고가 서로 맞을 때만 서버가 두 사람의 Elo 점수를 바꾼다.
    /// 클라이언트는 순위표에 직접 쓰지 못한다(<c>Cloud/NoClientScores.ac</c>). 순위에 드는 판은 Relay 방에서 사람과 싸운 판뿐이다.
    /// </summary>
    public sealed class DuelReporter : MonoBehaviour
    {
        private const string Endpoint = "ReportDuel";

        [SerializeField] private DuelMatch match;
        [SerializeField] private DuelNetwork network;
        [SerializeField] private OnlineRoom room;
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private SkillCaster playerSkills;

        private void OnEnable()
        {
            match.RoundDecided += Report;
        }

        private void OnDisable()
        {
            match.RoundDecided -= Report;
        }

        private async void Report(bool won)
        {
            string matchId = network.MatchId;
            if (matchId == null || !room.TryGetOpponent(out string opponentId))
            {
                return;
            }

            var args = new Dictionary<string, object>
            {
                { "matchId", matchId },
                { "opponentId", opponentId },
                { "won", won },
                { "nickname", profile.Nickname },
                // 판도라는 에셋 이름(Flame, Demon, Dimension)으로 적는다. 화면에 보이는 이름은 바뀔 수 있다.
                { "pandora", playerSkills.Pandora.name },
            };

            try
            {
                DuelVerdict verdict = await CloudCodeService.Instance.CallEndpointAsync<DuelVerdict>(Endpoint, args);
                Debug.Log($"[순위] 판 {matchId}: {verdict.Status}, 점수 {verdict.Rating} ({verdict.Change:+0;-0;0})");
            }
            catch (CloudCodeException e)
            {
                Debug.LogWarning($"[순위] 결과를 보고하지 못했다: {e.Message}");
            }
        }
    }
}
