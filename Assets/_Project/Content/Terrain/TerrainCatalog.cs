using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Content.Terrain
{
    /// <summary>
    /// Hợp đồng hành vi địa hình mở rộng ở lớp Content (không sửa Core).
    /// Cho phép ô Rương hồi máu khi bước vào, ô Gạch Nứt kích hoạt sập khi bước qua, v.v.
    /// </summary>
    public interface ITerrainBehaviour
    {
        TerrainId Id { get; }
        void OnUnitEntered(UnitState unit, GridCoord coord, GameState state);
    }

    public sealed class StandardTerrainBehaviour : ITerrainBehaviour
    {
        public TerrainId Id { get; }
        public StandardTerrainBehaviour(TerrainId id) { Id = id; }
        public void OnUnitEntered(UnitState unit, GridCoord coord, GameState state) { }
    }

    /// <summary>
    /// Ô Gạch nứt vỡ (GDD §3.2.2): Sau khi có đơn vị bước vào, ô sẽ kích hoạt đếm ngược sập thành Vực thẳm ở lượt kế.
    /// </summary>
    public sealed class CrackedTerrainBehaviour : ITerrainBehaviour
    {
        public TerrainId Id => TerrainId.Cracked;

        public void OnUnitEntered(UnitState unit, GridCoord coord, GameState state)
        {
            if (state?.Grid == null || !state.Grid.InBounds(coord)) return;
            var cell = state.Grid.Get(coord);
            if (cell.TurnsUntilCollapse <= 0 || cell.TurnsUntilCollapse > 1)
            {
                state.Grid.ArmCollapse(coord, 1);
            }
        }
    }

    public readonly struct ChestOpenedEvent
    {
        public readonly GridCoord Coord;
        public readonly int HpRestored;

        public ChestOpenedEvent(GridCoord coord, int hpRestored)
        {
            Coord = coord;
            HpRestored = hpRestored;
        }
    }

    /// <summary>Ô Rương kho báu hồi phục tối đa 4 HP khi người chơi bước vào.</summary>
    public sealed class ChestTerrainBehaviour : ITerrainBehaviour
    {
        public const int HealAmount = 4;
        public TerrainId Id => TerrainId.Chest;

        public void OnUnitEntered(UnitState unit, GridCoord coord, GameState state)
        {
            if (unit == null || !unit.IsAlive || unit.Faction != UnitFaction.Player || state == null) return;

            int hpBefore = unit.Hp;
            var queue = new EffectQueue();
            queue.Enqueue(new HealEffect(unit.Id, HealAmount));
            var ctx = new EffectContext(state, queue, unit.Id, coord, unit.Id, coord, null, HealAmount, ElementId.Holy);
            queue.Run(ctx);

            // Mở rương xong chuyển ô về Sàn đá thường
            state.Grid.SetTerrain(coord, TerrainId.Floor, TerrainFlags.None, 1, 0);
            state.Knowledge?.Increment("chests-opened", 1);
            state.Events?.Publish(new ChestOpenedEvent(coord, unit.Hp - hpBefore));
        }
    }

    /// <summary>
    /// Đặc tả một loại ô địa hình trong bộ Tileset bàn cờ (CV3 - Xây dựng bộ Tileset địa hình ô cờ).
    /// </summary>
    public sealed class TerrainSpec
    {
        public TerrainId Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string TileSpriteName { get; set; } = string.Empty;
        public TerrainFlags Flags { get; set; }
        public int MovementCost { get; set; } = 1;
        public int HazardDamagePerTurn { get; set; }
        public int DefaultCollapseTurns { get; set; }
        public string TacticalDescription { get; set; } = string.Empty;
        public ITerrainBehaviour Behaviour { get; set; }
    }

    public static class TerrainCatalog
    {
        public static readonly TerrainId SpikeTrap = new TerrainId("spike-trap");

        private static readonly Dictionary<string, TerrainSpec> Specs = new Dictionary<string, TerrainSpec>(System.StringComparer.OrdinalIgnoreCase)
        {
            [TerrainId.Floor.Value] = new TerrainSpec
            {
                Id = TerrainId.Floor,
                DisplayName = "Ô Sàn Đá (Normal Tile)",
                TileSpriteName = "terrain_floor",
                Flags = TerrainFlags.None,
                MovementCost = 1,
                HazardDamagePerTurn = 0,
                TacticalDescription = "Gạch lát đá phẳng nguyên khối. Tốc độ di chuyển tiêu chuẩn 1 ô; không có hiệu ứng bất lợi.",
                Behaviour = new StandardTerrainBehaviour(TerrainId.Floor)
            },
            [TerrainId.Wall.Value] = new TerrainSpec
            {
                Id = TerrainId.Wall,
                DisplayName = "Tường Ngục & Trụ Đá (Dungeon Wall)",
                TileSpriteName = "terrain_wall",
                Flags = TerrainFlags.BlocksMove | TerrainFlags.BlocksLineOfSight | TerrainFlags.IsCover,
                MovementCost = 99,
                HazardDamagePerTurn = 0,
                TacticalDescription = "Chướng ngại vật chặn di chuyển và che khuất tầm nhìn (LOS) để né đòn tầm xa của Boss.",
                Behaviour = new StandardTerrainBehaviour(TerrainId.Wall)
            },
            [TerrainId.Pit.Value] = new TerrainSpec
            {
                Id = TerrainId.Pit,
                DisplayName = "Ô Vực Thẳm (Abyss Pit)",
                TileSpriteName = "terrain_pit",
                Flags = TerrainFlags.BlocksMove,
                MovementCost = 99,
                HazardDamagePerTurn = 0,
                TacticalDescription = "Hố sâu vô tận chặn đơn vị đi bộ; chỉ Quái Thạch có cánh bay hoặc kỹ năng dịch chuyển mới vượt qua.",
                Behaviour = new StandardTerrainBehaviour(TerrainId.Pit)
            },
            [TerrainId.Cracked.Value] = new TerrainSpec
            {
                Id = TerrainId.Cracked,
                DisplayName = "Ô Gạch Nứt Vỡ (Cracked Tile)",
                TileSpriteName = "terrain_cracked",
                Flags = TerrainFlags.None,
                MovementCost = 1,
                HazardDamagePerTurn = 0,
                DefaultCollapseTurns = 2,
                TacticalDescription = "Gạch nứt toác, sụt lở. Sau khi bước qua hoặc hết đếm ngược sẽ sập thành Vực thẳm (Pit).",
                Behaviour = new CrackedTerrainBehaviour()
            },
            [TerrainId.Poison.Value] = new TerrainSpec
            {
                Id = TerrainId.Poison,
                DisplayName = "Ô Sương Độc (Poison Miasma)",
                TileSpriteName = "terrain_poison",
                Flags = TerrainFlags.IsHazard,
                MovementCost = 2,
                HazardDamagePerTurn = 3,
                TacticalDescription = "Khí xanh ngọc sủi bọt từ bùn lầy. Làm chậm di chuyển và gây 3 sát thương độc cuối mỗi lượt.",
                Behaviour = new StandardTerrainBehaviour(TerrainId.Poison)
            },
            [SpikeTrap.Value] = new TerrainSpec
            {
                Id = SpikeTrap,
                DisplayName = "Ô Bẫy Chông Sắt (Spike Trap)",
                TileSpriteName = "trap_spikes",
                Flags = TerrainFlags.IsHazard,
                MovementCost = 1,
                HazardDamagePerTurn = 2,
                TacticalDescription = "Lỗ chông sắt ngầm dưới khe đá. Đơn vị giẫm lên và dừng chân trên bẫy chông mất 2 HP.",
                Behaviour = new StandardTerrainBehaviour(SpikeTrap)
            },
            [TerrainId.Lava.Value] = new TerrainSpec
            {
                Id = TerrainId.Lava,
                DisplayName = "Ô Nham Thạch Hỏa Ngục (Hellfire Lava)",
                TileSpriteName = "terrain_lava",
                Flags = TerrainFlags.IsHazard,
                MovementCost = 2,
                HazardDamagePerTurn = 4,
                TacticalDescription = "Dung nham đỏ rực sôi sục tại Vương Tọa Quỷ Vương, gây 4 sát thương chuẩn mỗi lượt.",
                Behaviour = new StandardTerrainBehaviour(TerrainId.Lava)
            },
            [TerrainId.Chest.Value] = new TerrainSpec
            {
                Id = TerrainId.Chest,
                DisplayName = "Ô Rương Kho Báu (Chest Tile)",
                TileSpriteName = "terrain_chest",
                Flags = TerrainFlags.IsInteractive,
                MovementCost = 1,
                HazardDamagePerTurn = 0,
                TacticalDescription = "Rương cổ thánh điện: Bước vào ô này sẽ mở rương và hồi phục tức thời +4 HP!",
                Behaviour = new ChestTerrainBehaviour()
            }
        };

        public static IReadOnlyDictionary<string, TerrainSpec> All => Specs;

        public static TerrainSpec Get(TerrainId id)
        {
            if (Specs.TryGetValue(id.Value ?? string.Empty, out var spec))
                return spec;
            return Specs[TerrainId.Floor.Value];
        }

        public static void HandleUnitEnteredCell(UnitState unit, GridCoord coord, GameState state)
        {
            if (unit == null || state?.Grid == null || !state.Grid.InBounds(coord)) return;
            var cell = state.Grid.Get(coord);
            var spec = Get(cell.Terrain);
            spec.Behaviour?.OnUnitEntered(unit, coord, state);
        }

        public static TerrainOverride CreateOverride(int x, int y, TerrainId id, int collapseTurns = 0, int? customHazard = null)
        {
            var spec = Get(id);
            int collapse = collapseTurns > 0 ? collapseTurns : spec.DefaultCollapseTurns;
            int hazard = customHazard ?? spec.HazardDamagePerTurn;
            return new TerrainOverride(
                new GridCoord(x, y),
                spec.Id,
                spec.Flags,
                spec.MovementCost,
                hazard,
                collapse);
        }
    }
}
