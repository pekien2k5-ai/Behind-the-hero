using System.Collections.Generic;
using BTH.Core.Grid;
using UnityEngine;

namespace BTH.Game
{
    public enum UnitAnimState
    {
        Idle = 0,
        Walk = 1,
        Attack = 2,
        Death = 3
    }

    /// <summary>
    /// Quản lý và cắt tự động toàn bộ Spritesheets (Idle, Walk, Attack, Death), Tileset địa hình,
    /// hiệu ứng VFX kỹ năng, Portrait nhân vật và Background Duel từ Resources/Art/.
    /// </summary>
    public static class ArtCatalog
    {
        private static readonly Dictionary<string, Sprite[][]> UnitSheets = new Dictionary<string, Sprite[][]>();
        private static readonly Dictionary<string, Sprite> TileSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> VfxStrips = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Texture2D> Portraits = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> Backgrounds = new Dictionary<string, Texture2D>();
        private static Sprite _whitePixelSprite;
        private static Texture2D _whitePixelTex;
        private static Sprite _borderedTileSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            UnitSheets.Clear();
            TileSprites.Clear();
            VfxStrips.Clear();
            Portraits.Clear();
            Backgrounds.Clear();
        }

        public static Sprite BorderedTileSprite
        {
            get
            {
                if (_borderedTileSprite == null)
                {
                    const int sz = 64;
                    var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    var pixels = new Color[sz * sz];
                    for (int y = 0; y < sz; y++)
                    {
                        for (int x = 0; x < sz; x++)
                        {
                            bool border = x < 4 || x >= sz - 4 || y < 4 || y >= sz - 4;
                            pixels[y * sz + x] = border
                                ? new Color(1f, 1f, 1f, 0.95f)
                                : new Color(1f, 1f, 1f, 0.28f);
                        }
                    }
                    tex.SetPixels(pixels);
                    tex.Apply();
                    _borderedTileSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
                }
                return _borderedTileSprite;
            }
        }

        public static Texture2D WhiteTex
        {
            get
            {
                if (_whitePixelTex == null)
                {
                    _whitePixelTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _whitePixelTex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    _whitePixelTex.Apply();
                }
                return _whitePixelTex;
            }
        }

        public static Sprite WhiteSprite
        {
            get
            {
                if (_whitePixelSprite == null)
                {
                    _whitePixelSprite = Sprite.Create(WhiteTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
                }
                return _whitePixelSprite;
            }
        }

        public static Sprite[] GetUnitAnimFrames(string unitId, UnitAnimState state)
        {
            if (string.IsNullOrEmpty(unitId)) unitId = "wanderer";
            if (!UnitSheets.TryGetValue(unitId, out var rows))
            {
                rows = SliceUnitSheet(unitId);
                UnitSheets[unitId] = rows;
            }
            int r = (int)state;
            if (rows == null || r < 0 || r >= rows.Length) return new[] { WhiteSprite };
            return rows[r];
        }

        private static Sprite[][] SliceUnitSheet(string unitId)
        {
            var tex = Resources.Load<Texture2D>("Art/Spritesheets/" + unitId);
            if (tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/Spritesheets/wanderer");
            }
            if (tex == null)
            {
                var fallback = new[] { WhiteSprite, WhiteSprite, WhiteSprite, WhiteSprite };
                return new[] { fallback, fallback, fallback, fallback };
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            int cellW = tex.width / 4;
            int cellH = tex.height / 4;
            var rows = new Sprite[4][];
            for (int r = 0; r < 4; r++)
            {
                rows[r] = new Sprite[4];
                // Trong ảnh PNG gốc: hàng 0 ở trên cùng (Idle), hàng 1 (Walk), hàng 2 (Attack), hàng 3 ở dưới cùng (Death).
                // Trong hệ tọa độ Texture2D của Unity: y=0 là đáy ảnh, nên hàng r tương ứng y = (3 - r) * cellH.
                int unityY = (3 - r) * cellH;
                for (int c = 0; c < 4; c++)
                {
                    var rect = new Rect(c * cellW, unityY, cellW, cellH);
                    rows[r][c] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.15f), cellW);
                }
            }
            return rows;
        }

