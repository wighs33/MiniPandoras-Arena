using System.Collections.Generic;
using MiniPandoras.Duel;
using MiniPandoras.Fighters;
using MiniPandoras.Skills;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 참가자 화면: 호스트의 조금 전을 그대로 다시 보인다. 받은 모습은 자리마다 쌓아 두고 그 시각의 모습을 메워 두며(보간),
    /// 받은 일(공격·스킬·맞음·새 판)도 그 시각이 되었을 때 보인다. 모습만 늦추고 일을 바로 보이면
    /// 휘두르는 몸짓보다 맞는 순간이 먼저 보인다. 「그 시각」은 다시 보기 시계(<see cref="PlaybackClock"/>)가 정한다.
    /// </summary>
    public sealed class HostPlayback
    {
        // 한 인물의 모습을 쌓아 두는 칸 수. 초당 30틱이면 1초 남짓이다.
        private const int PoseCapacity = 32;

        private readonly Fighter[] fighters;
        private readonly SkillCaster[] skills;
        private readonly DuelMatch match;
        private readonly PandoraDefinition[] pandoras;
        private readonly SnapshotBuffer[] poses;
        private readonly Queue<HostEvent> events = new Queue<HostEvent>();
        private readonly PlaybackClock clock = new PlaybackClock();
        private readonly int predictedSeat;
        // 자리마다 호스트에서 마지막으로 본 「숨었는지」.
        private readonly bool[] hostHidden;

        /// <param name="seatFighters">자리 순서(0 호스트, 1 참가자)로 놓은 이 PC의 인물</param>
        /// <param name="predictedSeat">이 PC의 사람이 예측하는 자리. 그 인물은 받은 모습에 두지 않는다(스스로 움직이고 따로 맞춘다)</param>
        public HostPlayback(Fighter[] seatFighters, SkillCaster[] seatSkills, DuelMatch match, PandoraDefinition[] pandoras, int predictedSeat)
        {
            this.predictedSeat = predictedSeat;
            fighters = seatFighters;
            skills = seatSkills;
            this.match = match;
            this.pandoras = pandoras;
            hostHidden = new bool[seatFighters.Length];
            poses = new SnapshotBuffer[seatFighters.Length];
            for (int seat = 0; seat < poses.Length; seat++)
            {
                poses[seat] = new SnapshotBuffer(PoseCapacity);
            }
        }

        public void AddPose(int seat, double time, FighterSnapshot pose)
        {
            poses[seat].Add(time, pose);
            clock.Received(time);
        }

        public void AddEvent(HostEvent hostEvent)
        {
            events.Enqueue(hostEvent);
            clock.Received(hostEvent.Time);
        }

        /// <summary>
        /// 다시 보기 시계를 흐른 실제 시간만큼 돌리고, 그 시각까지 일어난 일을 보이고, 두 인물을 그 시각의 모습으로 둔다.
        /// </summary>
        /// <param name="delay">받은 가장 새 소식보다 얼마나 늦게 그릴지(초)</param>
        public void Advance(double realDeltaTime, double delay)
        {
            clock.Advance(realDeltaTime, delay);
            if (clock.HasStarted)
            {
                Play(clock.Time);
            }
        }

        private void Play(double time)
        {
            while (events.Count > 0 && events.Peek().Time <= time)
            {
                Show(events.Dequeue());
            }

            for (int seat = 0; seat < poses.Length; seat++)
            {
                if (seat != predictedSeat && poses[seat].TrySample(time, out FighterSnapshot pose))
                {
                    Follow(seat, pose);
                }
            }
        }

        private void Show(HostEvent hostEvent)
        {
            Fighter fighter = fighters[hostEvent.Seat];
            switch (hostEvent.Kind)
            {
                case HostEventKind.RoundBegan:
                    skills[0].Equip(pandoras[hostEvent.HostPandora]);
                    skills[1].Equip(pandoras[hostEvent.GuestPandora]);
                    match.BeginRound();
                    break;
                case HostEventKind.Attacked:
                    fighter.Attack();
                    break;
                case HostEventKind.SkillUsed:
                    skills[hostEvent.Seat].Replay(hostEvent.Slot);
                    break;
                case HostEventKind.Struck:
                    fighter.ShowHostHit(hostEvent.Hit);
                    break;
                case HostEventKind.Evaded:
                    fighter.ShowHostEvade();
                    break;
            }
        }

        private void Follow(int seat, FighterSnapshot pose)
        {
            fighters[seat].Follow(pose.Position, pose.Yaw, pose.Speed);
            skills[seat].ShowHostMana(pose.Mana);
            bool cameOut = hostHidden[seat] && !pose.Hidden;
            hostHidden[seat] = pose.Hidden;
            SkillGate gate = skills[seat].ActiveGate;
            if (gate == null)
            {
                return;
            }

            // 차원이동으로 숨은 동안 표시를 호스트의 자리에 두고, 호스트에서 나왔으면 같은 자리로 나온다.
            // 「숨었다가 나옴」을 보고 나온다. 이 화면의 틈이 호스트보다 한 프레임 먼저 열리면 아직 「안 숨음」을 받기 때문이다.
            gate.PlaceAt(pose.Hidden ? pose.GatePoint : pose.Position);
            if (cameOut)
            {
                gate.Leave();
            }
        }
    }
}
