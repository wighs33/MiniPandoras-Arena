using System;
using MiniPandoras.Fighters;
using UnityEngine;

namespace MiniPandoras.Net
{
    /// <summary>
    /// 온라인 대전 기록. 두 화면의 결과가 같은지 견주려고, 맞을 때마다(자리, 피해, 남은 체력)와 쓰러짐을 로그로 남긴다.
    /// 호스트와 참가자가 같은 형식·같은 자리 번호로 남기므로 두 로그를 줄마다 견줄 수 있다(관문 3의 시험 기록).
    /// 맞을 때만 남기므로 매 프레임 메모리를 만들지 않는다.
    /// </summary>
    public sealed class MatchLog
    {
        private const string Tag = "[대전 기록]";

        private readonly string side;
        private readonly Fighter[] seats;
        private readonly Action<Hit>[] onStruck;
        private readonly Action[] onKnockedOut;
        private int round;

        /// <param name="side">이 PC가 누구인지(「호스트」, 「참가자」)</param>
        /// <param name="seatFighters">자리 순서(0 호스트, 1 참가자)로 놓은 이 PC의 인물</param>
        public MatchLog(string side, Fighter[] seatFighters)
        {
            this.side = side;
            seats = seatFighters;
            onStruck = new Action<Hit>[seats.Length];
            onKnockedOut = new Action[seats.Length];
            for (int seat = 0; seat < seats.Length; seat++)
            {
                int thisSeat = seat;
                onStruck[seat] = hit => Debug.Log($"{Tag} {side} 판{round} 자리{thisSeat} 맞음 피해 {hit.Damage}{(hit.Critical ? "(치명타)" : "")} 남은 체력 {seats[thisSeat].Hp}");
                onKnockedOut[seat] = () => Debug.Log($"{Tag} {side} 판{round} 자리{thisSeat} 쓰러짐");
                seats[seat].Struck += onStruck[seat];
                seats[seat].KnockedOut += onKnockedOut[seat];
            }
        }

        public void BeginRound()
        {
            round++;
            Debug.Log($"{Tag} {side} 판{round} 시작");
        }

        public void Stop()
        {
            for (int seat = 0; seat < seats.Length; seat++)
            {
                seats[seat].Struck -= onStruck[seat];
                seats[seat].KnockedOut -= onKnockedOut[seat];
            }
        }
    }
}
