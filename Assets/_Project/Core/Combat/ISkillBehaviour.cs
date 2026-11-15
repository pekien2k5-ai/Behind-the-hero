namespace BTH.Core.Combat
{
    /// <summary>
    /// Logic riêng của từng chiêu: dựng danh sách effect dựa trên ngữ cảnh.
    /// Content cài đặt; Core chỉ gọi.
    /// </summary>
    public interface ISkillBehaviour
    {
        void BuildEffects(EffectContext ctx);
    }
}
