using System.Collections.Generic;
using BTH.Content.Duels;
using BTH.Content.Stages;
using BTH.Content.Terrain;
using BTH.Content.Units;
using BTH.Core.Combat;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Content
{
    /// <summary>
    /// Quản lý High Concept, Game Flow, Ma trận cân bằng Đơn vị và Hệ thuộc tính.
    /// </summary>
    public sealed class UnitMatrixEntry
    {
        public string UnitId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Mov { get; set; }
        public string RangeText { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
    }

    public static class GameMatrixCatalog
    {
        public const string HighConcept =
            "Từ kẻ lang thang bị nguyền rủa tới hành trình cùng dũng sĩ tiêu diệt quỷ vương, và bi kịch khi người hùng sa ngã.";

        public static IGameRules CreateBalancedRules() => new GameRules
        {
            ApPerTurn = 2,
            MovePoints = 2,
            TurnLimit = 25,
            Phase2Threshold = 0.70f,
            GuardMultiplier = 0.20f,
            StaggerMultiplier = 2.00f,
            FlankMultiplier = 1.50f,
            PoisonPerTurn = 3
        };

        public static ElementMatrix CreateElementMatrix() => ElementMatrix.CreateDefault();

        /// <summary>Bảng Ma trận cân bằng Đơn vị (GDD Phần 7.1).</summary>
        public static IReadOnlyList<UnitMatrixEntry> GetUnitMatrix() => new List<UnitMatrixEntry>
        {
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.WandererId, DisplayName = "Kẻ Lang Thang", Role = "Anh hùng (Player)",
                Hp = 12, Atk = 4, Mov = 2, RangeText = "1 ô",
                Strengths = "Tua ngược thời gian, kiếm hoàn mỹ, 2 kỹ năng phòng thủ (Protect, Dodge)",
                Weaknesses = "Lượng máu thấp (12 HP), đòi hỏi tính toán 2 AP chính xác"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.EosphorosId, DisplayName = "Dũng Giả Eosphoros", Role = "Anh hùng / Boss Cuối",
                Hp = 30, Atk = 6, Mov = 2, RangeText = "1-2 ô",
                Strengths = "Giáp dày, sát thương Thánh quang diện rộng, Khiên Thánh",
                Weaknesses = "Tốc độ chậm, dễ bị đỡ gạt bởi kiếm kỹ hoàn mỹ"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.ImpId, DisplayName = "Tiểu Quỷ (Imp)", Role = "Lính quái cận chiến",
                Hp = 6, Atk = 2, Mov = 3, RangeText = "1 ô",
                Strengths = "Cơ động cao (3 ô/lượt)",
                Weaknesses = "Máu thấp (6 HP), tiêu diệt nhanh"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.GargoyleId, DisplayName = "Quái Thạch (Gargoyle)", Role = "Lính tinh anh tầm xa",
                Hp = 10, Atk = 3, Mov = 2, RangeText = "2-3 ô",
                Strengths = "Bay qua vực thẳm và bẫy địa hình, bắn tỉa từ xa",
                Weaknesses = "Sát thương cận chiến thấp"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.AshenExecutionerId, DisplayName = "Đao Phủ Tro Tàn", Role = "Boss Ải 1 (Solo)",
                Hp = 20, Atk = 5, Mov = 1, RangeText = "1-2 ô",
                Strengths = "Bổ rìu Earth Crack làm nứt sàn đá, Tro Tàn Cuồng Nộ uy lực 15",
                Weaknesses = "Khắc chế bởi Protect / Blade Guard và Swift Slash -> Stagger"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.PlagueWitchId, DisplayName = "Vu Nữ Morbida", Role = "Boss Ải 2 (Solo)",
                Hp = 18, Atk = 4, Mov = 2, RangeText = "1-3 ô",
                Strengths = "Phun sương độc và Grave Hands trói chân (Root), Bão Khí Độc uy lực 14",
                Weaknesses = "Khắc chế bởi Necrotic Ward / Necrotic Burst -> Tự trúng độc 3 HP/lượt"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.AbyssKnightId, DisplayName = "Kỵ Sĩ Hư Không", Role = "Boss Ải 3 (Solo)",
                Hp = 25, Atk = 6, Mov = 1, RangeText = "1-3 ô",
                Strengths = "Khiên Mắt Quỷ và Tia Thương Hư Không xuyên thấu uy lực 16",
                Weaknesses = "Khắc chế bởi Dodge / Temporal Step và Swift Slash -> Flanked (×1.5)"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.DemonKingMalkorId, DisplayName = "Quỷ Vương Malkor", Role = "Đại Trùm Ải 4 (Solo 2×2)",
                Hp = 45, Atk = 8, Mov = 1, RangeText = "Toàn sân",
                Strengths = "HP khổng lồ 45, Cự Kiếm Hỏa Ngục uy lực 18, Thiên thạch nổ sập sàn",
                Weaknesses = "Kích thước 2×2 dễ trúng đòn, bị Temporal Step / Dodge khắc chế -> 3 đòn kết liễu"
            },
            new UnitMatrixEntry
            {
                UnitId = UnitCatalog.FallenEosphorosId, DisplayName = "Eosphoros Sa Ngã", Role = "Boss Cuối Ải 5 (Solo)",
                Hp = 30, Atk = 6, Mov = 2, RangeText = "1-3 ô",
                Strengths = "Cột sáng thiên phạt và Thánh Quang Trảm Hoàn Mỹ uy lực 18",
                Weaknesses = "Khắc chế bởi Swift Slash và Protect / Blade Guard -> Mở 2 Ending A/B"
            }
        };
    }
}
