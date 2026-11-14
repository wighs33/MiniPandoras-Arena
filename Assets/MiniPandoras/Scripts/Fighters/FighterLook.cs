using System;
using UnityEngine;

namespace MiniPandoras.Fighters
{
    /// <summary>판도라 하나의 차림. 인물(Luna)은 한 몸이고 판도라마다 옷을 갈아입는다. 정본은 Data/Fighters/Looks의 에셋이다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Fighter Look", fileName = "FighterLook")]
    public sealed class FighterLook : ScriptableObject
    {
        [Tooltip("입을 옷 오브젝트의 이름들(Luna.fbx의 Jacket·Shorts, Dress_Angel, Dress_Gothic, Dress_Saint)")]
        [SerializeField] private string[] pieces = Array.Empty<string>();

        public bool Wears(string piece)
        {
            return Array.IndexOf(pieces, piece) >= 0;
        }
    }
}
