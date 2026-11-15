namespace BTH.Core.Combat
{
    /// <summary>Cách skill chọn mục tiêu. Vùng ảnh hưởng chi tiết do ISkillBehaviour dựng effect.</summary>
    public enum SkillTargeting
    {
        Self = 0,
        Unit = 1,
        Cell = 2
    }
}
