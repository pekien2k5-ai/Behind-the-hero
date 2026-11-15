using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Units;

namespace BTH.Core.Run
{
    /// <summary>
    /// Ảnh chụp phiên Duel để rewind. Không chứa UnitState — unit đã nằm trong
    /// UnitSnapshot của GameSnapshot; chỉ giữ con trỏ vòng/chiêu và kết quả.
    /// </summary>
    public sealed class DuelSnapshot
    {
        public DuelSnapshot(string duelId, int round, int bossSkillIndex, string bossSkillId,
            int bossPower, ElementId bossElement, bool awaitingFinisher, int finishersRemaining,
            ClashResult lastResult, bool isOver, UnitId winner, IReadOnlyList<string> seenBossSkills)
        {
            DuelId = duelId ?? string.Empty;
            Round = round;
            BossSkillIndex = bossSkillIndex;
            BossSkillId = bossSkillId ?? string.Empty;
            BossPower = bossPower;
            BossElement = bossElement;
            AwaitingFinisher = awaitingFinisher;
            FinishersRemaining = finishersRemaining;
            LastResult = lastResult;
            IsOver = isOver;
            Winner = winner;
            SeenBossSkills = seenBossSkills ?? new List<string>();
        }

        public string DuelId { get; }

        public int Round { get; }

        public int BossSkillIndex { get; }

        public string BossSkillId { get; }

        public int BossPower { get; }

        public ElementId BossElement { get; }

        public bool AwaitingFinisher { get; }

        public int FinishersRemaining { get; }

        public ClashResult LastResult { get; }

        public bool IsOver { get; }

        public UnitId Winner { get; }

        public IReadOnlyList<string> SeenBossSkills { get; }

        public bool ContentEquals(DuelSnapshot other)
        {
            if (other == null)
            {
                return false;
            }

            if (DuelId != other.DuelId || Round != other.Round ||
                BossSkillIndex != other.BossSkillIndex || BossSkillId != other.BossSkillId ||
                BossPower != other.BossPower || BossElement != other.BossElement ||
                AwaitingFinisher != other.AwaitingFinisher ||
                FinishersRemaining != other.FinishersRemaining || LastResult != other.LastResult ||
                IsOver != other.IsOver || Winner != other.Winner ||
                SeenBossSkills.Count != other.SeenBossSkills.Count)
            {
                return false;
            }

            for (int i = 0; i < SeenBossSkills.Count; i++)
            {
                if (SeenBossSkills[i] != other.SeenBossSkills[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
