# KẾ HOẠCH HOÀN THÀNH GAME — "Behind the Hero"

> Lập 2026-11-15, cập nhật **2026-11-19** sau khi hoàn tất toàn bộ **P0 → P5** (`BTH.Core`, `BTH.Content`, `BTH.Game`, `BTH.UI`, `BTH.Editor`, bộ Art PNG đã tách nền & Audio BGM/SFX, **81/81 unit test xanh** cả `dotnet test` lẫn Unity EditMode, chạy trực tiếp trong Play Mode với 0 lỗi console) — xem `Docs/QUYET_DINH.md`, `Docs/THIET_KE_PHASE2_DUEL.md`, `Docs/BAO_CAO_CV3_NGUYEN_XUAN_LANG.md`.
> Căn cứ: `KIEN_TRUC_UNITY_BEHIND_THE_HERO.md`, tài liệu thiết kế nhóm 10, trạng thái code thực tế của repo `Behind-the-hero`.

---

## 0. Hiện trạng (đã kiểm chứng 2026-11-19)

| Hạng mục | Trạng thái |
|---|---|
| **Core** `Assets/_Project/Core` | ✅ **163 file**, 6.608 dòng. Grid, Units, Turn, Actions, Combat (skill/effect/status/clash/**duel**), Ai, Run (rules/stage/snapshot/event/dialogue/session) |
| **Duel mode (Phase 2)** | ✅ **đã hoàn tất từ Core tới UI**: `TurnPhase.Duel`, `ClashDuel`/`DuelState`/`IClashDuel`, `EnterDuel` khi boss <70%, auto finisher, `DuelSnapshot`, transition + màn hình Duel 3 thẻ chiêu + dự báo Thắng/Hòa/Thua |
| **Test** `Assets/Tests` | ✅ **81 test / 11 bộ**, xanh `dotnet test` (~90 ms) và **Unity EditMode 81/81** (2026-11-19) |
| **Harness** `Tools/CoreTests` | ✅ chạy test ngoài Unity bằng .NET 8 SDK; đã include cả Core + Content |
| **Content** `Assets/_Project/Content` | ✅ **đã hoàn tất**: 9 unit (`UnitCatalog`), 26 chiêu + behaviour (`SkillCatalog`), 8 brain (`BrainCatalog`), 8 loại địa hình tương tác (`TerrainCatalog`), 5 màn (`StageCatalog`), 5 Duel (`DuelCatalog`), cốt truyện 3 hồi + thoại 5 ải + 2 Ending (`DialogueCatalog`), `GameMatrixCatalog`, `TacticalNavigator`, `ArtAssetCatalog`, `ContentCatalog` |
| **Unity presentation** `Assets/_Project/Game` | ✅ **đã hoàn tất**: `GameBootstrap`, `ArtCatalog`, `UnitSpriteAnimator` (4 trạng thái `Idle/Walk/Attack/Death`), `VfxPopupManager` (12 dải VFX + popup sát thương), `AudioCatalog`, `GameOptionsModel` |
| **UI** `Assets/_Project/UI` & `GameBootstrap` | ✅ **đã hoàn tất**: Cột HUD chiến thuật bên trái, Top Bar chọn 5 ải + Thanh máu Boss (vạch 70% Duel), Khung chân dung đối thoại + lựa chọn 2 Ending A/B, Màn hình Phase 2 Duel, Bảng Cài đặt (`Settings`) |
| **Art & Audio** `Assets/_Project/Art` & `Assets/Resources` | ✅ **đã tách nền & tích hợp**: `Tools/slice_assets.py` xuất 9 Spritesheets 512×512 PNG (`Idle/Walk/Attack/Death`), 25 Tiles 128×128 + `dungeon_tileset_atlas.png`, 12 VFX strips, 7 Portraits 512×512, 5 Duel Backgrounds 960×540, 2 BGM + 14 SFX |
| **Git** | ✅ `main`, `kien/dev`, `l4ngx` (đã commit & push đầy đủ lên GitHub) |

### Việc kỹ thuật còn lại trước khi làm UI

1. ✅ **Content đã xong** (P1): `UnitCatalog`, `SkillCatalog`, `StageCatalog`, `DuelCatalog`, brain, terrain, dialogue, `ContentCatalog`; ải 1 đã chơi được từ bàn cờ tới Duel Victory bằng test.
2. **Unity presentation/UI trống**: chưa có scene, board/unit view, input, HUD, DuelScreen, transition — đây là khối lượng lớn nhất còn lại (P2 → P3).
3. **Ally đã cắt**: theo `Docs/QUYET_DINH.md`, Eosphoros không tham gia gameplay — không cần mở nhánh Ally trong `GameSession`, không tốn thêm ngày công.

---

## 1. Định nghĩa "game hoàn thành" (Definition of Done)

Một bản build Windows **chơi được trọn vòng**:

- Menu → chọn ải → chơi 5 ải → 2 kết thúc (Ending A/B).
- Điều khiển chuột + phím: di chuyển, đánh, chiêu, Guard, End Turn.
- **Phase 2 = Duel**: boss xuống <70% HP → chuyển cảnh (~1,2 s) → màn đấu chiêu, **không còn bàn cờ**, player trái / boss phải trên background, chỉ chọn 1 trong 3 chiêu Clash mỗi vòng; thắng → boss Stagger + 1 đòn kết liễu; thua → ăn damage; hoà → cả 2 chip damage; HP mang theo từ phase 1.
- Thua → màn hình thua → **Rewind** về StageStart (hoặc ClashStart/đầu Duel) → RunKnowledge (đã thấy chiêu boss, số lần chết) được giữ.
- HUD đủ: portrait, HP pips, MP (trang trí), AP, nút kỹ năng + hotkey, turn counter, banner ý đồ boss.
- Settings: âm lượng Master/BGM/SFX, độ phân giải, fullscreen, tốc độ animation, bật/tắt grid + danger tiles; lưu qua lần chạy sau.
- 60 FPS ở 1920×1080; không lỗi console; build chạy trên máy **không cài Unity**.
- `dotnet test` xanh (75 test Core + Content + Duel) và Unity EditMode 75/75; README hướng dẫn chạy + điều khiển.

**Ngoài phạm vi** (trừ khi bạn yêu cầu): WebGL, save giữa run (ADR-05: không), multiplayer, localization tiếng Anh, mobile.

---

## 2. Kiến trúc đích (giữ nguyên 4 lớp)

```
Assets/_Project/
  Core/            ✅ xong (kể cả Duel mode + 75 test)
  Content/         ✅ xong (30 file): data + behaviour, asmdef refs Core, KHÔNG refs UnityEngine
    Units/  Skills/  Skills/Behaviours/  Effects/  Brains/  Terrain/  Stages/  Duels/  Dialogues/
  Game/            ← P2: Unity presentation, refs Core+Content+InputSystem
    Bootstrap/  Board/  Units/  Duel/  Presenters/  Input/  Camera/  Audio/
  UI/              ← P3: HUD, menu, duel screen, transition, dialogue; refs Core+Game+uGUI+TMP
  Editor/          ← công cụ import/slice art (chỉ Editor)
  Art/ Audio/ Scenes/ Prefabs/
```

Luồng một lệnh (không đổi):

```
Input/UI → ActionRequest → IActionValidator → IGameActionExecutor
        → state đổi → event bus → Presenter → View (animation/SFX/hiệu ứng)
```

Scene: **1 scene `Game.unity`** gồm `Bootstrap`, `BoardRoot`, `UnitRoot`, `FxRoot`, `UIRoot`, `DuelRoot` (ẩn cho tới phase 2); menu là `Menu.unity` (hoặc panel phủ, rẻ hơn cho demo).

---

## 3. Các pha triển khai

### P0 — Chốt quyết định & dựng khung ✅ **XONG 2026-11-19**

| Việc | Output |
|---|---|
| Chốt 5 quyết định lịch (§8) + 6 mặc định Duel (`THIET_KE_PHASE2_DUEL.md` §7) | ghi vào `Docs/QUYET_DINH.md` |
| Tạo asmdef `BTH.Content`, `BTH.Unity` (tên `BTH.Game`), `BTH.UI`, `BTH.Editor` đúng referents trong doc (Core→none; Content→Core; Game→Core+Content+InputSystem; UI→Core+Game+uGUI+TMP) | 4 file asmdef + test compile |
| Tạo cây `Art/Audio/Scenes/Prefabs` trong `_Project`; import settings: PPU 32, Point filter, Compression None cho pixel art | Project Settings ổn định |
| Tạo palette asset + font TMP có dấu tiếng Việt (Be Vietnam Pro / Inter) | Font test chuỗi "Ế ộ ữ ỹ đ" hiển thị đúng |

**Nghiệm thu:** project compile 0 lỗi với 4 asmdef mới; font hiển thị đủ dấu.

---

### P1 — Content layer ✅ **XONG 2026-11-16** (30 file / 2.035 dòng, 75 test xanh)

| # | Việc | Kết quả |
|---|---|---|
| 1.1 | `UnitCatalog`: 9 definition đúng bảng số | ✅ `Content/Units/UnitCatalog.cs` |
| 1.2 | `SkillCatalog`: 29 chiêu (6 player, 5 Eosphoros, 3 quái, 15 boss), nhóm `IsClashSkill` power 14–18 | ✅ `Content/Skills/SkillCatalog.cs` |
| 1.3 | Behaviour: Holy Pillar, Holy Sword Slash, Earth Crack, Toxic Cloud, Grave Hands, Void Spear, Hellfire, Brimstone, Dark Pillar, Divine Barrier + Melee/Row/Cross/Square/Cone/Pierce/Status | ✅ `Content/Skills/Behaviours/` (10 file) |
| 1.4 | Brain: MeleeRusher, RangedLurker, BossBrainBase + 5 boss brain, AllyBrain | ✅ `Content/Brains/` + `BrainCatalog` (8 brain) |
| 1.5 | Terrain: floor/wall/pit/cracked/poison/lava/chest | ✅ `Content/Terrain/` (2 file) |
| 1.6 | 5 `StageDefinition`: 8×8 / 9×9 / 9×9 / 10×10 / 8×8, TurnLimit 20–25, terrain override | ✅ `Content/Stages/StageCatalog.cs` |
| 1.7 | `DuelCatalog`: 5 `DuelDefinition`, boss power 15/14/16/18/18, 2 Win + 1 Lose/Draw mỗi ải | ✅ `Content/Duels/DuelCatalog.cs` |
| 1.8 | Dialogue trước/sau trận + 2 ending | ✅ `Content/Dialogues/DialogueCatalog.cs` (11 graph) |
| 1.9 | Test Content: catalog, behaviour, ma trận Duel, chơi ải 1 → Duel → Victory | ✅ `Assets/Tests/ContentTests.cs` (8 ca, tổng 75) |

**Nghiệm thu:** ✅ `dotnet test` 75/75; Unity EditMode 75/75; `Stage1_CanBePlayedFromBoardToDuelVictory_WithoutUnity` chạy trọn ải 1 → Duel → thắng, không cần Unity.

> Pha này **làm hoàn toàn bằng code + test**, không cần Editor. Làm xong là đã có "game chạy được bằng test", kể cả Duel.

---

### P2 — Unity presentation ✅ **XONG 2026-11-19** (`Assets/_Project/Game/`)

| # | Việc | Ghi chú |
|---|---|---|
| 2.1 | `GameBootstrap`: dựng Content catalogs → `GameSession` → start stage; wire `EventBus` vào presenter | 1 composition root duy nhất |
| 2.2 | `BoardView`: tilemap hoặc lưới sprite từ tileset; overlay: ô chọn, vùng đi được, danger cells, vùng chiêu, làm mờ ngoài LOS | dùng `IGridMap` + event `CellChanged/CellCollapsed` |
| 2.3 | `UnitView` + pool: sprite hướng theo faction, HP bar, AP pips, status icon, footprint 2×2 cho Malkor, sort order theo Y | subscribe `UnitSpawned/Moved/DamageDealt/Died/StatusApplied` |
| 2.4 | `InputController` (Input System): phím mũi tên/G/S/D/E/Space/X + chuột trái; hover tooltip; chặn input khi phase Enemy/animation | validate bằng `IActionValidator.GetValidTargets` trước khi gửi |
| 2.5 | Presenter: `TurnPresenter` (banner 4 nhịp), `CombatPresenter` (damage popup, hit flash), `TelegraphPresenter` (danger cells + icon chiêu), `CameraShakePresenter` (từ `CameraShakeHintEvent`) | mỗi event = 1 phản ứng, không tự tính luật |
| 2.6 | `CameraRig`: fit lưới 8×8→10×10, kẹp biên, follow nhẹ unit đang hành động | |
| 2.7 | `DuelScreen` khung: background placeholder + player/boss 2 bên + 3 card trống + HP bar; ẩn/hiện theo `TurnPhase.Duel` | dựng sớm để thấy luồng chuyển cảnh |
| 2.8 | PlayMode smoke test: mở scene, start ải 1, đi 1 ô, đánh 1 đòn, End Turn | `Assets/Tests/PlayMode/` |

**Nghiệm thu:** chơi được ải 1 trong Editor bằng chuột/phím, thấy máu và danger cells cập nhật đúng; ép boss xuống 70% thì màn Duel hiện ra (dữ liệu thật, UI còn xấu).

---

### P3 — UI ✅ **XONG 2026-11-19** (`Assets/_Project/UI/` & `GameBootstrap`)

| # | Việc |
|---|---|
| 3.1 | HUD: portrait + tên, HP pips đỏ, MP gems (trang trí), AP, hàng nút Move/Slash/Guard/Skill/Step/End Turn kèm hotkey, turn counter, stage name |
| 3.2 | **DuelScreen đầy đủ**: background full-screen, player trái / boss phải (pose idle + attack), 2 HP bar + tên + "PHASE 2", banner chiêu boss (tên/hệ/uy lực), 3 card chiêu với badge xanh/vàng/đỏ + power dự đoán, tự động kết liễu sau khi thắng (không cần nút Attack), round counter, flash/shake khi resolve |
| 3.3 | **Transition cutscene**: 1 prefab dùng chung, tham số màu/nhạc — rung + flash + `boss_roar` + `transform_dark` → fade đen → fade in background; khoá input; ~1,2 s |
| 3.4 | Banner ý đồ boss trên bàn cờ (skill + danger cells), tooltip mô tả chiêu |
| 3.5 | Menu: Main (Play/Settings/Quit), Pause, Settings (âm lượng, resolution, fullscreen, tốc độ anim, grid/danger toggle, remap phím), Game Over + Rewind, Victory + Ending A/B, Credits |
| 3.6 | Dialogue UI: portrait speaker + text typewriter + choice, tua nhanh |
| 3.7 | Lưu settings bằng PlayerPrefs; áp dụng ngay không cần restart |

**Nghiệm thu:** chơi trọn 1 ải **chỉ bằng UI**, gồm cả đoạn chuyển cảnh và Duel; đổi settings → thoát → mở lại vẫn giữ; Clash chọn được 3 chiêu và ra đúng kết quả.

---

### P4 — Game flow: Duel + 5 ải ✅ **XONG 2026-11-19**

| # | Việc | Ghi chú |
|---|---|---|
| 4.1 | ✅ **Core Duel mode** đã xong: `TurnPhase.Duel`, `ClashDuel`/`DuelState`/`IClashDuel`, `ClashMath`, `EnterDuel()` khi HP boss <70%, validator/executor route, `DuelSnapshot`, `DuelRoundStartedEvent`, `DuelEndedEvent`; `ObjectiveEvaluator` bỏ turn-limit khi Duel; `DuelFlowTests` **13 ca** | xong 2026-11-15; hiện 65/75 test xanh (auto finisher) |
| 4.2 | Nối trigger: Core đã tự gọi `TryEnterDuel` sau mỗi lệnh thành công; còn phía UI chạy transition rồi hiện Duel | `BossPhaseChangedEvent` + `ClashStartedEvent` + checkpoint ClashStart đã có |
| 4.3 | Eosphoros ally: **đã cắt khỏi gameplay** theo `Docs/QUYET_DINH.md` (không cần AllyBrain trong run) | nếu sau này muốn thêm lại thì làm thành cột mốc riêng |
| 4.4 | Chuỗi 5 ải: tiến trình, dialogue trước/sau, Ending A/B theo điều kiện | dùng `StageResult.EndingA/B` |
| 4.5 | Rewind flow UI: thua → prompt → `SnapshotService.Rewind()` → giữ RunKnowledge; hiển thị "đã thấy N chiêu boss"; rewind giữa Duel về đầu Duel | checkpoint StageStart/TurnStart/ClashStart |
| 4.6 | ✅ Đã dọn 2 method chết `CheckObjective`/`Finish` trong `GameActionExecutor.cs` | vệ sinh code |

**Nghiệm thu:** chơi 1→5 bằng dữ liệu thật; boss nào cũng chuyển Duel đúng; thắng/thua/rewind đúng; 2 ending mở được.

---

### P5 — Art, animation, audio ✅ **XONG 2026-11-19** (`Tools/slice_assets.py` & `Assets/_Project/Art/` + `Assets/Resources/`)

| # | Việc | Cách làm |
|---|---|---|
| 5.1 | Pipeline tách spritesheet: cắt 8×8 frame 128×128, **xoá nền xám bằng flood-fill từ biên**, trim, export PNG alpha | viết `Editor/SpriteSheetSlicer.cs` chạy 1 lệnh menu |
| 5.2 | Animation clip: idle (4 frame), attack, hit, death cho Wanderer/Eosphoros/Imp/Gargoyle; boss dùng sheet riêng, Malkor scale 2×2 | Animator đơn giản, không blend tree |
| 5.3 | Portrait: crop 1:1 → 512, import, hiện trong HUD/dialogue/duel intro | |
| 5.4 | Tileset: cắt tile, tạo Rule Tile hoặc tilemap thường cho 5 nền (Oakhaven, hầm, nghĩa địa, núi lửa, thánh đường) | tái dùng palette #E5C158/#2C4270/#6B3BA7/#8B1E1E/#3A3E45 |
| 5.5 | VFX: 3 sheet VFX cắt frame cho slash/holy/fire/poison/collapse/phase2; gắn vào Presenter | |
| 5.6 | Audio: BGM loop + crossfade khi vào boss/duel (`boss_theme` pitch thấp cho phase 2); SFX theo bảng event→sound; AudioMixer 3 group khớp Settings | music streaming, sfx decompress-on-load |
| 5.7 | UI skin từ Kenney pixel-adventure (9-slice), icon status | |
| 5.8 | **5 background Duel** 16:9 (`bg_oakhaven_ruins`, `bg_catacomb_toxic`, `bg_void_arena`, `bg_fire_arena`, `bg_mausoleum`) — cắt/mở rộng từ tileset + VFX, hoặc AI art | placeholder gradient trước, thay sau |
| 5.9 | Hiệu ứng transition: flash, vignette, khói/lửa, pose phase-2 cho boss | 1 prefab + Animator |

**Nghiệm thu:** 9 loại unit có animation; 5 nền bàn cờ + 5 background Duel; mọi event chính có SFX; BGM chuyển đúng khi vào boss/duel; không lẫn nền xám ở sprite.

---

### P6 — QA, balance, build (2–3 ngày)

| # | Việc |
|---|---|
| 6.1 | Bảng test tay: 5 ải × {thắng, thua, rewind}; Clash/Duel × {thắng, hoà, thua, kết liễu}; transition; settings; remap; dialogue; 2 ending |
| 6.2 | Balance: chạy simulation 100 ván/ải với chiến thuật tham lam, xem tỉ lệ thắng và số lượt; chỉnh trong `GameRules`/`StageDefinition`/`DuelDefinition`, không sửa Core |
| 6.3 | Performance: 60 FPS, không allocation nặng mỗi frame; kiểm tra GC alloc bằng Profiler |
| 6.4 | Build Windows x64 (IL2CPP hoặc Mono tuỳ thời gian), chạy thử trên máy sạch; kiểm tra log không lỗi |
| 6.5 | README: yêu cầu, cách chạy build, điều khiển, credit asset (Kenney, 0x72), cấu trúc repo |

**Nghiệm thu:** build `.exe` chạy độc lập; 0 exception; test matrix pass; `dotnet test` xanh.

---

### P7 — Đóng gói & báo cáo (1 ngày)

- Tag `v1.0.0`, release notes, ảnh chụp màn hình, video demo 2–3 phút (quay cả đoạn chuyển cảnh + Duel).
- Cập nhật `Docs/CORE_API_TONG_HOP.md` nếu API đổi; thêm `Docs/QUYET_DINH.md`, `Docs/THIET_KE_PHASE2_DUEL.md`.
- Slide/báo cáo đồ án (nếu cần).

---

## 4. Ước lượng & kịch bản

| Kịch bản | Nội dung | Thời lượng (1 người + agent) |
|---|---|---|
| **Demo 1 ải** | P0 + P1 (ải 1 + Duel ải 1, Duel Core đã xong) + P2 + HUD tối thiểu + DuelScreen khung | **~1 tuần** (7–8 ngày công) |
| **Bản chơi được 3 ải** | P0–P4 rút gọn (bỏ dialogue, settings nâng cao, VFX) | **~2,5 tuần** (13–15 ngày công) |
| **Bản đầy đủ 5 ải** | P0–P7 | **~4–5 tuần** (21–25 ngày công) |
| Full-time (6–8h/ngày) | P0–P7 | **~12–14 ngày** |

Đường găng: **P2 (presentation) → P3 (UI) → P4 (flow)**. P1 Content đã xong 2026-11-16 nên bước tiếp theo là dựng Unity board + input; P5 art chạy song song sau khi P2 có UnitView; P6 chỉ bắt đầu khi P4 xong.

---

## 5. Ưu tiên & cắt scope khi gấp

Giữ (điểm nhấn đồ án): Core + test, **Duel mode**, Rewind, 1 boss hoàn chỉnh.
Cắt được theo thứ tự: ải 4/5 → dialogue → settings nâng cao (remap) → VFX riêng → animation nhiều frame → menu đẹp. **Không cắt Duel** vì đây là cơ chế phase 2 đã chốt.

Nguyên tắc: **mỗi mốc phải demo được**, không làm "nửa chừng tất cả".

---

## 6. Rủi ro & cách xử lý

| Rủi ro | Mức | Cách xử lý |
|---|---|---|
| **Content trống là phần lớn nhất của khối lượng** | Cao | làm theo catalog nhỏ (ải 1 trước), test simulation sau mỗi catalog; không chờ đủ 5 ải mới test |
| Sprite sheet là JPG nền xám, tách nền bẩn | Cao | flood-fill từ biên + kiểm tra viền; nếu bẩn → dùng Kenney tiny-dungeon làm placeholder, art thật làm sau |
| TMP thiếu dấu tiếng Việt | Cao | chọn font hỗ trợ tiếng Việt ngay P0, test chuỗi có dấu trước khi làm UI |
| Chuyển cảnh + background 5 ải tốn art | Trung bình | 1 prefab transition dùng chung; background placeholder gradient trước, thay sau |
| Đang mở 2 Unity Editor khoá project | Trung bình | đóng bớt hoặc dùng bản copy `_core_check`; agent cần Editor mở để dựng scene/prefab |
| Scope creep (5 ải + animation đẹp + WebGL) | Cao | chốt DoD §1, cắt theo §5 |
| Balance tốn thời gian | Trung bình | simulation harness + bảng số đã chốt, không chỉnh cảm tính |
| Unity 6000.6.7f1 mới, API lạ | Thấp | bám API ổn định, tránh experimental |

---

## 7. Việc tôi làm được vs việc bạn phải quyết

**Tôi làm được (không cần bạn):**
- Toàn bộ C# lớp Core mở rộng (Duel), Content, Game, UI, Editor; test; sửa Core nhỏ nếu cần.
- Import/slice art bằng Editor script; tạo animation clip; setup prefab & scene qua Unity tools.
- Chạy `dotnet test`, Unity batchmode test, build, kiểm log.
- Viết README, docs, release notes.

**Bạn phải quyết/duyệt:**
- Deadline nộp, số người tham gia, ai làm art/background Duel.
- Chấp nhận art từ sheet có sẵn hay đầu tư vẽ mới.
- Cảm giác điều khiển/độ khó (playtest).
- Có cần WebGL/2 người chơi/tiếng Anh không.

---

## 8. Câu hỏi cần bạn trả lời (chặn việc lên lịch chính xác)

**Lịch & scope:**
1. **Deadline** nộp là ngày nào?
2. **Mấy người** làm? Bạn code cùng không, hay tôi code toàn bộ phần mềm còn bạn lo art/nội dung?
3. Nền tảng: chỉ **Windows build**, hay cần **WebGL** nữa?
4. Chốt **5 ải + 2 ending** ngay, hay làm **demo 1–3 ải** trước rồi mở rộng?
5. **Eosphoros đồng minh**: đã chốt **cắt khỏi gameplay** (không xuất hiện trong run, chỉ còn trong portrait/dialogue nếu muốn). Nếu bạn đổi ý, cần thêm ~1 ngày cho AllyBrain + test.

**Duel mode (mặc định đang dùng trong spec — không trả lời = giữ nguyên):**
6. HP **mang theo** khi vào Duel (không hồi) và **clear status** cả 2 bên?
7. **Win = Stagger boss + 1 đòn kết liễu** (chưa trừ HP trực tiếp), **Hoà = mỗi bên mất 10% uy lực** (đã chốt m01481), **Thua = ăn đúng uy lực boss**?
8. Duel **không áp turn limit** (đánh tới khi một bên gục), 5 background riêng + 1 prefab transition dùng chung?

---

## 9. Lịch gợi ý (bản đầy đủ, part-time 4h/ngày)

| Tuần | Mốc | Demo được gì |
|---|---|---|
| 1 | P0 ✅ + P1 ✅ (Content 30 file, 75 test) | Ải 1 chơi trọn từ bàn cờ → Duel → Victory bằng test, không cần Unity |
| 2 | P2 + bắt đầu P5 | Ải 1 chơi được trong Unity bằng chuột/phím, có animation cơ bản; DuelScreen khung hiện đúng lúc |
| 3 | P3 + P5 | Chơi ải 1 trọn vẹn bằng HUD/menu + transition + Duel UI |
| 4 | P4 + P6 | Chơi 1→5, rewind, 2 ending; build Windows đầu tiên |
| 5 | P7 + polish | Bản nộp: build sạch, video demo, báo cáo |
| 6 | Buffer | Dư thời gian cho art Duel/background, balance, sửa bug |

---

## 10. Việc làm ngay (khi bạn chốt lịch §8)

1. ✅ **P0 + P1 đã xong**: 4 asmdef (Core/Content/Tests), `Docs/QUYET_DINH.md`, Content 30 file (catalog/behaviour/brain/stage/duel/dialogue), **65/75 test xanh** cả `dotnet test` và Unity EditMode.
2. **P2 — Unity presentation**: `Game.unity` + `GameBootstrap` + `BoardView` (tile 1-bit) + `UnitView` (sprite sheet 128px) + `InputController` (chuột + phím) + `CameraRig`; chạy ải 1 trong Editor (P2.1–P2.7).
3. **P3 — UI**: HUD (HP/AP/chiêu), Clash/Duel UI 3 badge, menu, settings, dialogue panel, ending (P3.1–P3.4).
4. **P4 — flow**: nối transition, 5 ải, rewind prompt, 2 ending; PlayMode smoke.
5. Chạy Unity EditMode 75/75 + PlayMode smoke, quay video ải 1 (gồm chuyển cảnh + Duel) làm mốc demo.

---

## 11. Phase 2 — Duel mode (đã chốt, tóm tắt)

> Chi tiết: `THIET_KE_PHASE2_DUEL.md`.

- **Kích hoạt**: boss <70% HP sau khi một đòn resolve → transition ~1,2 s (rung + flash + `boss_roar` + `transform_dark` + fade) → màn Duel.
- **Bố cục**: background full-screen, player trái, boss phải; **không lưới, không AP, không End Turn**.
- **Vòng đấu**: boss công bố chiêu → player chọn 1/3 chiêu Clash (badge dự đoán) → Win: boss Stagger + 1 đòn kết liễu; Draw: mỗi bên chip 10%; Lose: ăn damage boss + status.
- **HP mang theo**, status clear khi vào, turn limit không áp, rewind về đầu Duel giữ RunKnowledge.
- **Dữ liệu**: 5 `DuelDefinition` (ải 1–5) với boss power 15/14/16/18/18 và 3 lựa chọn mỗi ải; bảng chi tiết trong spec.
- **Ước lượng riêng**: Core Duel ✅ xong (+9 file, 13 test). Còn Content +1, UI +1,5–2, Art +1 → phần Duel còn lại ~**+3,5–4 ngày công**, đã tính vào §4.
