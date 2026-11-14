using MiniPandoras.Skills;

namespace MiniPandoras.Ranking
{
    /// <summary>순위표의 한 줄: 순위(1부터), 닉네임, 마지막 판에 든 판도라(모르면 null), Elo 점수.</summary>
    public readonly struct RankingEntry
    {
        public RankingEntry(int rank, string nickname, PandoraDefinition pandora, int score)
        {
            Rank = rank;
            Nickname = nickname;
            Pandora = pandora;
            Score = score;
        }

        public int Rank { get; }
        public string Nickname { get; }
        public PandoraDefinition Pandora { get; }
        public int Score { get; }
    }
}
