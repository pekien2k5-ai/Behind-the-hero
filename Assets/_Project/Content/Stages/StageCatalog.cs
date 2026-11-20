using System.Collections.Generic;
using BTH.Content.Duels;
using BTH.Content.Terrain;
using BTH.Content.Units;
using BTH.Core.Grid;
using BTH.Core.Run;

namespace BTH.Content.Stages
{
    /// <summary>
    /// Thiết kế bố cục và cơ chế câu đố của 5 ải màn chơi (CV3 - Level & Puzzle Design).
    /// </summary>
    public static class StageCatalog
    {
        public static StageDefinition CreateStage1()
        {
            var stage = new StageDefinition
            {
                Id = "stage-1",
                DisplayName = "Ải 1: Tàn Tích Oakhaven (8×8)",
                Width = 8,
                Height = 8,
                TurnLimit = 22,
                Player = UnitCatalog.CreateWanderer(),
                PlayerSpawn = new GridCoord(2, 2),
                BossId = UnitCatalog.AshenExecutionerId,
                Clash = DuelCatalog.CreateClashStage1(),
                Duel = DuelCatalog.CreateDuelStage1()
            };

            stage.Spawns.Add(new UnitSpawnDefinition(UnitCatalog.CreateAshenExecutioner(), new GridCoord(4, 2)));

            // Bố cục câu đố Ải 1: Trụ đá đổ nát chắn góc, cầu đá nứt ở trung tâm, vực thẳm và rương di vật
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(6, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 1, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(6, 1, TerrainId.Wall));

            // Sàn nứt (sập sau 3 lượt) & hố chông vực thẳm hai bên sườn
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 4, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 4, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(0, 3, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 3, TerrainId.Pit));

