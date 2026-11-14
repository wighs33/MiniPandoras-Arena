using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 온라인 상대와의 왕복 시간(핑)을 방 메뉴 아래에 보인다. 호스트는 참가자까지, 참가자는 호스트까지 잰다
    /// (Relay를 거치면 그 길까지 든다). 숫자가 매 프레임 바뀌면 읽을 수 없어서 0.5초마다 고친다. 상대가 없으면 숨긴다.
    /// </summary>
    public sealed class PingIndicator : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [Tooltip("핑 표시 묶음(점과 글). 상대가 있을 때만 켠다")]
        [SerializeField] private GameObject display;
        [SerializeField] private Image dot;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float refreshInterval = 0.5f;

        [Header("빠르기 구간 (왕복 ms)")]
        [Tooltip("이보다 빠르면 「좋음」")]
        [SerializeField] private int goodBelow = 100;
        [Tooltip("이보다 빠르면 「보통」, 아니면 「느림」")]
        [SerializeField] private int fairBelow = 200;

        [Header("색 (2D판 UI 지침)")]
        [SerializeField] private Color goodColor = new Color32(0x8C, 0xC4, 0xA8, 0xFF);
        [SerializeField] private Color fairColor = new Color32(0xDE, 0xAF, 0x76, 0xFF);
        [SerializeField] private Color slowColor = new Color32(0xC8, 0x51, 0x3D, 0xFF);

        private float nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + refreshInterval;
            bool hasPeer = TryGetPeer(out ulong peer);
            display.SetActive(hasPeer);
            if (hasPeer)
            {
                Show((int)network.NetworkConfig.NetworkTransport.GetCurrentRtt(peer));
            }
        }

        /// <summary>핑을 잴 상대: 호스트에서는 참가자, 참가자에서는 호스트.</summary>
        private bool TryGetPeer(out ulong peer)
        {
            peer = NetworkManager.ServerClientId;
            if (!network.IsListening)
            {
                return false;
            }

            if (!network.IsServer)
            {
                return network.IsConnectedClient;
            }

            var clients = network.ConnectedClientsIds;
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] != NetworkManager.ServerClientId)
                {
                    peer = clients[i];
                    return true;
                }
            }

            return false;
        }

        private void Show(int roundTripMs)
        {
            label.SetText("{0}ms", roundTripMs);
            dot.color = roundTripMs < goodBelow ? goodColor : roundTripMs < fairBelow ? fairColor : slowColor;
        }
    }
}
