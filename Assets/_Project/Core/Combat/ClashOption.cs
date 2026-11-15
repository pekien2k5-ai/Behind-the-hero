namespace BTH.Core.Combat
{
    /// <summary>Một lựa chọn chiêu trong màn Clash, kèm dự đoán để UI tô badge.</summary>
    public sealed class ClashOption
    {
        public ClashOption(string skillId, string displayName, int power, ClashPrediction prediction)
        {
            SkillId = skillId;
            DisplayName = displayName;
            Power = power;
            Prediction = prediction;
        }

        public string SkillId { get; }

        public string DisplayName { get; }

        public int Power { get; }

        public ClashPrediction Prediction { get; }
    }
}
