using System.Collections.Generic;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Phiên Duel sau khi boss xuống dưới ngưỡng 70%: chỉ đấu chiêu, không bàn cờ,
    /// không AP, không End Turn. Xem Docs/QUYET_DINH.md.
    /// </summary>
    public interface IClashDuel
    {
        DuelState State { get; }

        DuelDefinition Definition { get; }

        bool IsActive { get; }

        void Start(GameState state, DuelDefinition definition, UnitState player, UnitState boss);

        IReadOnlyList<ClashOption> GetOptions();

        /// <summary>Người chơi chọn chiêu; ném nếu sai lượt/sai chiêu.</summary>
        ClashRoundResult Choose(string playerSkillId);

        /// <summary>Đánh kết liễu khi boss đang choáng; false nếu chưa tới lượt kết liễu.</summary>
        bool TryFinisher(out DamageResult damage);

        DuelSnapshot Capture();

        void Restore(DuelSnapshot snapshot);

        /// <summary>Tắt Duel khi rewind về snapshot trước khi Duel bắt đầu.</summary>
        void Clear();
    }
}
