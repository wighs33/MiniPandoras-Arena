using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>인물에 판도라의 차림(<see cref="FighterLook"/>)을 입힌다. 옷은 모델에 모두 들어 있고, 그 차림의 옷만 켠다.</summary>
    public sealed class FighterOutfit : MonoBehaviour
    {
        [SerializeField] private FighterLook look;
        [Tooltip("갈아입는 옷 렌더러 전부")]
        [SerializeField] private Renderer[] outfits;

        public void Wear(FighterLook newLook)
        {
            look = newLook;
            foreach (Renderer outfit in outfits)
            {
                outfit.gameObject.SetActive(look.Wears(outfit.name));
            }
        }

        private void Awake()
        {
            Wear(look);
        }
    }
}
