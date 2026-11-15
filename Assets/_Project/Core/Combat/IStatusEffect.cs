namespace BTH.Core.Combat
{
    /// <summary>
    /// Một trạng thái gắn lên unit. Duration đếm theo lượt của chủ thể;
    /// StatusSet quản lý đếm lượt, effect chỉ mô tả hệ số và phản ứng.
    /// </summary>
    public interface IStatusEffect
    {
        StatusId Id { get; }

        /// <summary>Thời gian sống ban đầu, tính bằng lượt của chủ thể.</summary>
        int Duration { get; }

        /// <summary>Hệ số sát thương phải nhận khi đang có trạng thái (1 = bình thường).</summary>
        float DamageTakenMultiplier { get; }

        /// <summary>Chặn hành động (Stagger mất lượt).</summary>
        bool PreventsAction { get; }

        /// <summary>Chặn di chuyển (Root).</summary>
        bool PreventsMovement { get; }

        void OnApply(StatusContext ctx);

        void OnTurnStart(StatusContext ctx);

        /// <summary>Chạy trước khi Duration bị trừ, tại EndTurn của chủ thể.</summary>
        void OnTurnEnd(StatusContext ctx);

        void OnRemoved(StatusContext ctx);

        /// <summary>Chạy sau khi sát thương đã áp lên HP.</summary>
        void OnDamaged(StatusContext ctx, DamageResult result);
    }
}