        public static Sprite GetTerrainSprite(TerrainId terrain, string stageId, int x, int y)
        {
            string key = ResolveTileKey(terrain, stageId, x, y);
            if (TileSprites.TryGetValue(key, out var sp) && sp != null)
            {
                return sp;
            }

            var tex = Resources.Load<Texture2D>("Art/Tilesets/" + key);
            if (tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/Tilesets/terrain_floor");
            }
            if (tex == null)
            {
                return WhiteSprite;
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            TileSprites[key] = sp;
            return sp;
        }

        private static string ResolveTileKey(TerrainId terrain, string stageId, int x, int y)
        {
            string val = (terrain.Value ?? "floor").ToLowerInvariant();
            bool alt = ((x + y) & 1) == 1;

            switch (val)
            {
                case "wall":
                    if (stageId == "stage-2") return alt ? "wall_moss" : "wall_stone";
                    if (stageId == "stage-3") return alt ? "wall_grate" : "wall_stone";
                    if (stageId == "stage-4") return alt ? "wall_cracked" : "torch_lit";
                    if (stageId == "stage-5") return alt ? "pillar_intact" : "wall_stone";
                    return alt ? "wall_cracked" : "wall_stone";

                case "pit":
                    return alt ? "pit_spikes" : "pit_stairs";

                case "cracked":
                    return "floor_cracked_stone";

                case "spike-trap":
                    return "trap_spikes";

                case "poison":
                    return alt ? "poison_swamp" : "poison_puddle";

                case "lava":
                    return "lava_pool";

                case "chest":
                    return alt ? "chest_gold" : "chest_iron";

                default:
                    if (stageId == "stage-2") return alt ? "floor_moss" : "floor_oakhaven";
                    if (stageId == "stage-3") return alt ? "floor_grate" : "floor_oakhaven";
                    if (stageId == "stage-4") return alt ? "floor_dirt" : "floor_brick";
                    if (stageId == "stage-5") return alt ? "floor_brick" : "floor_cobble";
                    return alt ? "floor_oakhaven" : "floor_cobble";
            }
        }

        public static Sprite[] GetVfxFrames(string skillId)
        {
            string vfxKey = MapSkillToVfxKey(skillId);
            if (VfxStrips.TryGetValue(vfxKey, out var frames) && frames != null)
            {
                return frames;
            }

            var tex = Resources.Load<Texture2D>("Art/VFX/" + vfxKey);
            if (tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/VFX/vfx_swift_slash");
            }
            if (tex == null)
            {
                return new[] { WhiteSprite };
            }

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            int count = Mathf.Max(1, tex.width / Mathf.Max(1, tex.height));
            int cellW = tex.width / count;
            int cellH = tex.height;
            frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(tex, new Rect(i * cellW, 0, cellW, cellH), new Vector2(0.5f, 0.5f), cellH);
            }
            VfxStrips[vfxKey] = frames;
            return frames;
        }

        public static string MapSkillToVfxKey(string skillId)
        {
            switch (skillId)
            {
                case "rewind":
                case "temporal-step":
                case "dodge":
                    return "vfx_time_rewind";
                case "necrotic-burst":
                case "necrotic-ward":
                    return "vfx_necrotic_burst";
                case "blade-guard":
                case "protect":
                case "divine-barrier":
                case "eye-shield":
                    return "vfx_divine_barrier";
                case "earth-crack":
                case "ashen-fury":
                case "axe-cleave":
                    return "vfx_earth_crack";
                case "plague-storm":
                case "poison-dance":
                case "grave-hands":
                    return "vfx_toxic_miasma";
                case "void-spear-beam":
                case "spear-thrust":
                    return "vfx_void_spear";
                case "hellfire-greatsword":
                case "fireball":
                case "imp-poke":
                    return "vfx_hellfire_wave";
                case "brimstone-meteor":
                    return "vfx_brimstone_meteor";
                case "holy-pillar":
                    return "vfx_holy_pillar";
                case "holy-light-slash":
                case "holy-strike":
                case "holy-sword-slash":
                    return "vfx_holy_slash";
                default:
                    return "vfx_swift_slash";
            }
        }

        public static Texture2D GetPortrait(string portraitId)
        {
            if (string.IsNullOrEmpty(portraitId)) portraitId = "wanderer";
            if (Portraits.TryGetValue(portraitId, out var tex) && tex != null)
            {
                return tex;
            }
            tex = Resources.Load<Texture2D>("Art/Portraits/" + portraitId);
            if (tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/Portraits/wanderer");
            }
            if (tex != null)
            {
                Portraits[portraitId] = tex;
            }
            return tex != null ? tex : WhiteTex;
        }

        public static Texture2D GetBackground(string bgId)
        {
            if (string.IsNullOrEmpty(bgId)) bgId = "bg_oakhaven_ruins";
            if (Backgrounds.TryGetValue(bgId, out var tex) && tex != null)
            {
                return tex;
            }
            tex = Resources.Load<Texture2D>("Art/Backgrounds/" + bgId);
            if (tex == null)
            {
                tex = Resources.Load<Texture2D>("Art/Backgrounds/bg_oakhaven_ruins");
            }
            if (tex != null)
            {
                Backgrounds[bgId] = tex;
            }
            return tex != null ? tex : WhiteTex;
        }
    }
}
