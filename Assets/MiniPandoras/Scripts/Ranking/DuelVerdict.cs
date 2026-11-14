namespace MiniPandoras.Ranking
{
    /// <summary>
    /// Cloud Code 「ReportDuel」의 답. Status는 "waiting"(상대 보고를 기다림), "rated"(두 보고가 맞아 점수를 바꿈),
    /// "settled"(상대 쪽 호출이 이미 정리함), "disputed"(두 보고가 맞지 않아 점수를 바꾸지 않음) 가운데 하나다.
    /// Rating·Change는 "rated"일 때만 채워진다.
    /// </summary>
    public sealed class DuelVerdict
    {
        public string Status { get; set; }
        public int Rating { get; set; }
        public int Change { get; set; }
    }
}
