using System;
using MiniPandoras.Duel;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 시험용 명령줄. 사람이 하는 방 만들기·참가는 <see cref="OnlineRoom"/>의 메뉴가 맡는다.
    /// 주소에 바로 잇기(Relay 없이): <c>-host</c>, <c>-join [주소]</c>.
    /// Relay 방: <c>-room</c>(방을 열고 코드를 로그에 남김), <c>-code 방코드</c>, <c>-quick</c>(빠른 매칭).
    /// 나쁜 네트워크 흉내 <c>-lag 밀리초 -jitter 밀리초 -loss 퍼센트</c>, 호스트의 내 인물을 AI에게 맡기기 <c>-autoplay</c>.
    /// 관전 모드로 시작하기 <c>-spectate</c>(AI끼리 쉬지 않고 싸우는 판이라 성능 측정과 영상에 쓴다).
    /// </summary>
    public sealed class NetworkSession : MonoBehaviour
    {
        [SerializeField] private NetworkManager network;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private DuelNetwork duel;
        [SerializeField] private OnlineRoom room;
        [SerializeField] private SpectateMode spectate;
        [Tooltip("이 PC가 보내는 소식을 늦추고 흔들고 잃는다(개발 빌드와 에디터에서만 움직인다)")]
        [SerializeField] private NetworkSimulator simulator;
        [Tooltip("참가할 호스트의 주소")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        public void Host()
        {
            // 어느 주소로 오든 받는다.
            transport.SetConnectionData(address, port, "0.0.0.0");
            network.StartHost();
        }

        public void Join()
        {
            transport.SetConnectionData(address, port);
            network.StartClient();
        }

        /// <summary>
        /// 이 PC가 보내는 소식을 그만큼 늦추고(밀리초), 늦어지는 정도를 흔들고(밀리초), 그 비율(%)만큼 잃는다.
        /// 보내는 쪽에만 걸리므로 두 PC에 함께 걸어야 오가는 길 모두가 나빠진다.
        /// </summary>
        public void SimulateConditions(int lagMs, int jitterMs, int lossPercent)
        {
            simulator.ConnectionPreset = NetworkSimulatorPreset.Create("시험", packetDelayMs: lagMs, packetJitterMs: jitterMs, packetLossPercent: lossPercent);
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int lag = NumberAfter(args, "-lag");
            int jitter = NumberAfter(args, "-jitter");
            int loss = NumberAfter(args, "-loss");
            if (lag > 0 || jitter > 0 || loss > 0)
            {
                SimulateConditions(lag, jitter, loss);
            }

            if (Array.IndexOf(args, "-autoplay") >= 0)
            {
                duel.PlayByItself();
            }

            int joinAt = Array.IndexOf(args, "-join");
            int codeAt = Array.IndexOf(args, "-code");
            if (Array.IndexOf(args, "-host") >= 0)
            {
                Host();
            }
            else if (joinAt >= 0)
            {
                if (joinAt + 1 < args.Length && !args[joinAt + 1].StartsWith("-"))
                {
                    address = args[joinAt + 1];
                }

                Join();
            }
            else if (Array.IndexOf(args, "-room") >= 0)
            {
                room.CreateRoom();
            }
            else if (codeAt >= 0 && codeAt + 1 < args.Length)
            {
                room.JoinRoom(args[codeAt + 1]);
            }
            else if (Array.IndexOf(args, "-quick") >= 0)
            {
                room.QuickMatch();
            }
            else if (Array.IndexOf(args, "-spectate") >= 0)
            {
                spectate.Enter();
            }
        }

        private static int NumberAfter(string[] args, string option)
        {
            int at = Array.IndexOf(args, option);
            return at >= 0 && at + 1 < args.Length && int.TryParse(args[at + 1], out int value) ? value : 0;
        }
    }
}
