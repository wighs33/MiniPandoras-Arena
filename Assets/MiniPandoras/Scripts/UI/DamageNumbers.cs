using MiniPandoras.Fighters;
using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 맞은 인물의 머리 위에 피해 숫자를 띄운다. 숫자는 맞은 자리에서 떠오르며 사라진다.
    /// 글자는 미리 만들어 둔 몇 개를 돌려 쓴다(맞을 때마다 만들지 않는다).
    /// </summary>
    public sealed class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private Fighter player;
        [SerializeField] private Fighter rival;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform area;
        [Tooltip("돌려 쓸 숫자 글자들. 모두 꺼 둔 채로 둔다")]
        [SerializeField] private TMP_Text[] labels;

        [Header("색 (2D판 전투 색)")]
        [Tooltip("상대가 맞았을 때")]
        [SerializeField] private Color hitColor = Color.white;
        [Tooltip("치명타")]
        [SerializeField] private Color criticalColor = new Color32(0xFF, 0xD8, 0x4A, 0xFF);
        [Tooltip("내가 맞았을 때")]
        [SerializeField] private Color hurtColor = new Color32(0xFF, 0x7A, 0x6A, 0xFF);
        [Tooltip("위상전이로 공격을 흘렸을 때(「흘림」, 차원 판도라 색을 밝게)")]
        [SerializeField] private Color evadeColor = new Color32(0xB4, 0xAA, 0xFF, 0xFF);

        [Header("움직임")]
        [Tooltip("숫자가 뜨는 높이(m, 발 기준)")]
        [SerializeField] private float headHeight = 1.9f;
        [Tooltip("사는 동안 떠오르는 높이(m)")]
        [SerializeField] private float rise = 0.5f;
        [Tooltip("사는 시간(초)")]
        [SerializeField] private float lifetime = 0.8f;
        [Tooltip("처음 튀어나올 때의 크기 배율")]
        [SerializeField] private float popScale = 1.6f;
        [Tooltip("튀어나온 크기에서 제 크기로 줄어드는 시간(초)")]
        [SerializeField] private float popTime = 0.12f;
        [Tooltip("치명타 숫자의 크기 배율")]
        [SerializeField] private float criticalScale = 1.35f;

        private Vector3[] origins;
        private float[] ages;
        private float[] sizes;
        private int next;

        private void Awake()
        {
            origins = new Vector3[labels.Length];
            ages = new float[labels.Length];
            sizes = new float[labels.Length];
        }

        private void OnEnable()
        {
            player.Struck += OnPlayerStruck;
            rival.Struck += OnRivalStruck;
            player.Evaded += OnPlayerEvaded;
            rival.Evaded += OnRivalEvaded;
        }

        private void OnDisable()
        {
            player.Struck -= OnPlayerStruck;
            rival.Struck -= OnRivalStruck;
            player.Evaded -= OnPlayerEvaded;
            rival.Evaded -= OnRivalEvaded;
        }

        private void OnPlayerEvaded()
        {
            ShowEvade(player);
        }

        private void OnRivalEvaded()
        {
            ShowEvade(rival);
        }

        private void ShowEvade(Fighter fighter)
        {
            TMP_Text label = Pop(fighter, evadeColor, 1f);
            label.SetText("흘림");
        }

        private void OnPlayerStruck(Hit hit)
        {
            Show(player, hit, hurtColor);
        }

        private void OnRivalStruck(Hit hit)
        {
            Show(rival, hit, hit.Critical ? criticalColor : hitColor);
        }

        private void Show(Fighter fighter, Hit hit, Color color)
        {
            TMP_Text label = Pop(fighter, color, hit.Critical ? criticalScale : 1f);
            label.SetText("{0}", hit.Damage);
        }

        /// <summary>그 인물의 머리 위에 글자 하나를 띄운다(무엇을 쓸지는 부르는 쪽이 정한다).</summary>
        private TMP_Text Pop(Fighter fighter, Color color, float scale)
        {
            // 가장 오래된 글자부터 다시 쓴다.
            int index = next;
            next = (next + 1) % labels.Length;

            // 연타 숫자가 겹치지 않게 옆으로 조금씩 흩는다.
            Vector3 side = worldCamera.transform.right * Random.Range(-0.25f, 0.25f);
            origins[index] = fighter.transform.position + Vector3.up * headHeight + side;
            ages[index] = 0f;
            sizes[index] = scale;

            TMP_Text label = labels[index];
            label.color = color;
            label.gameObject.SetActive(true);
            Place(index);
            return label;
        }

        private void LateUpdate()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (!labels[i].gameObject.activeSelf)
                {
                    continue;
                }

                ages[i] += Time.deltaTime;
                if (ages[i] >= lifetime)
                {
                    labels[i].gameObject.SetActive(false);
                    continue;
                }

                Place(i);
            }
        }

        private void Place(int index)
        {
            float age = ages[index];
            float progress = age / lifetime;
            float lift = rise * (1f - (1f - progress) * (1f - progress));
            Vector3 screen = worldCamera.WorldToScreenPoint(origins[index] + Vector3.up * lift);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, worldCamera, out Vector2 local);

            TMP_Text label = labels[index];
            label.rectTransform.anchoredPosition = local;
            float pop = Mathf.Lerp(popScale, 1f, Mathf.Clamp01(age / popTime));
            label.rectTransform.localScale = Vector3.one * (pop * sizes[index]);
            // 마지막 3분의 1 동안 사라진다.
            label.alpha = Mathf.Clamp01((1f - progress) * 3f);
        }
    }
}
