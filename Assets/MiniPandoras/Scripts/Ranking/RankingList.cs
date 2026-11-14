using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Ranking
{
    /// <summary>
    /// 순위표의 줄 목록. 5000줄을 모두 만들지 않고 화면에 보이는 만큼의 줄(<see cref="RankingRow"/>)만 만들어 둔다.
    /// 스크롤할 때마다 그 줄들을 보이는 자리로 옮기고 그 자리의 순위를 다시 채운다.
    /// 받아 둔 줄의 끝 가까이 내려가면 다음 쪽을 불러 달라고 알린다(<see cref="NearEnd"/>).
    /// </summary>
    public sealed class RankingList : MonoBehaviour
    {
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [Tooltip("줄 하나의 본. 꺼 두고 보이는 만큼 복제한다")]
        [SerializeField] private RankingRow rowTemplate;
        [SerializeField] private float rowHeight = 84f;
        [Tooltip("받아 둔 줄이 화면 아래로 이만큼 남았을 때 다음 쪽을 부른다")]
        [SerializeField] private int loadAhead = 20;

        private readonly List<RankingRow> rows = new List<RankingRow>();
        private readonly List<RectTransform> rowRects = new List<RectTransform>();
        private IReadOnlyList<RankingEntry> entries = Array.Empty<RankingEntry>();
        // 지금 맨 위에 그린 줄의 번호. 스크롤해도 맨 위 줄이 그대로면 다시 채우지 않는다.
        private int firstShown = -1;

        public event Action NearEnd;

        /// <summary>받아 둔 줄을 보인다. 다음 쪽을 받을 때마다 같은 목록으로 다시 부른다.</summary>
        public void Show(IReadOnlyList<RankingEntry> shownEntries)
        {
            entries = shownEntries;
            content.sizeDelta = new Vector2(content.sizeDelta.x, entries.Count * rowHeight);
            firstShown = -1;
            Redraw();
        }

        public void ScrollToTop()
        {
            scroll.verticalNormalizedPosition = 1f;
        }

        private void Awake()
        {
            // 보이는 줄 수 + 위아래로 반쯤 걸치는 줄 둘.
            int count = Mathf.CeilToInt(scroll.viewport.rect.height / rowHeight) + 2;
            for (int i = 0; i < count; i++)
            {
                RankingRow row = Instantiate(rowTemplate, content);
                rows.Add(row);
                rowRects.Add((RectTransform)row.transform);
            }

            rowTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            scroll.onValueChanged.AddListener(OnScrolled);
        }

        private void OnDisable()
        {
            scroll.onValueChanged.RemoveListener(OnScrolled);
        }

        private void OnScrolled(Vector2 position)
        {
            Redraw();
        }

        private void Redraw()
        {
            int first = Mathf.Clamp(Mathf.FloorToInt(content.anchoredPosition.y / rowHeight), 0, Mathf.Max(0, entries.Count - 1));
            if (first == firstShown)
            {
                return;
            }

            firstShown = first;
            for (int i = 0; i < rows.Count; i++)
            {
                int index = first + i;
                bool used = index < entries.Count;
                rows[i].gameObject.SetActive(used);
                if (used)
                {
                    rowRects[i].anchoredPosition = new Vector2(0f, -index * rowHeight);
                    rows[i].Show(entries[index], index % 2 == 1);
                }
            }

            if (first + rows.Count + loadAhead >= entries.Count)
            {
                NearEnd?.Invoke();
            }
        }
    }
}
