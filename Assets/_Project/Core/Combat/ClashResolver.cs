using System;
using System.Collections.Generic;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>Cài đặt Clash mặc định: power chiêu + bonus theo bảng, so với power boss.</summary>
    public sealed class ClashResolver : IClashResolver
    {
        public ClashPrediction Predict(UnitState player, UnitState boss, SkillDefinition playerSkill,
            ClashDefinition clash)
        {
            if (playerSkill == null || clash == null)
            {
                return new ClashPrediction(ClashResult.None, 0, 0, 0);
            }

            int bonus = GetBonus(playerSkill, clash);
            int playerPower = EffectivePower(playerSkill, bonus);
            int bossPower = clash.BossPower;
            return new ClashPrediction(Compare(playerPower, bossPower), playerPower, bossPower, bonus);
        }

        public ClashOutcome Resolve(UnitState player, UnitState boss, SkillDefinition playerSkill,
            ClashDefinition clash)
        {
            var prediction = Predict(player, boss, playerSkill, clash);
            if (prediction.Result == ClashResult.None)
            {
                return new ClashOutcome(ClashResult.None, prediction.PlayerPower, prediction.BossPower,
                    null, false);
            }

            IStatusEffect effect = null;
            bool toBoss = false;
            if (prediction.Result == ClashResult.Win)
            {
                effect = clash.CreateWinEffect();
                toBoss = true;
            }
            else if (prediction.Result == ClashResult.Lose)
            {
                effect = clash.CreateLoseEffect();
            }

            return new ClashOutcome(prediction.Result, prediction.PlayerPower, prediction.BossPower,
                effect, toBoss);
        }

        public IReadOnlyList<ClashOption> GetOptions(UnitState player, UnitState boss,
            IReadOnlyList<string> skillIds, ISkillLibrary skills, ClashDefinition clash)
        {
            var options = new List<ClashOption>();
            if (skillIds == null || skills == null || clash == null)
            {
                return options;
            }

            for (int i = 0; i < skillIds.Count; i++)
            {
                if (!skills.TryGet(skillIds[i], out var skill) || !skill.IsClashSkill)
                {
                    continue;
                }

                options.Add(new ClashOption(skill.Id, skill.DisplayName, skill.Power,
                    Predict(player, boss, skill, clash)));
            }

            return options;
        }

        /// <summary>Dùng chung cho Predict và Resolve.</summary>
        public static int EffectivePower(SkillDefinition playerSkill, int bonus)
        {
            return playerSkill.Power + bonus;
        }

        private static int GetBonus(SkillDefinition playerSkill, ClashDefinition clash)
        {
            var table = clash.Rules ?? new ClashRuleTable();
            return table.GetBonus(playerSkill.Id, clash.BossSkillId, playerSkill.Element,
                clash.BossElement);
        }

        private static ClashResult Compare(int playerPower, int bossPower)
        {
            if (playerPower > bossPower)
            {
                return ClashResult.Win;
            }

            return playerPower == bossPower ? ClashResult.Draw : ClashResult.Lose;
        }
    }
}
