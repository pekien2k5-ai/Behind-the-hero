using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>Kho chiêu của màn. Lookup theo chuỗi để Core không phụ thuộc enum.</summary>
    public interface ISkillLibrary
    {
        IReadOnlyList<SkillDefinition> All { get; }

        bool TryGet(string id, out SkillDefinition skill);

        SkillDefinition Get(string id);
    }
}
