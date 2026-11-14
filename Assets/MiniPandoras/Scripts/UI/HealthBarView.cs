using MiniPandoras.Fighters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 위 체력 막대 하나. 인물의 체력을 보고 채움을 줄이고, 잃은 만큼 밝은 자국을 잠깐 남겼다가 따라 줄인다(2D판 HUD와 같다).
    /// 상대 막대는 화면 오른쪽 끝에 붙어서 가운데 쪽부터 줄어든다.
    /// </summary>
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Fighter fighter;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform trail;
        [SerializeField] private TMP_Text hpText;
        [Tooltip("오른쪽 끝에 붙은 막대(상대). 켜면 왼쪽부터 줄어든다")]
        [SerializeField] private bool anchoredRight;

        [Header("색 (2D판 UI 지침: 반 아래 주황, 4분의 1 아래 붉음)")]
        [SerializeField] private Color healthyColor = new Color32(0x8C, 0xC4, 0xA8, 0xFF);
        [SerializeField] private Color hurtColor = new Color32(0xDE, 0xAF, 0x76, 0xFF);
        [SerializeField] private Color dangerColor = new Color32(0xC8, 0x51, 0x3D, 0xFF);

        [Header("잃은 만큼 남는 자국")]
        [Tooltip("자국이 따라 줄기 시작할 때까지(초)")]
        [SerializeField] private float trailDelay = 0.4f;
        [Tooltip("자국이 줄어드는 빠르기(초당 막대 길이의 비율)")]
        [SerializeField] private float trailSpeed = 0.9f;

        private int shownHp = -1;
        private float trailRatio = 1f;
        private float trailWait;

        private void Update()
        {
            int hp = fighter.Hp;
            float ratio = (float)hp / fighter.MaxHp;
            if (hp != shownHp)
            {
                if (hp < shownHp)
                {
                    trailWait = trailDelay;
                }
                else
                {
                    // 새 판에서 체력이 다시 차면 자국도 함께 찬다(자국은 채움 뒤에 깔려 있다).
                    trailRatio = ratio;
                }

                shownHp = hp;
                BarFill.Stretch(fill, ratio, anchoredRight);
                fillImage.enabled = hp > 0;
                fillImage.color = ColorFor(ratio);
                hpText.SetText("{0}", hp);
            }

            if (trailWait > 0f)
            {
                trailWait -= Time.deltaTime;
            }
            else
            {
                trailRatio = Mathf.MoveTowards(trailRatio, ratio, trailSpeed * Time.deltaTime);
            }

            BarFill.Stretch(trail, trailRatio, anchoredRight);
        }

        private Color ColorFor(float ratio)
        {
            if (ratio <= 0.25f)
            {
                return dangerColor;
            }

            return ratio <= 0.5f ? hurtColor : healthyColor;
        }
    }
}
