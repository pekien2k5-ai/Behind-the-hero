namespace BTH.Core.Combat
{
    /// <summary>
    /// Logic riêng của từng chiêu: dựng danh sách effect dựa trên ngữ cảnh.
    /// Content cài đặt; Core chỉ gọi.
    /// </summary>
    public interface ISkillBehaviour
    {
        /// <summary>Uy lực mặc định để SkillCatalog chép vào SkillDefinition.BoardPower.</summary>
        int DefaultPower { get; }

        void BuildEffects(EffectContext ctx);
    }
}
