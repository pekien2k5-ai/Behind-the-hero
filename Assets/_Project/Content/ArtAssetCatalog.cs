using System.Collections.Generic;
using BTH.Content.Terrain;
using BTH.Content.Units;
using BTH.Core.Grid;

namespace BTH.Content
{
    public sealed class SpritesheetSpec
    {
        public string UnitId { get; set; } = string.Empty;
        public string ResourcePath { get; set; } = string.Empty;
        public int SheetWidth { get; set; } = 512;
        public int SheetHeight { get; set; } = 512;
        public int FrameSize { get; set; } = 128;
        public int FramesPerState { get; set; } = 4;
        public IReadOnlyList<string> States { get; set; } = new[] { "Idle", "Walk", "Attack", "Death" };
    }

    public sealed class VfxSheetSpec
    {
        public string VfxId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ResourcePath { get; set; } = string.Empty;
        public int FrameCount { get; set; } = 4;
        public int FrameSize { get; set; } = 128;
    }

    public sealed class HudColorSpec
    {
        public string Name { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public static class ArtAssetCatalog
    {
        public static readonly string[] AnimationStates = { "Idle", "Walk", "Attack", "Death" };

        public static IReadOnlyList<HudColorSpec> GetHudColorPalette() => new List<HudColorSpec>
        {
            new HudColorSpec { Name = "Holy Gold", Hex = "#E5C158", Role = "Sắc vàng hoàng kim Thần điện, viền khung Portrait & Tiêu đề HUD" },
            new HudColorSpec { Name = "Royal Deep Blue", Hex = "#2C4270", Role = "Xanh thẫm vương giả, viền ô cờ chiến thuật & Thanh năng lượng MP" },
            new HudColorSpec { Name = "Necrotic Temporal Purple", Hex = "#6B3BA7", Role = "Sắc tím đen hoại tử / lời nguyền thời gian, nút lệnh đang kích hoạt" },
            new HudColorSpec { Name = "Ashen Crimson", Hex = "#8B1E1E", Role = "Đỏ quạch tro tàn, thanh máu HP pips & vùng nguy hiểm Telegraph" },
            new HudColorSpec { Name = "Dungeon Slate", Hex = "#3A3E45", Role = "Xám ngục tối, nền bảng điều khiển HUD & khung thẻ Clash" }
        };

        public static IReadOnlyList<SpritesheetSpec> GetSpritesheetSpecs()
        {
            var list = new List<SpritesheetSpec>();
            foreach (var u in UnitCatalog.All())
            {
                list.Add(new SpritesheetSpec
                {
                    UnitId = u.Id,
                    ResourcePath = "Art/Spritesheets/" + u.Id,
                    SheetWidth = 512,
                    SheetHeight = 512,
                    FrameSize = 128,
                    FramesPerState = 4,
                    States = AnimationStates
                });
            }
            return list;
        }

        public static IReadOnlyList<VfxSheetSpec> GetVfxSpecs() => new List<VfxSheetSpec>
        {
            new VfxSheetSpec { VfxId = "vfx_time_rewind", DisplayName = "Đồng Hồ Tua Ngược (Time Rewind)", ResourcePath = "Art/VFX/vfx_time_rewind" },
            new VfxSheetSpec { VfxId = "vfx_swift_slash", DisplayName = "Đoản Kiếm Hoàn Mỹ (Swift Slash)", ResourcePath = "Art/VFX/vfx_swift_slash" },
            new VfxSheetSpec { VfxId = "vfx_necrotic_burst", DisplayName = "Bùng Nổ Hoại Tử (Necrotic Burst)", ResourcePath = "Art/VFX/vfx_necrotic_burst" },
            new VfxSheetSpec { VfxId = "vfx_divine_barrier", DisplayName = "Khiên Thánh Chữ Thập (Divine Barrier)", ResourcePath = "Art/VFX/vfx_divine_barrier" },
            new VfxSheetSpec { VfxId = "vfx_holy_pillar", DisplayName = "Cột Sáng Thiên Phạt (Holy Pillar)", ResourcePath = "Art/VFX/vfx_holy_pillar" },
            new VfxSheetSpec { VfxId = "vfx_holy_slash", DisplayName = "Thánh Quang Trảm (Holy Sword Slash)", ResourcePath = "Art/VFX/vfx_holy_slash" },
            new VfxSheetSpec { VfxId = "vfx_hellfire_wave", DisplayName = "Sóng Chém Hỏa Ngục (Hellfire Wave)", ResourcePath = "Art/VFX/vfx_hellfire_wave" },
            new VfxSheetSpec { VfxId = "vfx_brimstone_meteor", DisplayName = "Thiên Thạch Dung Nham (Brimstone Meteor)", ResourcePath = "Art/VFX/vfx_brimstone_meteor" },
            new VfxSheetSpec { VfxId = "vfx_dark_pillar", DisplayName = "Cột Lửa Quỷ Tím (Dark Violet Pillar)", ResourcePath = "Art/VFX/vfx_dark_pillar" },
            new VfxSheetSpec { VfxId = "vfx_earth_crack", DisplayName = "Bổ Rìu Chấn Động (Earth Crack)", ResourcePath = "Art/VFX/vfx_earth_crack" },
            new VfxSheetSpec { VfxId = "vfx_toxic_miasma", DisplayName = "Sương Độc & Tay Xác Sống (Toxic Miasma)", ResourcePath = "Art/VFX/vfx_toxic_miasma" },
            new VfxSheetSpec { VfxId = "vfx_void_spear", DisplayName = "Tia Thương Hư Không (Void Spear Beam)", ResourcePath = "Art/VFX/vfx_void_spear" }
        };

        public static IReadOnlyList<string> GetPortraitIds() => new[]
        {
            UnitCatalog.WandererId,
            UnitCatalog.EosphorosId,
            UnitCatalog.AshenExecutionerId,
            UnitCatalog.PlagueWitchId,
            UnitCatalog.AbyssKnightId,
            UnitCatalog.DemonKingMalkorId,
            UnitCatalog.FallenEosphorosId
        };

        public static IReadOnlyList<TerrainId> GetRequiredTerrainTypes() => new[]
        {
            TerrainId.Floor,
            TerrainId.Wall,
            TerrainId.Pit,
            TerrainId.Cracked,
            TerrainId.Poison,
            TerrainCatalog.SpikeTrap,
            TerrainId.Lava,
            TerrainId.Chest
        };
    }
}
