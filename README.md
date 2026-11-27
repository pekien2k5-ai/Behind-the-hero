# Behind the Hero

Game chiến thuật theo lượt (turn-based tactics) 1v1 kết hợp giải đố không gian (spatial grid puzzle), làm bằng Unity 2D — đồ án Nhóm 10, lớp 65CNTT.

Người chơi điều khiển **Wanderer (Kẻ Lang Thang)** qua 5 ải, mỗi ải đối đầu một Boss với cơ chế giải đố địa hình riêng ở **Phase 1 (Bàn cờ)** và bước vào màn đấu chiêu trực diện **Phase 2 (Duel Mode)** khi máu Boss xuống dưới 70%, kết hợp cơ chế **Tua ngược thời gian (Rewind)** khi gục ngã và **2 nhánh kết thúc (Ending A / Ending B)** ở cuối Ải 5.

## Trạng thái

| Phần | Trạng thái |
|---|---|
| **Core (`BTH.Core`)** — luật chơi (C# thuần, không dùng `UnityEngine`) | ✅ 163 file, **98/98 unit test xanh** (`dotnet test` & Unity EditMode) |
| **Content (`BTH.Content`)** — đơn vị, chiêu, AI, 8 địa hình, 5 màn, 5 Duel, thoại 3 hồi | ✅ 9 unit, 25 chiêu + behaviour (đã thống nhất `blade-guard`), 8 brain (hỗ trợ LOS & bảo toàn địa hình), 8 địa hình tương tác, 5 màn, 5 Duel cân bằng cả vòng lẻ & chẵn, 10 đồ thị thoại riêng |
| **Unity (`BTH.Game` & `BTH.Editor`)** — Main Menu, Lore 3 Hồi, Hướng dẫn, Credits, Pause, Cutscene hoạt ảnh, bàn cờ Tileset, Spritesheet 4 trạng thái, VFX, Input, Audio | ✅ Đã hoàn thành (`GameBootstrap`, `CutsceneController`, `ArtCatalog`, `UnitSpriteAnimator`, `VfxPopupManager`, `AudioCatalog`) |
| **UI (`BTH.UI`)** — Main Menu, Pause Menu, Sổ tay Chiêu đã học (`[K]`), HUD (Cooldown, AoE Cross preview, Combat Log), Cutscene nhân vật, Phase 2 Duel (`[H]` Gợi ý & Combo 3 hit), Cài đặt (Resolution/Fullscreen) | ✅ Đã hoàn thành + PlayMode Test (`GameFlowPlayModeTests`) |
| **Art & Audio (`Assets/_Project/Art` & `Assets/Resources`)** — Spritesheets, Tileset, VFX, Portraits, BGM/SFX | ✅ 9 Spritesheets (`Idle/Walk/Attack/Death`), 25 Tiles + Atlas, 12 VFX strips, 7 Portraits, 5 Duel Backgrounds, 2 BGM + 14 SFX |

## Cấu trúc

```
Assets/
  Resources/
    Art/           # Bộ ảnh PNG đã tách nền nạp tự động ở runtime (Spritesheets, Tilesets, VFX, Portraits, Backgrounds)
    Audio/         # Nhạc nền BGM (forest_theme, boss_theme) và 14 hiệu ứng âm thanh SFX
  _Project/
    Core/          # BTH.Core — luật chơi, C# thuần, không tham chiếu UnityEngine
      Grid/        #   bàn cờ, địa hình, tầm nhìn, hình dạng vùng
      Units/       #   quân, faction, footprint, registry, A*
      Turn/        #   pha lượt, AP, thứ tự hành động
      Actions/     #   request, validator, executor
      Combat/      #   damage, effect, status, element, skill, clash, duel
      Ai/          #   enemy intent, telegraph, boss phase
      Run/         #   rules, stage, snapshot, event bus, session, dialogue
    Content/       # BTH.Content — dữ liệu game + hành vi chiêu/AI/địa hình, không tham chiếu UnityEngine
      Units/       #   9 unit definition (Wanderer, Eosphoros, Imp, Gargoyle, 5 Boss)
      Skills/      #   26 chiêu + behaviour (chữ thập, xuyên giáp, dịch chuyển, hộ kiếm...)
      Brains/      #   8 bộ não AI tất định + TelegraphedZoneEffect + StepTowardsEffect
      Stages/      #   5 màn chơi (8×8, 9×9, 9×9, 10×10, 8×8) & cơ chế câu đố địa hình
      Duels/       #   5 ClashDefinition (Phase 1) + 5 DuelDefinition (Phase 2)
      Dialogues/   #   cốt truyện 3 hồi + thoại trước/sau 5 ải + 2 nhánh Ending A/B
      Terrain/     #   8 loại địa hình (floor, wall, pit, cracked, poison, spike-trap, lava, chest hồi máu)
    Game/          # BTH.Game — lớp hiển thị Unity 2D, điều khiển, hoạt ảnh, VFX, âm thanh, cài đặt
    UI/            # BTH.UI — bảng màu Dark Fantasy & presenter giao diện
    Editor/        # BTH.Editor — cấu hình tự động Pixel Art & menu công cụ Unity Editor
    Art/           # Tài nguyên đồ họa gốc đã xử lý tách nền (Spritesheets, Tilesets, VFX, Portraits, Backgrounds)
  Tests/           # BTH.Core.Tests (98 EditMode/dotnet tests) + PlayMode/ (GameFlowPlayModeTests)
Tools/
  CoreTests/       # project dotnet để chạy test nhanh ngoài Unity (~90 ms)
  slice_assets.py  # script Python tách nền & chuẩn hóa Spritesheets, Tileset, VFX, Portraits, Backgrounds
Docs/
  KIEN_TRUC_UNITY_BEHIND_THE_HERO.md   # kiến trúc 4 lớp
  CORE_API_TONG_HOP.md                 # bảng tra class/hàm cho đồng đội
  THIET_KE_PHASE2_DUEL.md              # đặc tả cơ chế Phase 2 Duel Mode
  QUYET_DINH.md                        # các quyết định thiết kế đã chốt
  KE_HOACH_HOAN_THANH_GAME.md          # tiến độ triển khai các pha P0–P6
  BAO_CAO_CV3_NGUYEN_XUAN_LANG.md      # báo cáo chi tiết Spritesheets, Tileset, 5 màn chơi & UI/HUD
```

Chi tiết kiến trúc: [Docs/KIEN_TRUC_UNITY_BEHIND_THE_HERO.md](Docs/KIEN_TRUC_UNITY_BEHIND_THE_HERO.md).
Tra nhanh API: [Docs/CORE_API_TONG_HOP.md](Docs/CORE_API_TONG_HOP.md).
Báo cáo Đồ họa, 5 Màn chơi & Giao diện: [Docs/BAO_CAO_CV3_NGUYEN_XUAN_LANG.md](Docs/BAO_CAO_CV3_NGUYEN_XUAN_LANG.md).

## Cách chơi trong Unity Editor

1. Mở project bằng **Unity 6000.6.7f1** (mở `Assets/Scenes/SampleScene.unity`).
2. Bấm nút **Play (▶)** ở thanh công cụ phía trên (hoặc vào menu `Behind the Hero > 2. Chạy Game Ngay (Play Mode)`).
3. `GameBootstrap` sẽ tự động khởi tạo bàn cờ, nhân vật, Boss, âm thanh và giao diện HUD:
   - **Phím tắt & Chuột:** `[G]` Di chuyển (`Move`), `[S]` Đánh thường (`Slash`), `[1]` Trảm Tốc (`Swift Slash`), `[2]` Bùng Nổ Hoại Tử (`Necrotic Burst`), `[E]` / `[3]` Bước Nhảy Thời Gian (`Temporal Step`), `[D]` Phòng thủ (`Guard`), `[R]` Tua ngược thời gian (`Rewind`), `[X]` Chơi lại màn (`Reset`), `[Space]` Kết thúc lượt / Tiếp tục hội thoại.

## Chạy test

**Cách 1 — ngoài Unity (nhanh, ~90 ms):**

```powershell
dotnet test Tools/CoreTests/CoreTests.csproj
```

Không có .NET SDK? Dùng bản Unity đang cài:

```powershell
& "D:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" test Tools/CoreTests/CoreTests.csproj
```

**Cách 2 — trong Unity:** mở project bằng Unity **6000.6.7f1**, vào `Window > General > Test Runner > EditMode > Run All` (**81/81 Passed**).

## Nguyên tắc

- `BTH.Core` **không** được `using UnityEngine` (`noEngineReferences: true` trong asmdef).
- Mọi thay đổi state đi qua `ActionRequest → IActionValidator → IGameActionExecutor`.
- Tất định: RNG có seed, sort luôn có tie-break, snapshot khôi phục được từng trường.
- Thêm chiêu / quái / màn chơi **không sửa Core** — chỉ thêm dữ liệu và behaviour ở lớp Content.
- UI nghe event để vẽ, không tự tính luật.
