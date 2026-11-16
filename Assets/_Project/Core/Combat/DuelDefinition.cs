using System;
using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Dữ liệu Duel của một màn boss (Content dựng; Core không biết nội dung cụ thể).
    /// Chốt thiết kế: Docs/QUYET_DINH.md — Draw 10%, HP mang theo, clear status khi vào.
    /// </summary>
    public sealed class DuelDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string BackgroundId { get; set; } = string.Empty;

        public string MusicId { get; set; } = string.Empty;

        public string TransitionId { get; set; } = string.Empty;

        /// <summary>Chu kỳ chiêu boss: tuyệt kỹ → đòn thường → lặp vòng.</summary>
        public List<string> BossSkillIds { get; set; } = new List<string>();

        /// <summary>Đúng 3 chiêu người chơi được chọn trong Duel này.</summary>
        public List<string> PlayerSkillIds { get; set; } = new List<string>();

        /// <summary>Uy lực Clash cố định của boss mỗi vòng; 0 = lấy Power của chiêu boss.</summary>
        public int BossPower { get; set; }

        /// <summary>0 = không giới hạn (mặc định).</summary>
        public int RoundLimit { get; set; }

        /// <summary>Hoà: mỗi bên mất uy lực của mình × ratio (chốt 10%).</summary>
        public float DrawChipRatio { get; set; } = 0.10f;

        /// <summary>Số đòn kết liễu sau khi thắng Clash (ải 4 = 3 nhát).</summary>
        public int FinisherCount { get; set; } = 1;

        public ClashRuleTable Rules { get; set; } = new ClashRuleTable();

        /// <summary>Hiệu ứng gắn lên boss khi thắng Clash (mặc định Stagger).</summary>
        public Func<IStatusEffect> WinEffect { get; set; } = () => new StaggerStatus();

        /// <summary>Hiệu ứng gắn lên người chơi khi thua Clash (mặc định không có).</summary>
        public Func<IStatusEffect> LoseEffect { get; set; }

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
