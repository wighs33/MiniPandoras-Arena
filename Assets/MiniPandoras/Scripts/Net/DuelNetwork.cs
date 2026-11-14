using System;
using MiniPandoras.Controls;
using MiniPandoras.Duel;
using MiniPandoras.Fighters;
using MiniPandoras.Skills;
using MiniPandoras.UI;
using Unity.Netcode;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 온라인 1대1. 호스트가 두 인물의 움직임과 판정을 모두 돌리고, 참가자는 명령만 보낸다(<see cref="GuestCommands"/>).
    /// 호스트는 일어난 일(공격·스킬을 쓴 것, 맞은 것, 새 판)을 그때그때, 두 인물의 모습은 네트워크 틱마다 호스트 시계의 시각과 함께 보낸다.
    /// 참가자 화면의 상대는 호스트를 따라 하는 모드(<see cref="Fighter.FollowHost"/>)라서, 받은 일을 같은 코드로 다시 보인다.
    /// 상대는 호스트의 조금 전을 그린다(<see cref="HostPlayback"/>): 받은 모습 사이를 메워 부드럽게 움직인다.
    /// 참가자의 내 인물은 조작에 바로 움직이고(예측, <see cref="Fighter.PredictForHost"/>), 호스트가 알려 온 자리와 견줘
    /// 어긋난 만큼 고친다(<see cref="PositionReconciler"/>). 맞았는지는 두 인물 모두 호스트가 정한다.
    ///
    /// 자리(seat): 0은 호스트의 인물, 1은 참가자의 인물이다. 장면의 Player는 늘 「이 PC의 사람」이라서
    /// 호스트에서는 Player가 자리 0이고 참가자에서는 Player가 자리 1이다. 그래서 HUD 왼쪽, 초록 발밑 고리, 카메라가
    /// 두 화면 모두 고치지 않고 「나」를 가리킨다.
    /// </summary>
    public sealed class DuelNetwork : NetworkBehaviour
    {
        private const int HostSeat = 0;
        private const int GuestSeat = 1;
        // 내 인물의 예측 자리를 적어 두는 칸 수. 초당 30번 보내니 2초 남짓이다.
        private const int InputHistory = 64;

        [SerializeField] private DuelMatch match;
        [Tooltip("이 PC 사람의 닉네임과 판도라. 방에 들어가면 상대에게 알린다")]
        [SerializeField] private PlayerProfile profile;
        [Tooltip("상대 이름표. 온라인에서는 「상대」 대신 상대의 닉네임을 보인다")]
        [SerializeField] private FighterNameView rivalName;
        [SerializeField] private Fighter player;
        [SerializeField] private Fighter rival;
        [SerializeField] private SkillCaster playerSkills;
        [SerializeField] private SkillCaster rivalSkills;
        [SerializeField] private PlayerControls playerControls;
        [Tooltip("시험할 때 이 PC의 사람 대신 내 인물을 조작하는 AI(명령줄 -autoplay, 호스트에서만)")]
        [SerializeField] private Behaviour playerAi;
        [Tooltip("혼자 할 때 상대를 조작하는 AI")]
        [SerializeField] private Behaviour rivalAi;
        [Tooltip("호스트에서 참가자의 인물을 조작한다")]
        [SerializeField] private RemoteControls remoteControls;
        [Tooltip("판도라 번호표. 두 PC가 같은 순서로 판도라를 가리킨다")]
        [SerializeField] private PandoraDefinition[] pandoras;
        [Tooltip("자리마다 입는 차림(0: 호스트, 1: 참가자). 두 화면에서 같은 사람이 같은 옷을 입는다")]
        [SerializeField] private FighterLook[] seatLooks;
        [Tooltip("참가자 화면이 받은 가장 새 소식보다 늦게 그리는 시간(초). 틱 간격(1/30초)의 세 배쯤이면 모습 하나를 놓치거나 " +
                 "늦게 받아도 앞뒤 모습 사이를 메울 수 있다. 0이면 받은 가장 새 모습에 둔다")]
        [SerializeField] private float interpolationDelay = 0.1f;

        private SeatRelay[] relays;
        private MatchLog log;
        private bool autoplay;
        private bool guestJoined;
        // 호스트에서: 참가자에게서 마지막으로 받은 이동 입력의 번호.
        private int guestInput = -1;

        private GuestCommands guestCommands;
        private HostPlayback playback;
        private PositionReconciler reconciler;
        // 참가자에서: 다음에 보낼 이동 입력의 번호.
        private int nextInput;

        /// <summary>
        /// 지금 판의 번호표. 호스트가 새 판마다 무작위로 만들어 참가자에게 알린다. 두 사람이 같은 판의 결과를
        /// 순위표에 보고할 때 이것으로 판을 가리킨다. 상대 없이 혼자 하는 판이면 null이다.
        /// </summary>
        public string MatchId { get; private set; }

        /// <summary>참가자의 버튼을 호스트로 보낸다.</summary>
        public void SendButton(GuestButton button, int slot)
        {
            ButtonRpc(button, slot);
        }

        /// <summary>
        /// 시험용: 호스트의 내 인물을 사람 대신 AI가 조작한다. 빌드 하나를 호스트로 띄워 두고 참가자와 끝까지 싸우게 할 때 쓴다.
        /// 참가자는 명령을 호스트로도 보내야 해서 AI(인물을 바로 조작한다)를 쓸 수 없다.
        /// </summary>
        public void PlayByItself()
        {
            autoplay = true;
        }

        private Behaviour LocalControls => autoplay ? playerAi : playerControls;

        public override void OnNetworkSpawn()
        {
            NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
            if (!IsServer)
            {
                BecomeGuest();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.NetworkTickSystem != null)
            {
                NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
            }

            if (IsServer)
            {
                EndMatch();
            }
            else
            {
                LeaveHost();
            }
        }

        // ── 호스트 ──────────────────────────────────────────────

        /// <summary>참가자가 들어왔다: 서로 닉네임을 알리고, 상대 자리를 참가자에게 넘기고 새 판을 세운다.</summary>
        [Rpc(SendTo.Server)]
        private void JoinRpc(int guestPandora, string guestNickname)
        {
            rivalName.ShowName(PlayerProfile.CleanNickname(guestNickname) ?? "상대");
            MeetRpc(profile.Nickname);
            rivalSkills.Equip(pandoras[guestPandora]);
            match.RivalPicksRandomPandora = false;
            match.UseControls(LocalControls, remoteControls);
            relays = new[] { new SeatRelay(this, HostSeat, Seat(HostSeat), SeatSkills(HostSeat)), new SeatRelay(this, GuestSeat, Seat(GuestSeat), SeatSkills(GuestSeat)) };
            log = new MatchLog("호스트", new[] { Seat(HostSeat), Seat(GuestSeat) });
            match.RoundBegan += OnRoundBegan;
            NetworkManager.OnClientDisconnectCallback += OnGuestLeft;
            guestJoined = true;
            guestInput = -1;
            match.BeginRound();
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        private void MoveRpc(int input, Vector3 direction)
        {
            // 놓칠 수 있는 길이라 순서가 뒤바뀌어 올 수 있다. 더 새 입력만 받는다.
            if (input <= guestInput)
            {
                return;
            }

            guestInput = input;
            remoteControls.ReceiveMove(direction);
        }

        [Rpc(SendTo.Server)]
        private void ButtonRpc(GuestButton button, int slot)
        {
            remoteControls.ReceiveButton(button, slot);
        }

        [Rpc(SendTo.Server)]
        private void RematchRpc()
        {
            match.Rematch();
        }

        private void OnRoundBegan()
        {
            log.BeginRound();
            MatchId = Guid.NewGuid().ToString("N");
            RoundBeganRpc(Now, PandoraIndex(SeatSkills(HostSeat)), PandoraIndex(SeatSkills(GuestSeat)), MatchId);
        }

        private void OnGuestLeft(ulong clientId)
        {
            if (clientId != NetworkManager.ServerClientId)
            {
                EndMatch();
            }
        }

        /// <summary>참가자가 나갔다: 다시 AI 상대와 혼자 한다.</summary>
        private void EndMatch()
        {
            if (!guestJoined)
            {
                return;
            }

            guestJoined = false;
            MatchId = null;
            foreach (SeatRelay relay in relays)
            {
                relay.Stop();
            }

            log.Stop();
            log = null;
            match.RoundBegan -= OnRoundBegan;
            NetworkManager.OnClientDisconnectCallback -= OnGuestLeft;
            match.RivalPicksRandomPandora = true;
            match.UseControls(LocalControls, rivalAi);
            rivalName.ShowDefaultName();
            match.Interrupt("상대가 나갔다");
        }

        private FighterSnapshot Capture(int seat)
        {
            Fighter fighter = Seat(seat);
            SkillGate gate = SeatSkills(seat).ActiveGate;
            return new FighterSnapshot
            {
                Position = fighter.transform.position,
                Yaw = fighter.transform.eulerAngles.y,
                Speed = fighter.Speed,
                Mana = SeatSkills(seat).Mana,
                Hidden = fighter.IsHidden,
                GatePoint = gate != null ? gate.transform.position : fighter.transform.position,
                LastInput = seat == GuestSeat ? guestInput : -1,
            };
        }

        // ── 참가자 ──────────────────────────────────────────────

        /// <summary>
        /// 호스트의 방에 들어왔다: 상대는 호스트를 따라 하게 하고, 내 인물은 예측하게 하고, 내 명령은 내 인물과 호스트에 함께 넘긴다.
        /// </summary>
        private void BecomeGuest()
        {
            player.PredictForHost(true);
            rival.FollowHost(true);
            match.SwapStartPoints();
            match.RivalPicksRandomPandora = false;
            match.WaitsForHost = true;
            match.RematchRequested += RematchRpc;
            match.RoundBegan += OnGuestRoundBegan;
            match.UseControls(playerControls);
            guestCommands = new GuestCommands(this, new FighterCommands(player, playerSkills));
            playerControls.SendCommandsTo(guestCommands);
            playback = new HostPlayback(new[] { Seat(HostSeat), Seat(GuestSeat) }, new[] { SeatSkills(HostSeat), SeatSkills(GuestSeat) },
                match, pandoras, predictedSeat: GuestSeat);
            reconciler = new PositionReconciler(InputHistory);
            nextInput = 0;
            log = new MatchLog("참가자", new[] { Seat(HostSeat), Seat(GuestSeat) });
            for (int seat = 0; seat < seatLooks.Length; seat++)
            {
                Seat(seat).GetComponent<FighterOutfit>().Wear(seatLooks[seat]);
            }

            JoinRpc(Array.IndexOf(pandoras, profile.Pandora), profile.Nickname);
        }

        /// <summary>호스트와 끊겼다: 다시 AI 상대와 혼자 한다.</summary>
        private void LeaveHost()
        {
            player.PredictForHost(false);
            rival.FollowHost(false);
            match.SwapStartPoints();
            match.RivalPicksRandomPandora = true;
            match.WaitsForHost = false;
            match.RematchRequested -= RematchRpc;
            match.RoundBegan -= OnGuestRoundBegan;
            match.UseControls(playerControls, rivalAi);
            playerControls.SendCommandsTo(null);
            guestCommands = null;
            playback = null;
            reconciler = null;
            MatchId = null;
            log.Stop();
            log = null;
            for (int seat = 0; seat < seatLooks.Length; seat++)
            {
                // 혼자 할 때는 장면에 놓인 그대로(Player가 호스트 자리의 차림).
                (seat == HostSeat ? player : rival).GetComponent<FighterOutfit>().Wear(seatLooks[seat]);
            }

            rivalName.ShowDefaultName();
            match.Interrupt("호스트와 연결이 끊겼다");
        }

        /// <summary>새 판: 내 인물이 처음 자리로 돌아갔으니 예측해 둔 자리를 잊는다.</summary>
        private void OnGuestRoundBegan()
        {
            reconciler.Clear();
            log.BeginRound();
        }

        // 상대에 대한 소식은 바로 보이지 않고 HostPlayback에 쌓아, 호스트 시계로 interpolationDelay만큼 늦게 보인다(Update).
        // 내 인물은 지금을 살고 있으므로(예측) 나를 맞힌 소식은 받는 대로 보이고, 내가 한 공격·스킬은 이미 해 보였으니 버린다.

        [Rpc(SendTo.NotServer)]
        private void MeetRpc(string hostNickname)
        {
            rivalName.ShowName(PlayerProfile.CleanNickname(hostNickname) ?? "상대");
        }

        [Rpc(SendTo.NotServer)]
        private void RoundBeganRpc(double time, int hostPandora, int guestPandora, string matchId)
        {
            // 판이 보이는 것은 늦추지만 번호표는 바로 받는다. 지난 판의 승패는 이미 났다.
            MatchId = matchId;
            playback.AddEvent(HostEvent.RoundBegan(time, hostPandora, guestPandora));
        }

        [Rpc(SendTo.NotServer)]
        private void AttackedRpc(double time, int seat)
        {
            if (seat != GuestSeat)
            {
                playback.AddEvent(HostEvent.Attacked(time, seat));
            }
        }

        [Rpc(SendTo.NotServer)]
        private void SkillUsedRpc(double time, int seat, int slot)
        {
            if (seat != GuestSeat)
            {
                playback.AddEvent(HostEvent.SkillUsed(time, seat, slot));
            }
        }

        [Rpc(SendTo.NotServer)]
        private void StruckRpc(double time, int seat, HitMessage hit)
        {
            if (seat == GuestSeat)
            {
                player.ShowHostHit(hit.ToHit());
            }
            else
            {
                playback.AddEvent(HostEvent.Struck(time, seat, hit.ToHit()));
            }
        }

        [Rpc(SendTo.NotServer)]
        private void EvadedRpc(double time, int seat)
        {
            if (seat == GuestSeat)
            {
                player.ShowHostEvade();
            }
            else
            {
                playback.AddEvent(HostEvent.Evaded(time, seat));
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void SnapshotRpc(double time, FighterSnapshot host, FighterSnapshot guest)
        {
            playback.AddPose(HostSeat, time, host);
            playback.AddPose(GuestSeat, time, guest);
            reconciler.Reconcile(guest.LastInput, guest.Position);
        }

        private void Update()
        {
            if (playback == null)
            {
                return;
            }

            // 쓰러진 순간의 느린 화면(timeScale)과 상관없이 호스트 시계는 실제 시간대로 흐른다.
            playback.Advance(Time.unscaledDeltaTime, interpolationDelay);
            player.Nudge(reconciler.TakeCorrection(Time.unscaledDeltaTime));
        }

        // ── 둘 다 ──────────────────────────────────────────────

        /// <summary>호스트 시계의 지금(초). 호스트가 보내는 소식에 붙인다.</summary>
        private double Now => NetworkManager.ServerTime.Time;

        private void OnNetworkTick()
        {
            if (IsServer && guestJoined)
            {
                SnapshotRpc(Now, Capture(HostSeat), Capture(GuestSeat));
            }
            else if (!IsServer && guestCommands != null)
            {
                // 이 입력을 보낼 때의 내 자리를 적어 둔다. 호스트가 이 입력까지 반영한 자리를 알려 오면 견준다.
                nextInput++;
                reconciler.Record(nextInput, player.transform.position);
                MoveRpc(nextInput, guestCommands.Direction);
            }
        }

        /// <summary>그 자리의 인물이 이 PC에서 어느 쪽인지(이 PC의 사람은 늘 Player다).</summary>
        private Fighter Seat(int seat)
        {
            return IsLocalSeat(seat) ? player : rival;
        }

        private SkillCaster SeatSkills(int seat)
        {
            return IsLocalSeat(seat) ? playerSkills : rivalSkills;
        }

        private bool IsLocalSeat(int seat)
        {
            return (seat == HostSeat) == IsServer;
        }

        private int PandoraIndex(SkillCaster skills)
        {
            return Array.IndexOf(pandoras, skills.Pandora);
        }

        /// <summary>호스트에서 한 자리의 인물이 한 일을 참가자에게 알린다.</summary>
        private sealed class SeatRelay
        {
            private readonly DuelNetwork network;
            private readonly int seat;
            private readonly Fighter fighter;
            private readonly SkillCaster skills;

            public SeatRelay(DuelNetwork network, int seat, Fighter fighter, SkillCaster skills)
            {
                this.network = network;
                this.seat = seat;
                this.fighter = fighter;
                this.skills = skills;
                fighter.Attacked += OnAttacked;
                fighter.Struck += OnStruck;
                fighter.Evaded += OnEvaded;
                skills.Used += OnUsed;
            }

            public void Stop()
            {
                fighter.Attacked -= OnAttacked;
                fighter.Struck -= OnStruck;
                fighter.Evaded -= OnEvaded;
                skills.Used -= OnUsed;
            }

            private void OnAttacked()
            {
                network.AttackedRpc(network.Now, seat);
            }

            private void OnStruck(Hit hit)
            {
                network.StruckRpc(network.Now, seat, new HitMessage(hit));
            }

            private void OnEvaded()
            {
                network.EvadedRpc(network.Now, seat);
            }

            private void OnUsed(int slot)
            {
                network.SkillUsedRpc(network.Now, seat, slot);
            }
        }
    }
}
