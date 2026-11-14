using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>맞은 순간 몸을 하얗게 번쩍였다가 되돌린다(2D판의 Flash). 툰 셰이더의 _Flash 값을 쓴다.</summary>
    public sealed class FighterFlash : MonoBehaviour
    {
        private static readonly int FlashProperty = Shader.PropertyToID("_Flash");

        [Tooltip("하얗게 빛났다가 사라지는 시간(초). 2D판의 10프레임")]
        [SerializeField] private float duration = 0.17f;

        private readonly List<Material> materials = new List<Material>();
        private float left;

        public void Play()
        {
            left = duration;
            enabled = true;
        }

        private void Awake()
        {
            // MaterialPropertyBlock으로 값을 넣으면 인물이 외곽선 색으로 덮여 보여서(2026-11-13) 재질 사본에 넣는다.
            // 꺼 둔 옷(드레스)도 갈아입으면 번쩍여야 하므로 함께 모은다.
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                materials.AddRange(part.materials);
            }

            enabled = false;
        }

        private void Update()
        {
            left -= Time.deltaTime;
            float amount = Mathf.Clamp01(left / duration);
            foreach (Material material in materials)
            {
                material.SetFloat(FlashProperty, amount);
            }

            if (left <= 0f)
            {
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
            {
                Destroy(material);
            }
        }
    }
}
