using System;
using System.Collections.Generic;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Bảng cộng điểm Clash: ưu tiên cặp chiêu cụ thể, sau đó cặp hệ, cuối cùng là
    /// defaultBonus (+3). Không RNG — Predict và Resolve dùng chung một đường tính.
    /// </summary>
    public sealed class ClashRuleTable
    {
        private readonly Dictionary<string, int> _pairBonuses =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _elementBonuses =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public int DefaultBonus { get; set; } = 3;

        public void Add(ClashRule rule)
        {
            if (string.IsNullOrEmpty(rule.PlayerSkillId) || string.IsNullOrEmpty(rule.BossSkillId))
            {
                return;
            }

            _pairBonuses[PairKey(rule.PlayerSkillId, rule.BossSkillId)] = rule.Bonus;
        }

        public void AddElement(ElementId playerElement, ElementId bossElement, int bonus)
        {
            _elementBonuses[ElementKey(playerElement, bossElement)] = bonus;
        }

        public int GetBonus(string playerSkillId, string bossSkillId, ElementId playerElement,
            ElementId bossElement)
        {
            if (!string.IsNullOrEmpty(playerSkillId) && !string.IsNullOrEmpty(bossSkillId) &&
                _pairBonuses.TryGetValue(PairKey(playerSkillId, bossSkillId), out var pairBonus))
            {
                return pairBonus;
            }

            if (_elementBonuses.TryGetValue(ElementKey(playerElement, bossElement), out var elementBonus))
            {
                return elementBonus;
            }

            return DefaultBonus;
        }

        private static string PairKey(string playerSkillId, string bossSkillId)
        {
            return playerSkillId + "|" + bossSkillId;
        }

        private static string ElementKey(ElementId playerElement, ElementId bossElement)
        {
            return playerElement.Value + "|" + bossElement.Value;
        }
    }
}
