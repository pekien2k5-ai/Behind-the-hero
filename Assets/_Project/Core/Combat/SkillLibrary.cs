using System;
using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>Kho chiêu mặc định, tra cứu Ordinal để tất định giữa các máy.</summary>
    public sealed class SkillLibrary : ISkillLibrary
    {
        private readonly Dictionary<string, SkillDefinition> _skills =
            new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
        private readonly List<SkillDefinition> _ordered = new List<SkillDefinition>();

        public IReadOnlyList<SkillDefinition> All => _ordered;

        public void Add(SkillDefinition skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.Id))
            {
                return;
            }

            if (_skills.ContainsKey(skill.Id))
            {
                _ordered.Remove(_skills[skill.Id]);
            }

            _skills[skill.Id] = skill;
            _ordered.Add(skill);
        }

        public bool TryGet(string id, out SkillDefinition skill)
        {
            if (string.IsNullOrEmpty(id))
            {
                skill = null;
                return false;
            }

            return _skills.TryGetValue(id, out skill);
        }

        public SkillDefinition Get(string id)
        {
            if (!TryGet(id, out var skill))
            {
                throw new KeyNotFoundException("Không có chiêu " + id);
            }

            return skill;
        }
    }
}
