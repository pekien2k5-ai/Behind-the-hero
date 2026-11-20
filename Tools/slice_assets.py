import os
import shutil
from collections import deque
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter, ImageDraw

RAW_DIR = r"D:\tlu\Game\assetsRaw"
PROJECT_DIR = r"D:\tlu\Game\Behind-the-hero"
RES_ART_DIR = os.path.join(PROJECT_DIR, "Assets", "Resources", "Art")
RES_AUDIO_DIR = os.path.join(PROJECT_DIR, "Assets", "Resources", "Audio")
PROJ_ART_DIR = os.path.join(PROJECT_DIR, "Assets", "_Project", "Art")

for d in [
    os.path.join(RES_ART_DIR, "Spritesheets"),
    os.path.join(RES_ART_DIR, "Tilesets"),
    os.path.join(RES_ART_DIR, "VFX"),
    os.path.join(RES_ART_DIR, "Portraits"),
    os.path.join(RES_ART_DIR, "Backgrounds"),
    os.path.join(RES_AUDIO_DIR, "BGM"),
    os.path.join(RES_AUDIO_DIR, "SFX"),
    os.path.join(PROJ_ART_DIR, "Spritesheets"),
    os.path.join(PROJ_ART_DIR, "Tilesets"),
    os.path.join(PROJ_ART_DIR, "VFX"),
    os.path.join(PROJ_ART_DIR, "Portraits"),
    os.path.join(PROJ_ART_DIR, "Backgrounds"),
]:
    os.makedirs(d, exist_ok=True)


def save_both(img: Image.Image, rel_path: str):
    p1 = os.path.join(RES_ART_DIR, rel_path)
    p2 = os.path.join(PROJ_ART_DIR, rel_path)
    img.save(p1, "PNG")
    img.save(p2, "PNG")


def flood_remove_grey_bg(arr_rgb: np.ndarray, tol: int = 26) -> Image.Image:
    """Removes uniform grey background from a cropped sprite cell using border flood-fill + neutral grey chroma key."""
    h, w, _ = arr_rgb.shape
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = arr_rgb
    rgba[:, :, 3] = 255

    # Estimate background color from border pixels
    border_pixels = np.concatenate([
        arr_rgb[0, :, :],
        arr_rgb[h - 1, :, :],
        arr_rgb[:, 0, :],
        arr_rgb[:, w - 1, :]
    ], axis=0)
    bg = np.median(border_pixels, axis=0).astype(int)

    diff_bg = np.abs(arr_rgb.astype(int) - bg).max(axis=2)
    # Also detect grid lines (grey ~90-145 with very low saturation)
    max_c = arr_rgb.max(axis=2).astype(int)
    min_c = arr_rgb.min(axis=2).astype(int)
    sat = max_c - min_c
    lum = arr_rgb.mean(axis=2)
    is_bg_like = (diff_bg <= tol) | ((sat <= 10) & (lum >= 85) & (lum <= 152))

    visited = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        if is_bg_like[0, x]:
            visited[0, x] = True
            q.append((0, x))
        if is_bg_like[h - 1, x] and not visited[h - 1, x]:
            visited[h - 1, x] = True
            q.append((h - 1, x))
    for y in range(h):
        if is_bg_like[y, 0] and not visited[y, 0]:
            visited[y, 0] = True
            q.append((y, 0))
        if is_bg_like[y, w - 1] and not visited[y, w - 1]:
            visited[y, w - 1] = True
            q.append((y, w - 1))

    while q:
        cy, cx = q.popleft()
        rgba[cy, cx, 3] = 0
        for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and is_bg_like[ny, nx]:
                visited[ny, nx] = True
                q.append((ny, nx))

    return Image.fromarray(rgba, mode="RGBA")


