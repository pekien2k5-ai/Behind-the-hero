using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Định nghĩa tĩnh một kỹ năng. Hành vi nằm ở ISkillBehaviour (Content).</summary>
    public sealed class SkillDefinition
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public SkillTargeting Targeting { get; set; } = SkillTargeting.Cell;

        public int RangeMin { get; set; } = 1;

        public int RangeMax { get; set; } = 1;

        /// <summary>Uy lực Clash (so power với boss trong Duel).</summary>
        public int Power { get; set; }

        /// <summary>
        /// Sát thương gây trên bàn cờ. 0 = behaviour tự quyết định (dùng fallback của behaviour).
        /// Đây là nguồn duy nhất cho sát thương board — behaviour đọc lại từ đây.
        /// </summary>
        public int BoardPower { get; set; }

        public ElementId Element { get; set; } = ElementId.None;

        public int ApCost { get; set; } = 1;

        public int CooldownTurns { get; set; }

        public bool RequiresLineOfSight { get; set; }

        public int AreaRadius { get; set; }

        /// <summary>Chiêu dùng để so Clash với boss.</summary>
        public bool IsClashSkill { get; set; }

        public ISkillBehaviour Behaviour { get; set; }

        public MoveProfile MoveAfterUse { get; set; }
    }
}
