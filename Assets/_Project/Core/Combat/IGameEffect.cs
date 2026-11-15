namespace BTH.Core.Combat
{
    /// <summary>
    /// Một hiệu ứng rời rạc trong hàng đợi. Priority nhỏ chạy trước;
    /// cùng priority thì giữ thứ tự enqueue.
    /// </summary>
    public interface IGameEffect
    {
        int Priority { get; }

        void Resolve(EffectContext ctx);
    }
}
