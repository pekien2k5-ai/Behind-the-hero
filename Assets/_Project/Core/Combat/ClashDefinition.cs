using System;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Dữ liệu Clash của một màn: chiêu boss, power gốc, hệ và hiệu ứng thưởng/phạt.
    /// Content dựng từ ScriptableObject; Core không biết nội dung cụ thể.
    /// </summary>
    public sealed class ClashDefinition
    {
        public string BossSkillId { get; set; } = string.Empty;

        public int BossPower { get; set; }

        public ElementId BossElement { get; set; } = ElementId.None;

        public ClashRuleTable Rules { get; set; } = new ClashRuleTable();

        /// <summary>Hiệu ứng gắn lên boss khi người chơi thắng Clash.</summary>
        public Func<IStatusEffect> WinEffect { get; set; } = () => new StaggerStatus();

        /// <summary>Hiệu ứng gắn lên người chơi khi thua Clash.</summary>
        public Func<IStatusEffect> LoseEffect { get; set; } = () => new StaggerStatus();

        public IStatusEffect CreateWinEffect()
        {
            return WinEffect != null ? WinEffect() : null;
        }

        public IStatusEffect CreateLoseEffect()
        {
            return LoseEffect != null ? LoseEffect() : null;
        }
    }
}