            // Rương kho báu (+4 HP khi bước vào) & Bẫy chông sắt
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 6, TerrainId.Chest));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 4, TerrainCatalog.SpikeTrap));

            return stage;
        }

        public static StageDefinition CreateStage2()
        {
            var stage = new StageDefinition
            {
                Id = "stage-2",
                DisplayName = "Ải 2: Hầm Mộ Độc Khí (9×9)",
                Width = 9,
                Height = 9,
                TurnLimit = 24,
                Player = UnitCatalog.CreateWanderer(),
                PlayerSpawn = new GridCoord(2, 3),
                BossId = UnitCatalog.PlagueWitchId,
                Clash = DuelCatalog.CreateClashStage2(),
                Duel = DuelCatalog.CreateDuelStage2()
            };

            stage.Spawns.Add(new UnitSpawnDefinition(UnitCatalog.CreatePlagueWitch(), new GridCoord(5, 3)));

            // Bố cục câu đố Ải 2: Hành lang đá nổi giữa đầm lầy độc (Poison 3 HP/lượt) và tường mộ cổ chắn LOS
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 1, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 1, TerrainId.Wall));

            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 4, TerrainId.Poison));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(2, 5, TerrainId.Poison));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 4, TerrainId.Poison));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(6, 4, TerrainId.Poison));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 3, TerrainId.Poison));

            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 2, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 2, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 7, TerrainId.Chest));

            return stage;
        }

        public static StageDefinition CreateStage3()
        {
            var stage = new StageDefinition
            {
                Id = "stage-3",
                DisplayName = "Ải 3: Hành Lang Hư Không (9×9)",
                Width = 9,
                Height = 9,
                TurnLimit = 24,
                Player = UnitCatalog.CreateWanderer(),
                PlayerSpawn = new GridCoord(2, 4),
                BossId = UnitCatalog.AbyssKnightId,
                Clash = DuelCatalog.CreateClashStage3(),
                Duel = DuelCatalog.CreateDuelStage3()
            };

            stage.Spawns.Add(new UnitSpawnDefinition(UnitCatalog.CreateAbyssKnight(), new GridCoord(5, 4)));

            // Bố cục câu đố Ải 3: Cầu đá hẹp bắc qua Vực Thẳm (Pit) + Trụ đá chắn tia thương xuyên thấu (Void Spear)
            stage.Terrains.Add(TerrainCatalog.CreateOverride(0, 2, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 2, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 2, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(8, 2, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(0, 6, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 6, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 6, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(8, 6, TerrainId.Pit));

            // Trụ đá che LOS để né đòn xuyên hàng của Kỵ Sĩ Hư Không
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 3, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(6, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(6, 3, TerrainId.Wall));

            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 4, TerrainId.Cracked, collapseTurns: 4));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 2, TerrainCatalog.SpikeTrap));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 7, TerrainId.Chest));

            return stage;
        }

        public static StageDefinition CreateStage4()
        {
            var stage = new StageDefinition
            {
                Id = "stage-4",
                DisplayName = "Ải 4: Đấu Trường Hỏa Ngục (10×10)",
                Width = 10,
                Height = 10,
                TurnLimit = 25,
                Player = UnitCatalog.CreateWanderer(),
                PlayerSpawn = new GridCoord(2, 4),
                BossId = UnitCatalog.DemonKingMalkorId,
                Clash = DuelCatalog.CreateClashStage4(),
                Duel = DuelCatalog.CreateDuelStage4()
            };

            // Boss Malkor có kích thước 2x2 (chiếm (5,4), (6,4), (5,5), (6,5))
            stage.Spawns.Add(new UnitSpawnDefinition(UnitCatalog.CreateDemonKingMalkor(), new GridCoord(5, 4)));

            // Bố cục câu đố Ải 4: Vòng vây Dung Nham (Lava 4 HP/lượt) và sàn nứt quanh Quỷ Vương 2x2
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 1, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(2, 1, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 1, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(8, 1, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(1, 8, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(2, 8, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 8, TerrainId.Lava));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(8, 8, TerrainId.Lava));

            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 6, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 2, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 6, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 3, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(0, 4, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(9, 4, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 8, TerrainId.Chest));

            return stage;
        }

        public static StageDefinition CreateStage5()
        {
            var stage = new StageDefinition
            {
                Id = "stage-5",
                DisplayName = "Ải 5: Mật Thất Thánh Điện (8×8)",
                Width = 8,
                Height = 8,
                TurnLimit = 25,
                Player = UnitCatalog.CreateWanderer(),
                PlayerSpawn = new GridCoord(2, 3),
                BossId = UnitCatalog.FallenEosphorosId,
                Clash = DuelCatalog.CreateClashStage5(),
                Duel = DuelCatalog.CreateDuelStage5()
            };

            stage.Spawns.Add(new UnitSpawnDefinition(UnitCatalog.CreateFallenEosphoros(), new GridCoord(5, 3)));

            // Bố cục câu đố Ải 5: Thánh đường đối xứng với 4 trụ thánh quang, vực thẳm ký ức và rương di vật
            stage.Terrains.Add(TerrainCatalog.CreateOverride(2, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 5, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(2, 1, TerrainId.Wall));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(5, 1, TerrainId.Wall));

            stage.Terrains.Add(TerrainCatalog.CreateOverride(0, 3, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(7, 3, TerrainId.Pit));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 4, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 2, TerrainId.Cracked, collapseTurns: 3));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(3, 6, TerrainId.Chest));
            stage.Terrains.Add(TerrainCatalog.CreateOverride(4, 6, TerrainId.Chest));

            return stage;
        }

        public static IReadOnlyList<StageDefinition> All() => new List<StageDefinition>
        {
            CreateStage1(),
            CreateStage2(),
            CreateStage3(),
            CreateStage4(),
            CreateStage5()
        };

        public static string GetPuzzleDescription(string stageId)
        {
            switch (stageId)
            {
                case "stage-1":
                    return "Câu đố Ải 1: Tránh đứng trên ô Sàn Nứt (sập sau 3 lượt), dụ Đao Phủ bổ Earth Crack vào khoảng trống; dùng Swift Slash hoặc Blade Guard trong Duel để làm choáng (Stagger ×2).";
                case "stage-2":
                    return "Câu đố Ải 2: Đi trên các dải đá khô giữa Đầm Lầy Độc (3 HP/lượt), nấp sau Trụ Đá chặn LOS; dùng Necrotic Ward / Necrotic Burst để phản độc ngược lên Vu Nữ.";
                case "stage-3":
                    return "Câu đố Ải 3: Cầu đá hẹp giữa Vực Thẳm — dùng Trụ Đá chắn tia Void Spear xuyên hàng và dùng Temporal Step dịch chuyển qua hố, mở trạng thái Flanked (×1.5 sát thương).";
                case "stage-4":
                    return "Câu đố Ải 4: Quỷ Vương Malkor chiếm 2×2 ô giữa hồ Dung Nham (4 HP/lượt); giữ khoảng cách ngoài biên footprint 2×2 và khắc chế bằng Temporal Step để tung 3 đòn kết liễu liên hoàn.";
                case "stage-5":
                    return "Câu đố Ải 5: Đối xứng Thánh Điện — né vùng chữ thập Holy Pillar quanh 4 trụ đá, đánh bại Eosphoros trong Duel để mở khóa lựa chọn 2 Kết Cục (Ending A / Ending B).";
                default:
                    return string.Empty;
            }
        }
    }
}
