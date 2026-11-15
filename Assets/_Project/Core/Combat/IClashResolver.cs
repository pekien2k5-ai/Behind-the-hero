using System.Collections.Generic;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// So Clash giữa chiêu người chơi và chiêu boss. Không RNG; Predict và Resolve
    /// phải dùng chung EffectivePower để UI và kết quả không lệch nhau.
    /// </summary>
    public interface IClashResolver
    {
        ClashPrediction Predict(UnitState player, UnitState boss, SkillDefinition playerSkill,
            ClashDefinition clash);

        ClashOutcome Resolve(UnitState player, UnitState boss, SkillDefinition playerSkill,
            ClashDefinition clash);

        IReadOnlyList<ClashOption> GetOptions(UnitState player, UnitState boss,
            IReadOnlyList<string> skillIds, ISkillLibrary skills, ClashDefinition clash);
    }
}
