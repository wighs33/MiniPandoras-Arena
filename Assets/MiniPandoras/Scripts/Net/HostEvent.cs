using MiniPandoras.Fighters;

namespace MiniPandoras.Net
{
    public enum HostEventKind : byte
    {
        RoundBegan,
        Attacked,
        SkillUsed,
        Struck,
        Evaded,
    }

    /// <summary>호스트에서 일어난 일 하나와 그 시각(호스트 시계). 참가자 화면이 모습과 같은 시각에 맞춰 보인다.</summary>
    public readonly struct HostEvent
    {
        private HostEvent(double time, HostEventKind kind, int seat, int slot = 0, Hit hit = default, int hostPandora = 0, int guestPandora = 0)
        {
            Time = time;
            Kind = kind;
            Seat = seat;
            Slot = slot;
            Hit = hit;
            HostPandora = hostPandora;
            GuestPandora = guestPandora;
        }

        public double Time { get; }
        public HostEventKind Kind { get; }
        /// <summary>일을 한(당한) 인물의 자리.</summary>
        public int Seat { get; }
        /// <summary>쓴 스킬의 칸(SkillUsed).</summary>
        public int Slot { get; }
        /// <summary>맞은 결과(Struck).</summary>
        public Hit Hit { get; }
        /// <summary>새 판에 두 사람이 든 판도라의 번호(RoundBegan).</summary>
        public int HostPandora { get; }
        public int GuestPandora { get; }

        public static HostEvent RoundBegan(double time, int hostPandora, int guestPandora)
        {
            return new HostEvent(time, HostEventKind.RoundBegan, 0, hostPandora: hostPandora, guestPandora: guestPandora);
        }

        public static HostEvent Attacked(double time, int seat)
        {
            return new HostEvent(time, HostEventKind.Attacked, seat);
        }

        public static HostEvent SkillUsed(double time, int seat, int slot)
        {
            return new HostEvent(time, HostEventKind.SkillUsed, seat, slot);
        }

        public static HostEvent Struck(double time, int seat, Hit hit)
        {
            return new HostEvent(time, HostEventKind.Struck, seat, hit: hit);
        }

        public static HostEvent Evaded(double time, int seat)
        {
            return new HostEvent(time, HostEventKind.Evaded, seat);
        }
    }
}
