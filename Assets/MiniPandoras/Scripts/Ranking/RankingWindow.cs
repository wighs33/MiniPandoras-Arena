using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniPandoras.Duel;
using MiniPandoras.Net;
using TMPro;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace MiniPandoras.Ranking
{
    /// <summary>
    /// 순위표 창: Elo 순위를 위에서부터 5000위까지 100줄씩 불러 보이고, 맨 아래에 내 순위를 고정해 보인다.
    /// 순위표는 누구나 읽을 수 있고 쓰는 것은 서버(<see cref="DuelReporter"/>가 부르는 Cloud Code)뿐이다.
    /// 여는 동안 판을 멈춘다(<see cref="WindowPause"/>). 여는 버튼이 방 메뉴의 고를 것들 안에 있다.
    /// </summary>
    public sealed class RankingWindow : MonoBehaviour
    {
        private const string LeaderboardId = "duel-elo";
        private const int PageSize = 100;
        private const int MaxRank = 5000;

        [SerializeField] private WindowPause pause;
        [Tooltip("순위표에 적힌 판도라(에셋 이름)를 화면 이름과 색으로 바꿀 때 쓴다")]
        [SerializeField] private PlayerProfile profile;
        [Tooltip("어둠 판과 창. 열 때 켜고 닫을 때 끈다")]
        [SerializeField] private GameObject window;
        [SerializeField] private RankingList list;
        [Tooltip("맨 아래에 고정한 내 순위 줄")]
        [SerializeField] private RankingRow myRow;
        [Tooltip("아직 순위에 들지 않았을 때 내 순위 줄 자리에 보이는 안내")]
        [SerializeField] private TMP_Text myRowHint;
        [Tooltip("목록 가운데의 안내(불러오는 중, 비었음, 실패)")]
        [SerializeField] private TMP_Text status;

        private readonly List<RankingEntry> entries = new List<RankingEntry>();
        private int total;
        private bool loading;
        // 창을 열 때마다 하나씩 늘린다. 닫았다 다시 연 뒤에 늦게 도착한 지난번 답은 버린다.
        private int openCount;

        private int Limit => Mathf.Min(total, MaxRank);

        public async void Open()
        {
            int opened = ++openCount;
            entries.Clear();
            total = 0;
            list.Show(entries);
            list.ScrollToTop();
            ShowMine(null);
            ShowStatus("불러오는 중…");
            window.SetActive(true);
            pause.Hold();

            try
            {
                await ServicesSignIn.SignInAsync();
                await LoadMine(opened);
                await LoadNextPage(opened);
            }
            catch (Exception e)
            {
                Fail(opened, e);
            }
        }

        /// <summary>닫기 버튼과 창 바깥(어둠 판)을 누르면 닫는다.</summary>
        public void Close()
        {
            window.SetActive(false);
            pause.Release();
        }

        private void Awake()
        {
            window.SetActive(false);
        }

        private void OnEnable()
        {
            list.NearEnd += OnNearEnd;
        }

        private void OnDisable()
        {
            list.NearEnd -= OnNearEnd;
        }

        private async void OnNearEnd()
        {
            int opened = openCount;
            try
            {
                await LoadNextPage(opened);
            }
            catch (Exception e)
            {
                Fail(opened, e);
            }
        }

        private async Task LoadMine(int opened)
        {
            try
            {
                var options = new GetPlayerScoreOptions { IncludeMetadata = true };
                LeaderboardEntry mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId, options);
                if (opened == openCount)
                {
                    ShowMine(ToEntry(mine));
                }
            }
            catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                // 사람과 온라인으로 한 판을 끝내야 순위에 든다. 아직이면 안내만 보인다.
            }
        }

        private async Task LoadNextPage(int opened)
        {
            bool loadedAll = total > 0 && entries.Count >= Limit;
            if (loading || loadedAll)
            {
                return;
            }

            loading = true;
            try
            {
                var options = new GetScoresOptions { Offset = entries.Count, Limit = PageSize, IncludeMetadata = true };
                LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, options);
                if (opened != openCount)
                {
                    return;
                }

                total = page.Total;
                foreach (LeaderboardEntry entry in page.Results)
                {
                    if (entries.Count < MaxRank)
                    {
                        entries.Add(ToEntry(entry));
                    }
                }

                ShowStatus(entries.Count == 0 ? "아직 순위에 든 사람이 없어요" : "");
                list.Show(entries);
            }
            finally
            {
                loading = false;
            }
        }

        private RankingEntry ToEntry(LeaderboardEntry entry)
        {
            ScoreMetadata metadata = string.IsNullOrEmpty(entry.Metadata) ? default : JsonUtility.FromJson<ScoreMetadata>(entry.Metadata);
            string nickname = string.IsNullOrEmpty(metadata.nickname) ? entry.PlayerName : metadata.nickname;
            return new RankingEntry(entry.Rank + 1, nickname, profile.PandoraNamed(metadata.pandora), (int)Math.Round(entry.Score));
        }

        /// <summary>내 순위 줄을 보인다. null이면 아직 순위에 들지 않았다는 안내를 보인다.</summary>
        private void ShowMine(RankingEntry? mine)
        {
            myRow.gameObject.SetActive(mine.HasValue);
            myRowHint.gameObject.SetActive(!mine.HasValue);
            if (mine.HasValue)
            {
                myRow.Show(mine.Value, false);
            }
        }

        private void Fail(int opened, Exception e)
        {
            Debug.LogWarning($"[순위] 순위표를 불러오지 못했다: {e.Message}");
            if (opened == openCount)
            {
                ShowStatus("순위표를 불러오지 못했어요. 인터넷 연결을 확인해 주세요");
            }
        }

        private void ShowStatus(string message)
        {
            status.SetText(message);
            status.gameObject.SetActive(message.Length > 0);
        }

        /// <summary>서버(Cloud/ReportDuel.js)가 점수와 함께 적는 것. JSON 이름과 맞추느라 필드 이름이 소문자다.</summary>
        [Serializable]
        private struct ScoreMetadata
        {
            public string nickname;
            public string pandora;
        }
    }
}
