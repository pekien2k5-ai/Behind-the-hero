using System.Collections.Generic;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// State của một phiên Duel. Chỉ IClashDuel được ghi; UI/Presenter chỉ đọc.
    /// </summary>
    public sealed class DuelState
    {
        private readonly List<string> _seenBossSkills = new List<string>();

        public string DuelId { get; internal set; } = string.Empty;

        /// <summary>Vòng hiện tại, 1-based.</summary>
        public int Round { get; internal set; }

        public int BossSkillIndex { get; internal set; }

        public string BossSkillId { get; internal set; } = string.Empty;

        public int BossPower { get; internal set; }

        public ElementId BossElement { get; internal set; } = ElementId.None;

        /// <summary>Đã thắng Clash, chờ đánh kết liễu.</summary>
        public bool AwaitingFinisher { get; internal set; }

        public int FinishersRemaining { get; internal set; }

        public ClashResult LastResult { get; internal set; }

        public bool IsOver { get; internal set; }

        public UnitId Winner { get; internal set; }

        public IReadOnlyList<string> SeenBossSkills => _seenBossSkills;

        internal void AddSeen(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || _seenBossSkills.Contains(skillId))
            {
                return;
            }

            _seenBossSkills.Add(skillId);
        }

        internal void RestoreSeen(IReadOnlyList<string> ids)
        {
            _seenBossSkills.Clear();
            if (ids == null)
            {
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                _seenBossSkills.Add(ids[i]);
            }
        }
    }
}