def fit_in_cell(sprite_rgba: Image.Image, cell_size: int = 128, bottom_pad: int = 8) -> Image.Image:
    """Trims transparent space and centers the character horizontally, anchored near bottom of a 128x128 cell."""
    bbox = sprite_rgba.getbbox()
    canvas = Image.new("RGBA", (cell_size, cell_size), (0, 0, 0, 0))
    if not bbox:
        return canvas
    cropped = sprite_rgba.crop(bbox)
    cw, ch = cropped.size
    max_dim = cell_size - bottom_pad * 2
    if cw > max_dim or ch > max_dim:
        scale = min(max_dim / cw, max_dim / ch)
        nw = max(1, int(round(cw * scale)))
        nh = max(1, int(round(ch * scale)))
        cropped = cropped.resize((nw, nh), Image.Resampling.NEAREST)
        cw, ch = nw, nh
    ox = (cell_size - cw) // 2
    oy = cell_size - bottom_pad - ch
    canvas.paste(cropped, (ox, max(0, oy)), cropped)
    return canvas


def make_death_frames(idle_frame: Image.Image, tint_rgb=(170, 70, 220)) -> list:
    """Generates a 4-frame Death animation sequence (Hit recoil -> Tilt & Ash -> Collapse -> Dissolve)."""
    frames = []
    # Frame 0: Hit recoil + flash red/purple
    f0 = idle_frame.copy()
    arr0 = np.array(f0).astype(int)
    mask0 = arr0[:, :, 3] > 0
    arr0[mask0, 0] = np.clip(arr0[mask0, 0] + 75, 0, 255)
    arr0[mask0, 1] = np.clip(arr0[mask0, 1] - 35, 0, 255)
    f0_img = Image.fromarray(arr0.astype(np.uint8), "RGBA").rotate(-12, resample=Image.Resampling.NEAREST, expand=False)
    frames.append(fit_in_cell(f0_img))

    # Frame 1: Kneel / tilt 38 deg + darken
    f1 = idle_frame.copy().rotate(-38, resample=Image.Resampling.NEAREST, expand=True)
    arr1 = np.array(f1).astype(int)
    mask1 = arr1[:, :, 3] > 0
    arr1[mask1, 0] = (arr1[mask1, 0] * 6 + tint_rgb[0] * 4) // 10
    arr1[mask1, 1] = (arr1[mask1, 1] * 6 + tint_rgb[1] * 4) // 10
    arr1[mask1, 2] = (arr1[mask1, 2] * 6 + tint_rgb[2] * 4) // 10
    frames.append(fit_in_cell(Image.fromarray(arr1.astype(np.uint8), "RGBA")))

    # Frame 2: Fallen horizontal (75 deg) + fading
    f2 = idle_frame.copy().rotate(-78, resample=Image.Resampling.NEAREST, expand=True)
    arr2 = np.array(f2).astype(int)
    mask2 = arr2[:, :, 3] > 0
    arr2[mask2, 0] = (arr2[mask2, 0] * 4 + tint_rgb[0] * 6) // 10
    arr2[mask2, 1] = (arr2[mask2, 1] * 4 + tint_rgb[1] * 6) // 10
    arr2[mask2, 2] = (arr2[mask2, 2] * 4 + tint_rgb[2] * 6) // 10
    arr2[mask2, 3] = (arr2[mask2, 3] * 75) // 100
    frames.append(fit_in_cell(Image.fromarray(arr2.astype(np.uint8), "RGBA"), bottom_pad=6))

    # Frame 3: Ash dissolve (checkerboard pixel dissolve on fallen pose)
    arr3 = arr2.copy()
    yy, xx = np.indices(arr3.shape[:2])
    dissolve = ((xx // 2 + yy // 2) % 2 == 0)
    arr3[dissolve, 3] = (arr3[dissolve, 3] * 25) // 100
    arr3[~dissolve, 3] = (arr3[~dissolve, 3] * 55) // 100
    frames.append(fit_in_cell(Image.fromarray(arr3.astype(np.uint8), "RGBA"), bottom_pad=4))
    return frames


def build_unit_sheet(unit_id: str, idle_frames: list, walk_frames: list, attack_frames: list, death_tint=(170, 70, 220)):
    """Builds a standardized 512x512 (4 rows x 4 cols of 128x128) PNG spritesheet:
       Row 0 (top): Idle (4 frames)
       Row 1: Walk (4 frames)
       Row 2: Attack (4 frames)
       Row 3 (bottom): Death (4 frames)
    """
    death_frames = make_death_frames(idle_frames[0], death_tint)
    sheet = Image.new("RGBA", (128 * 4, 128 * 4), (0, 0, 0, 0))
    rows = [idle_frames, walk_frames, attack_frames, death_frames]
    for r_idx, r_frames in enumerate(rows):
        for c_idx in range(4):
            frame = r_frames[c_idx % len(r_frames)]
            sheet.paste(frame, (c_idx * 128, r_idx * 128), frame)
    save_both(sheet, os.path.join("Spritesheets", f"{unit_id}.png"))
    print(f"Saved unit spritesheet: {unit_id}.png (512x512, 16 frames: Idle/Walk/Attack/Death)")


# ==============================================================================
# 1. SLICE CHARACTERS (Wanderer, Eosphoros, Imp, Gargoyle)
# ==============================================================================
chars_im = np.array(Image.open(os.path.join(RAW_DIR, "spritesheet_characters.jpg")).convert("RGB"))

def slice_char_grid(row_a: int, row_b: int):
    # row_a has 4 idle + 4 attack/action; row_b has 4 idle-alt + 4 walk/run
    idle = []
    walk = []
    attack = []
    for c in range(4):
        cell_idle = chars_im[row_a*128 + 4 : (row_a+1)*128 - 4, c*128 + 4 : (c+1)*128 - 4]
        idle.append(fit_in_cell(flood_remove_grey_bg(cell_idle)))
    for c in range(4, 8):
        cell_atk = chars_im[row_a*128 + 4 : (row_a+1)*128 - 4, c*128 + 4 : (c+1)*128 - 4]
        cell_wlk = chars_im[row_b*128 + 4 : (row_b+1)*128 - 4, c*128 + 4 : (c+1)*128 - 4]
        attack.append(fit_in_cell(flood_remove_grey_bg(cell_atk)))
        walk.append(fit_in_cell(flood_remove_grey_bg(cell_wlk)))
    return idle, walk, attack

w_idle, w_walk, w_atk = slice_char_grid(0, 1)
build_unit_sheet("wanderer", w_idle, w_walk, w_atk, (155, 80, 220))

e_idle, e_walk, e_atk = slice_char_grid(2, 3)
build_unit_sheet("eosphoros", e_idle, e_walk, e_atk, (230, 195, 90))

i_idle, i_walk, i_atk = slice_char_grid(4, 5)
build_unit_sheet("imp", i_idle, i_walk, i_atk, (180, 50, 90))

g_idle, g_walk, g_atk = slice_char_grid(6, 7)
build_unit_sheet("gargoyle", g_idle, g_walk, g_atk, (110, 120, 135))


# ==============================================================================
# 2. SLICE STAGE BOSSES (Ashen Executioner, Morbida, Abyss Knight)
# ==============================================================================
sb_im = np.array(Image.open(os.path.join(RAW_DIR, "spritesheet_stage_bosses.jpg")).convert("RGB"))

def extract_boxes(arr, y0, y1, x_ranges):
    frames = []
    for x0, x1 in x_ranges:
        sub = arr[y0:y1+1, x0:x1+1]
        frames.append(fit_in_cell(flood_remove_grey_bg(sub)))
    return frames

# Boss 1: Ashen Executioner
ashen_f = extract_boxes(sb_im, 14, 331, [(40, 193), (216, 391), (402, 588), (600, 799)])
ashen_idle = [ashen_f[0], ashen_f[1], ashen_f[0], ashen_f[1]]
ashen_walk = [ashen_f[0], ashen_f[1], ashen_f[2], ashen_f[1]]
ashen_atk  = [ashen_f[0], ashen_f[1], ashen_f[2], ashen_f[3]]
build_unit_sheet("boss-ashen", ashen_idle, ashen_walk, ashen_atk, (200, 95, 45))

# Boss 2: Plague Witch Morbida
morb_f = extract_boxes(sb_im, 417, 653, [(62, 197), (259, 514), (522, 744), (782, 989)])
morb_idle = [morb_f[0], morb_f[1], morb_f[0], morb_f[1]]
morb_walk = [morb_f[0], morb_f[1], morb_f[2], morb_f[1]]
morb_atk  = [morb_f[0], morb_f[1], morb_f[2], morb_f[3]]
build_unit_sheet("boss-morbida", morb_idle, morb_walk, morb_atk, (80, 210, 90))

# Boss 3: Abyss Knight
abyss_f = extract_boxes(sb_im, 737, 1000, [(31, 247), (251, 515), (525, 785), (787, 1000)])
abyss_idle = [abyss_f[0], abyss_f[3], abyss_f[0], abyss_f[3]]
abyss_walk = [abyss_f[0], abyss_f[1], abyss_f[0], abyss_f[3]]
abyss_atk  = [abyss_f[0], abyss_f[2], abyss_f[1], abyss_f[3]]
build_unit_sheet("boss-abyss", abyss_idle, abyss_walk, abyss_atk, (130, 60, 210))


# ==============================================================================
# 3. SLICE FINAL BOSSES (Demon King Malkor & Fallen Eosphoros)
# ==============================================================================
fb_im = np.array(Image.open(os.path.join(RAW_DIR, "spritesheet_bosses.jpg")).convert("RGB"))

malkor_idle_f = extract_boxes(fb_im, 14, 200, [(12, 178), (208, 373), (418, 586), (616, 785)])
malkor_atk_f  = extract_boxes(fb_im, 247, 502, [(11, 172), (184, 322), (484, 664), (676, 831)])
malkor_walk_f = [malkor_idle_f[0], malkor_idle_f[1], malkor_idle_f[2], malkor_idle_f[3]]
build_unit_sheet("boss-malkor", malkor_idle_f, malkor_walk_f, malkor_atk_f, (230, 70, 30))

fallen_cast_f = extract_boxes(fb_im, 521, 703, [(26, 149), (187, 382), (628, 820), (828, 1015)])
fallen_atk_f  = extract_boxes(fb_im, 851, 1018, [(25, 137), (176, 316), (362, 564), (581, 836)])
fallen_idle_f = [fallen_cast_f[0], fallen_atk_f[0], fallen_cast_f[1], fallen_atk_f[0]]
fallen_walk_f = [fallen_atk_f[0], fallen_atk_f[1], fallen_atk_f[2], fallen_atk_f[0]]
build_unit_sheet("boss-fallen-eosphoros", fallen_idle_f, fallen_walk_f, fallen_atk_f, (240, 200, 80))


# ==============================================================================
# 4. SLICE TILESET DUNGEON (24 tiles -> individual named PNGs + clean 4x6 atlas)
# ==============================================================================
tiles_im = np.array(Image.open(os.path.join(RAW_DIR, "tileset_dungeon.jpg")).convert("RGB"))
col_ranges = [(161, 316), (340, 501), (520, 680), (703, 864)]
row_ranges = [(28, 178), (196, 343), (358, 506), (517, 664), (676, 821), (835, 982)]

tile_names = [
    ["floor_oakhaven", "floor_brick", "floor_cracked_stone", "floor_moss"],
    ["wall_stone", "wall_cracked", "wall_grate", "wall_moss"],
    ["trap_spikes", "pit_spikes", "floor_grate", "rubble_rocks"],
    ["chest_gold", "chest_iron", "stairs_up", "pit_stairs"],
    ["pillar_intact", "pillar_broken", "torch_lit", "torch_unlit"],
    ["poison_puddle", "poison_swamp", "floor_dirt", "floor_cobble"],
]

atlas = Image.new("RGBA", (128 * 4, 128 * 6), (0, 0, 0, 0))
extracted_tiles = {}

for r_i, (y0, y1) in enumerate(row_ranges):
    for c_i, (x0, x1) in enumerate(col_ranges):
        sub = tiles_im[y0:y1+1, x0:x1+1]
        t_img = Image.fromarray(sub, "RGB").resize((128, 128), Image.Resampling.NEAREST).convert("RGBA")
        name = tile_names[r_i][c_i]
        extracted_tiles[name] = t_img
        save_both(t_img, os.path.join("Tilesets", f"{name}.png"))
        atlas.paste(t_img, (c_i * 128, r_i * 128))

# Also create canonical terrain tiles matching TerrainId (floor, wall, pit, cracked, poison, lava, chest)
# For Lava, tint poison_swamp / floor_cracked_stone with fiery magma orange-red glow
cracked_base = np.array(extracted_tiles["floor_cracked_stone"]).astype(int)
lava_arr = cracked_base.copy()
lum = lava_arr[:, :, :3].mean(axis=2)
cracks = lum < 72
lava_arr[cracks, 0] = 255
lava_arr[cracks, 1] = np.clip(lum[cracks] * 2, 70, 185)
lava_arr[cracks, 2] = 20
lava_arr[~cracks, 0] = np.clip(lava_arr[~cracks, 0] // 2 + 30, 0, 120)
lava_arr[~cracks, 1] = lava_arr[~cracks, 1] // 3
lava_arr[~cracks, 2] = lava_arr[~cracks, 2] // 3
lava_tile = Image.fromarray(lava_arr.astype(np.uint8), "RGBA")
extracted_tiles["lava_pool"] = lava_tile
save_both(lava_tile, os.path.join("Tilesets", "lava_pool.png"))

canonical_map = {
    "terrain_floor": extracted_tiles["floor_oakhaven"],
    "terrain_wall": extracted_tiles["wall_stone"],
    "terrain_pit": extracted_tiles["pit_spikes"],
    "terrain_cracked": extracted_tiles["floor_cracked_stone"],
    "terrain_poison": extracted_tiles["poison_swamp"],
    "terrain_lava": lava_tile,
    "terrain_chest": extracted_tiles["chest_gold"],
}
for cname, cimg in canonical_map.items():
    save_both(cimg, os.path.join("Tilesets", f"{cname}.png"))

save_both(atlas, os.path.join("Tilesets", "dungeon_tileset_atlas.png"))
print("Saved 25 individual terrain tiles + dungeon_tileset_atlas.png")


# ==============================================================================
# 5. SLICE VFX SPRITESHEETS (4 raw VFX sheets -> clean 4-frame 512x128 strips)
# ==============================================================================
def dark_bg_to_alpha(sub_rgb: np.ndarray) -> Image.Image:
    h, w, _ = sub_rgb.shape
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[:, :, :3] = sub_rgb
    max_c = sub_rgb.max(axis=2).astype(int)
    min_c = sub_rgb.min(axis=2).astype(int)
    sat = max_c - min_c
    # Remove dark grid background and subtle grey grid lines
    alpha = np.clip((max_c - 22) * 255 // 45, 0, 255)
    grid_line = (sat < 16) & (max_c < 110)
    alpha[grid_line] = 0
    # Clear 6px perimeter and top-left / bottom-right number label boxes
    alpha[:6, :] = 0
    alpha[-6:, :] = 0
    alpha[:, :8] = 0
    alpha[:, -8:] = 0
    alpha[:28, :28] = 0
    alpha[-26:, -28:] = 0
    alpha[-26:, :28] = 0
    rgba[:, :, 3] = alpha.astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def build_vfx_strip(vfx_name: str, src_path: str, boxes: list):
    im = np.array(Image.open(src_path).convert("RGB"))
    strip = Image.new("RGBA", (128 * len(boxes), 128), (0, 0, 0, 0))
    for i, (x0, y0, x1, y1) in enumerate(boxes):
        sub = im[y0:y1, x0:x1]
        rgba = dark_bg_to_alpha(sub)
        cell = fit_in_cell(rgba, 128, bottom_pad=6)
        strip.paste(cell, (i * 128, 0), cell)
    save_both(strip, os.path.join("VFX", f"{vfx_name}.png"))
    print(f"Saved VFX strip: {vfx_name}.png ({len(boxes)} frames)")

w_vfx_path = os.path.join(RAW_DIR, "vfx_skills_wanderer.jpg")
build_vfx_strip("vfx_time_rewind", w_vfx_path, [
    (10, 42, 140, 170), (295, 42, 430, 170), (730, 42, 865, 170), (150, 180, 285, 315)
])
build_vfx_strip("vfx_swift_slash", w_vfx_path, [
    (10, 380, 140, 500), (150, 380, 285, 500), (440, 380, 575, 500), (730, 380, 865, 500)
])
build_vfx_strip("vfx_necrotic_burst", w_vfx_path, [
    (15, 690, 195, 830), (415, 690, 595, 830), (820, 690, 1005, 830), (215, 850, 395, 995)
])

e_vfx_path = os.path.join(RAW_DIR, "vfx_skills_eosphoros.jpg")
build_vfx_strip("vfx_divine_barrier", e_vfx_path, [
    (185, 55, 325, 235), (345, 55, 495, 235), (650, 55, 815, 235), (825, 55, 1005, 235)
])
build_vfx_strip("vfx_holy_pillar", e_vfx_path, [
    (215, 260, 390, 595), (415, 405, 595, 595), (610, 405, 805, 595), (820, 405, 1005, 595)
])
build_vfx_strip("vfx_holy_slash", e_vfx_path, [
    (215, 615, 395, 795), (615, 615, 795, 795), (215, 815, 395, 975), (415, 815, 595, 975)
])

d_vfx_path = os.path.join(RAW_DIR, "vfx_skills_demon_king.jpg")
build_vfx_strip("vfx_hellfire_wave", d_vfx_path, [
    (195, 55, 385, 175), (595, 55, 805, 175), (25, 195, 215, 315), (415, 195, 605, 315)
])
build_vfx_strip("vfx_brimstone_meteor", d_vfx_path, [
    (175, 380, 315, 495), (510, 380, 665, 495), (340, 520, 495, 650), (505, 520, 665, 650)
])
build_vfx_strip("vfx_dark_pillar", d_vfx_path, [
    (165, 720, 295, 975), (435, 720, 575, 975), (580, 710, 715, 975), (720, 710, 865, 975)
])

b_vfx_path = os.path.join(RAW_DIR, "vfx_skills_stage_bosses.jpg")
build_vfx_strip("vfx_earth_crack", b_vfx_path, [
    (730, 50, 995, 180), (15, 195, 200, 325), (215, 195, 420, 325), (440, 195, 710, 325)
])
build_vfx_strip("vfx_toxic_miasma", b_vfx_path, [
    (405, 400, 550, 515), (565, 400, 705, 515), (290, 535, 425, 655), (585, 535, 725, 655)
])
build_vfx_strip("vfx_void_spear", b_vfx_path, [
    (130, 730, 245, 845), (380, 730, 695, 845), (305, 860, 585, 985), (710, 730, 1005, 985)
])


# ==============================================================================
# 6. COPY & RESIZE 7 CHARACTER PORTRAITS (512x512 PNG)
# ==============================================================================
portrait_map = {
    "portrait_wanderer.jpg": "wanderer",
    "portrait_eosphoros.jpg": "eosphoros",
    "portrait_boss_ashen_executioner.jpg": "boss-ashen",
    "portrait_boss_plague_witch.jpg": "boss-morbida",
    "portrait_boss_abyss_knight.jpg": "boss-abyss",
    "portrait_boss_demon_king.jpg": "boss-malkor",
    "portrait_boss_fallen_eosphoros.jpg": "boss-fallen-eosphoros",
}
for src_file, pid in portrait_map.items():
    p_img = Image.open(os.path.join(RAW_DIR, src_file)).convert("RGBA").resize((512, 512), Image.Resampling.BILINEAR)
    save_both(p_img, os.path.join("Portraits", f"{pid}.png"))
print("Saved 7 portraits (512x512 PNG)")


# ==============================================================================
# 7. GENERATE 5 DUEL / STAGE BACKGROUNDS (1280x720 16:9 PNG)
# ==============================================================================
bg_specs = [
    ("bg_oakhaven_ruins", (22, 18, 24), (88, 42, 28), "floor_oakhaven", "wall_cracked", "torch_lit"),
    ("bg_catacomb_toxic", (14, 24, 20), (36, 92, 52), "floor_moss", "wall_moss", "pillar_broken"),
    ("bg_void_arena", (16, 14, 30), (82, 46, 138), "floor_grate", "wall_grate", "pillar_intact"),
    ("bg_fire_arena", (28, 12, 12), (148, 38, 24), "lava_pool", "wall_cracked", "torch_lit"),
    ("bg_mausoleum", (20, 22, 34), (165, 138, 62), "floor_brick", "wall_stone", "pillar_intact"),
]

for bg_id, top_col, glow_col, floor_key, wall_key, prop_key in bg_specs:
    W, H = 960, 540
    bg = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    arr = np.zeros((H, W, 4), dtype=np.uint8)
    yy = np.linspace(0, 1, H)[:, None]
    xx = np.linspace(-1, 1, W)[None, :]
    radial = np.clip(1.0 - np.sqrt(xx**2 * 0.7 + (yy - 0.55)**2 * 1.4), 0, 1)
    for c in range(3):
        base_grad = top_col[c] * (1 - yy * 0.5) + glow_col[c] * (yy * 0.5)
        arr[:, :, c] = np.clip(base_grad + glow_col[c] * radial * 0.55, 0, 255).astype(np.uint8)
    arr[:, :, 3] = 255
    bg = Image.fromarray(arr, "RGBA")

    # Tile dungeon wall along the mid-ground & floor tiles along bottom
    wall_t = extracted_tiles[wall_key].resize((96, 96), Image.Resampling.NEAREST)
    floor_t = extracted_tiles[floor_key].resize((96, 96), Image.Resampling.NEAREST)
    prop_t = extracted_tiles[prop_key].resize((96, 96), Image.Resampling.NEAREST)
    for x in range(0, W, 96):
        bg.paste(wall_t, (x, 220))
        if (x // 96) % 2 == 0:
            bg.paste(prop_t, (x, 220))
        bg.paste(floor_t, (x, 316))
        bg.paste(floor_t, (x, 412))

    # Dark vignette overlay for dramatic Duel framing
    overlay = Image.new("RGBA", (W, H), (8, 10, 16, 110))
    bg = Image.alpha_composite(bg, overlay)
    save_both(bg, os.path.join("Backgrounds", f"{bg_id}.png"))

# Also copy mockup for reference
shutil.copy2(os.path.join(RAW_DIR, "mockup_gameplay_grid.jpg"), os.path.join(PROJ_ART_DIR, "mockup_gameplay_grid.jpg"))
print("Saved 5 Duel backgrounds (960x540 PNG)")


# ==============================================================================
# 8. COPY AUDIO FILES TO Assets/Resources/Audio/
# ==============================================================================
for bgm in ["forest_theme.ogg", "boss_theme.ogg"]:
    src = os.path.join(RAW_DIR, "downloaded", "music", bgm)
    if os.path.exists(src):
        shutil.copy2(src, os.path.join(RES_AUDIO_DIR, "BGM", bgm))

for sfx in [
    "attack1.ogg", "attack2.ogg", "attack3.ogg", "boss_attack.ogg", "boss_roar.ogg",
    "checkpoint.ogg", "dash.ogg", "enemy_death.ogg", "enemy_hurt.ogg",
    "player_death.ogg", "player_hurt.wav", "transform_dark.ogg", "transform_light.ogg",
    "ui_confirm.ogg", "ui_select.ogg"
]:
    src = os.path.join(RAW_DIR, "downloaded", "sfx", sfx)
    if os.path.exists(src):
        shutil.copy2(src, os.path.join(RES_AUDIO_DIR, "SFX", sfx))

print("Copied BGM & SFX audio assets into Assets/Resources/Audio/")
