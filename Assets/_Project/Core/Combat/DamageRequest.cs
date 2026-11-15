using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Yêu cầu tính sát thương. Không chứa RNG: mọi hệ số đến từ state.</summary>
    public readonly struct DamageRequest
    {
        public readonly UnitId Source;
        public readonly UnitId Target;
        public readonly int BasePower;
        public readonly ElementId Element;
        public readonly bool IsSkill;
        public readonly bool IgnoresArmor;
        public readonly GridCoord HitFrom;

        public DamageRequest(UnitId source, UnitId target, int basePower, ElementId element,
            bool isSkill = false, bool ignoresArmor = false, GridCoord hitFrom = default)
        {
            Source = source;
            Target = target;
            BasePower = basePower;
            Element = element;
            IsSkill = isSkill;
            IgnoresArmor = ignoresArmor;
            HitFrom = hitFrom;
        }
    }
}
