# BEHIND THE HERO — Kiến trúc mã nguồn Unity (v2.0)

> Tài liệu kiến trúc & phân rã tính năng, dựng từ thiết kế game trong `65CNTT_Nhom10_Tai_lieu_thiet_ke.docx`
> (Nhóm 10 – 65CNTT: Ngô Quý Kiên, Nguyễn Tuấn Kiệt, Nguyễn Xuân Lăng, Phạm Thị Huyền Linh).
>
> **Cập nhật 2026-11-16:** đối chiếu với code thật — `BTH.Core` (163 file) + `BTH.Content` (30 file) đã hoàn tất **65 unit test**
> (gồm **Duel mode** cho Phase 2 với auto finisher); Unity/UI vẫn là kế hoạch. Các chỗ ghi ✅ là đã có trong code;
> ⬜ là còn phải làm. Tài liệu API chi tiết song song: `CORE_API_TONG_HOP.md`.

---

## 0. Mục lục

| # | Chương | Nội dung |
|---|--------|----------|
| 1 | Nguyên tắc kiến trúc | 12 quy tắc bắt buộc, cách kiểm tra |
| 2 | Kiến trúc 4 lớp | Core → Content → Unity → UI, asmdef, ai giữ sự thật |
| 3 | Cây thư mục | Cây thật của Core, cây dự kiến Content/Unity/UI, bảng tra "sửa X ở đâu" |
| 4 | Phân rã tính năng | F00–F21, cột trạng thái ✅/⬜, ghi chú thiết kế |
| 5 | Hợp đồng Core | Từng nhóm kiểu, thành viên chính — đối chiếu API thật |
| 6 | Luồng xử lý | Vòng lượt, Clash, **Duel Phase 2**, chết & rewind, frame Unity |
| 7 | Mapping dữ liệu | ScriptableObject ↔ POCO, gồm DuelDataAsset |
| 8 | Bảng số liệu | Đơn vị, ma trận Clash, bảng Duel, hằng số, 5 màn |
| 9 | Công thức mở rộng | Thêm skill/quái/boss/màn/địa hình/duel không sửa Core |
| 10 | Lộ trình | Đã xong gì, còn gì, DoD |
| 11 | Kiểm thử & bảo trì | 65 test/10 bộ, lệnh chạy, checklist review, anti-pattern |
| 12 | ADR & quyết định | 7 ADR + danh sách đã chốt |

---

## 1. Nguyên tắc kiến trúc (bắt buộc)

| # | Nguyên tắc | Lý do / cách thực thi |
|---|-----------|------------------------|
| 1 | **Core là C# thuần**, không `using UnityEngine` | Chạy EditMode test và CI; đổi engine không phải viết lại luật. ✅ Thực thi bằng asmdef `noEngineReferences: true`. |
| 2 | **Một chiều phụ thuộc**: UI → Unity → Content → Core | Core không biết lớp trên. Kiểm bằng asmdef, không bằng lời hứa. |
| 3 | **Mọi thay đổi trạng thái đi qua Command** (`ActionRequest` → Validator → Executor) | Có log → test/rewind dễ; không có "code chui" sửa state từ UI. ✅ |
| 4 | **Tất định**: RNG có seed, AI sort ổn định | Clash/Duel không RNG; cùng input → cùng output, nếu không rewind sẽ lệch. ✅ |
| 5 | **Không dùng random/Time/Find trong Core** | Cấm `Random.Range`, `Time.deltaTime`, `FindObjectOfType`, `Instantiate` trong `BTH.Core`. ✅ |
| 6 | **Dữ liệu là asset, không phải code** | Chỉ số quái, skill, map, ma trận nằm trong ScriptableObject; Core chỉ thấy POCO (`UnitDefinition`, `StageDefinition`, `DuelDefinition`…). |
| 7 | **UI nghe event, không query ngược** | `IEventBus` phát `UnitMoved`, `DamageDealt`… Presenter tự cập nhật view. ✅ 28 event. |
| 8 | **Một composition root** (`GameSession`; Unity sẽ là `GameBootstrap`) | Không singleton rải rác; service inject qua constructor. ✅ |
| 9 | **Một file = một type** | Tìm bằng tên type; 163 file Core là hệ quả trực tiếp. |
| 10 | **ID dạng chuỗi** (`ElementId`, `TerrainId`, `SkillId`, `BrainId`) thay vì enum | Thêm hệ/địa hình/skill = thêm asset, không sửa Core. ✅ |
| 11 | **Hàm core thuần hoặc nhận state tường minh** | Không đọc static/global; dễ test và snapshot. ✅ |
| 12 | **Mọi con số cân bằng nằm trong `IGameRules`/definition** | Không hard-code 0.7 / 2 / 200% trong logic. ✅ (Duel: `DrawChipRatio`, `FinisherCount`) |

**Phân biệt 3 loại "core" trong tài liệu này**

| Loại | Nghĩa | Ví dụ |
|------|-------|-------|
| **Hợp đồng** | Interface + POCO + thuật toán nền, không biết nội dung cụ thể | `IGridMap`, `IClashResolver`, `IClashDuel`, `GameSession` |
| **Triển khai của Core** | Lớp cụ thể hoá hợp đồng, vẫn C# thuần | `AStarPathfinder`, `SnapshotService`, `ClashResolver`, `ClashDuel` |
| **Content** | Nội dung gameplay: skill nào, boss nào, địa hình nào | `SwiftSlashBehaviour`, `MorbidaPhase2`, `CrackedFloorTerrain` |
| **UI / Presentation** | MonoBehaviour, view, presenter, hiệu ứng | `HudPresenter`, `DuelScreenView`, `UnitView` |

> Quy tắc vàng: **thêm nội dung mới chỉ động vào Content/UI; thêm luật chơi mới mới động vào Core.**

---

## 2. Kiến trúc 4 lớp

```
┌─────────────────────────────────────────────────────────────┐
│ 4. BTH.UI          HUD, Clash/Duel UI, Menu, Settings        │  uGUI + TMP
│                    Presenter (MonoBehaviour)                 │
├─────────────────────────────────────────────────────────────┤
│ 3. BTH.Unity       Bootstrap, Adapter, View, Input, Audio    │  Unity runtime
│                    ScriptableObject ↔ POCO                   │
├─────────────────────────────────────────────────────────────┤
│ 2. BTH.Content     Skill, Terrain, Brain, Boss, Stage, Duel  │  nội dung game
│                    (implementation of Core contracts)        │
├─────────────────────────────────────────────────────────────┤
│ 1. BTH.Core        Grid, Units, Turn, Actions, Combat,       │  C# thuần 100%
│                    Clash/Duel, AI contract, Rewind, Event    │
└─────────────────────────────────────────────────────────────┘
         ▲ mũi tên phụ thuộc chỉ đi lên, không bao giờ đi xuống
```

### 2.1 Trách nhiệm từng lớp

| Lớp | Được làm | Không được làm |
|-----|----------|----------------|
| **Core** | Luật chơi, state, thuật toán, event | Biết prefab, sprite, âm thanh, input, coroutine |
| **Content** | Cài đặt cụ thể skill/AI/terrain/boss/duel; đọc `StageDefinition` | Gọi `GameObject`, chạm UI |
| **Unity** | Chuyển event → hình ảnh/âm thanh; input → `ActionRequest`; SO → POCO | Chứa luật chơi, tự sửa `Hp`, tự tính sát thương |
| **UI** | Hiển thị, chọn lệnh, dự báo Clash/Duel | Tự quyết định kết quả, tự trừ máu |

### 2.2 Assembly Definition (trạng thái thật)

| asmdef | Trạng thái | Ghi chú |
|--------|-----------|---------|
| `Assets/_Project/Core/BTH.Core.asmdef` | ✅ có | `noEngineReferences: true`, `autoReferenced: false`, không reference nào |
| `Assets/Tests/BTH.Core.Tests.asmdef` | ✅ có | refs `BTH.Core` + `UnityEngine.TestRunner` + `UnityEditor.TestRunner`; `overrideReferences` + `nunit.framework.dll`; `defineConstraints: ["UNITY_INCLUDE_TESTS"]`; **`includePlatforms: ["Editor"]`** (thiếu dòng này Unity xếp nhầm sang PlayMode → 0 test) |
| `Content/BTH.Content.asmdef` | ⬜ | refs `BTH.Core` |
| `Game/BTH.Unity.asmdef` | ⬜ | refs `BTH.Core`, `BTH.Content`, `Unity.InputSystem` |
| `UI/BTH.UI.asmdef` | ⬜ | refs `BTH.Core`, `BTH.Unity`, `Unity.TextMeshPro` |
| `Editor/BTH.Editor.asmdef` | ⬜ | refs tất cả, `includePlatforms: ["Editor"]` |

Bật `noEngineReferences` cho `BTH.Core` là **cách rẻ nhất để cấm** lớp 1 lén dùng `UnityEngine`.

### 2.3 Ai giữ "sự thật"?

| Dữ liệu | Nơi giữ | Ghi chú |
|---------|---------|---------|
| Máu, AP, vị trí, trạng thái | `UnitState` (Core) | View chỉ đọc để vẽ |
| Địa hình, vật cản, ô khoá | `CellState` trong `IGridMap` | Đổi địa hình phát `CellChangedEvent` |
| Ý định của quái (telegraph) | `EnemyIntent` (Core) | Sinh trước pha Player, thi hành ở pha Enemy |
| Kết quả Clash | `ClashResolver` (Core) | UI chỉ hiển thị dự báo |
| **Trạng thái Duel** | `DuelState` + `ClashDuel` (Core) | Round, chiêu boss hiện tại, người thắng |
| Seed RNG | `GameSnapshot` | Rewind khôi phục cả seed |
| Cài đặt âm thanh/hiển thị | `SettingsModel` (Unity) | Không nằm trong snapshot |
| Kiến thức về boss | `IRunKnowledge` (Core, **ngoài snapshot**) | Chết vẫn nhớ chiêu boss — đúng cốt truyện |

---

## 3. Cây thư mục

### 3.1 Core — cây thật (163 file)

```
Assets/_Project/Core/                     163 file · 7.366 dòng
├── BTH.Core.asmdef
├── AssemblyInfo.cs                       InternalsVisibleTo("BTH.Core.Tests")
├── Grid/          7 file ·   633 dòng   GridCoord, TerrainId, TerrainFlags, CellState,
│                                        IGridMap, GridMap, GridShapes
├── Units/        18 file · 1.060 dòng   UnitId, UnitFaction, UnitArchetype, MoveProfile,
│                                        UnitDefinition, UnitState, IUnitRegistry, UnitRegistry,
│                                        OccupancyIndex, PathStatus, PathRequest, PathResult,
│                                        ReachableSet, PathNode, PathMinHeap, IPathfinder,
│                                        AStarPathfinder, MovementRangeCalculator
├── Turn/          6 file ·   431 dòng   TurnPhase, ActionPoint, IInitiativePolicy,
│                                        InitiativePolicy, ITurnManager, TurnManager
├── Actions/      15 file ·   911 dòng   ActionKind, ActionError, ActionRequest,
│                                        ValidationResult, ActionResult, IGameAction,
│                                        MoveAction, AttackAction, SkillAction, GuardAction,
│                                        EndTurnAction, IActionValidator, ActionValidator,
│                                        IGameActionExecutor, GameActionExecutor
├── Combat/       51 file · 2.113 dòng   ElementId, ElementMatrix, DamageRequest/Result,
│                                        CombatMath, IDamageCalculator, DamageCalculator,
│                                        SkillDefinition/Targeting/Library/Behaviour,
│                                        IGameEffect, EffectQueue, EffectContext, 10 effect,
│                                        StatusId/StatusIds/IStatusEffect/StatusContext/
│                                        StatusEntry/StatusSet + 5 status,
│                                        ClashRule/ClashRuleTable/ClashDefinition/ClashOutcome/
│                                        ClashPrediction/ClashOption/IClashResolver/ClashResolver,
│                                        ClashMath, ClashRoundResult, DuelDefinition, DuelState,
│                                        IClashDuel, ClashDuel
├── Ai/            8 file ·   268 dòng   EnemyIntent, IEnemyBrain, IEnemyBrainLibrary,
│                                        EnemyBrainLibrary, ITelegraphService, TelegraphService,
│                                        IBossPhaseController, BossPhaseController
└── Run/          57 file · 1.946 dòng   GameRules/IGameRules, IRandomSource, XorShiftRandom,
                                         GameState, IGameStateView, GameSession,
                                         StageDefinition, UnitSpawnDefinition, TerrainOverride,
                                         IObjectiveEvaluator, ObjectiveEvaluator, StageResult,
                                         DialogueNode/Choice/Graph/Runner,
                                         ISnapshotStore/SnapshotStore/ISnapshotService/SnapshotService,
                                         SnapshotTrigger, GameSnapshot, UnitSnapshot,
                                         StatusSnapshot, DuelSnapshot, IRunKnowledge, RunKnowledge,
                                         IEventBus/EventBus, RefEventHandler<T>,
                                         + 28 event struct
```

### 3.2 Tests — cây thật (10 file · 1.114 dòng)

```
Assets/Tests/
├── BTH.Core.Tests.asmdef
├── TestWorld.cs                 (helper dựng grid/registry/state/stage)
├── GridTests.cs                 8 test
├── PathfindingTests.cs          6 test
├── TurnFlowTests.cs             5 test
├── DamageTests.cs               6 test
├── ClashTests.cs                7 test
├── AiDeterminismTests.cs        4 test
├── SnapshotTests.cs             4 test
├── StageObjectiveTests.cs       7 test
└── DuelFlowTests.cs             11 test
```

### 3.3 Dự kiến Content / Game / UI / Editor (chưa tạo)

```
Assets/_Project/
├── Content/            # BTH.Content
│   ├── Data/           # *.asset: Unit, Skill, Stage, Terrain, Clash, Duel, Dialogue
│   ├── Skills/         # ISkillBehaviour
│   ├── Enemies/        # IEnemyBrain
│   ├── Bosses/         # boss brain + phase controller
│   ├── Terrain/        # ITerrainBehaviour
│   └── Story/          # DialogueGraph
├── Game/               # BTH.Unity — bootstrap, view, input, audio, vfx, SO adapter
├── UI/                 # BTH.UI — HUD, Clash/Duel screen, dialogue, menu, settings
├── Editor/             # BTH.Editor — DataValidator, stage tool
├── Art/, Audio/, Scenes/, Prefabs/
```

**Quy ước:**

1. Tối đa **2 cấp** dưới `Assets/_Project`; cấp 3 phải có lý do trong PR.
2. **1 file = 1 type**; tên file trùng tên type.
3. Folder dưới 3 file thì gộp lên cha; file trên ~400 dòng mới tách.
4. Không tạo folder theo tính năng (F01, F02…).
5. Code Unity không nằm trong `Core/`; dữ liệu không nằm trong `Game/`.
6. Tiền tố miền khi cần: `GridMap.cs`, `ClashDuel.cs`, `BossMalkor.cs`.

### 3.4 Bảng tra nhanh — "muốn sửa X thì vào đâu"

| Muốn sửa | Folder | File tiêu biểu |
|----------|--------|----------------|
| Kích thước bàn, tường, ô nứt, LOS | `Core/Grid` | `GridMap.cs`, `CellState.cs` |
| Đường đi, vùng di chuyển | `Core/Units` | `AStarPathfinder.cs`, `MovementRangeCalculator.cs` |
| HP/ATK/MOV, boss 2×2 | `Core/Units` + `Content/Data` | `UnitRegistry.cs`, `UnitDataAsset` |
| Thứ tự lượt, AP, turn limit | `Core/Turn` | `TurnManager.cs`, `GameRules.cs` |
| Nút bấm nào hợp lệ, vì sao chặn | `Core/Actions` | `ActionValidator.cs` |
| Sát thương, hệ, Stagger/Guard | `Core/Combat` | `DamageCalculator.cs`, `StatusSet.cs` |
| Chiêu boss, Clash pha 1 | `Core/Combat` + `Content/Bosses` | `ClashResolver.cs`, `ClashRuleTable.cs` |
| **Duel pha 2** | `Core/Combat` | `ClashDuel.cs`, `DuelDefinition.cs`, `DuelState.cs` |
| Chết/tua lại, RESET | `Core/Run` | `SnapshotService.cs`, `GameSnapshot.cs` |
| Thắng thua, mục tiêu màn | `Core/Run` | `ObjectiveEvaluator.cs` |
| HUD, Clash/Duel UI, hội thoại | `UI` | `HudPresenter.cs`, `DuelScreenPresenter.cs` |
| Nhập chuột/phím, camera, âm thanh | `Game` | `UnityInputSource.cs`, `UnityAudioService.cs` |

---

## 4. Phân rã tính năng

### 4.1 Bảng tổng hợp

Trạng thái cột **Core**: ✅ = đã có + test; ⬜ = chưa. Content/Unity/UI đều ⬜.

| ID | Tính năng | Core (hợp đồng + thuật toán) | Triển khai Core | Content / Unity / UI | Core |
|----|-----------|------------------------------|-----------------|----------------------|------|
| F00 | Khung game & composition root | `GameSession`, `IGameStateView` | `GameSession` | `GameBootstrap` | ✅ |
| F01 | Bàn cờ & địa hình | `IGridMap`, `GridCoord`, `CellState`, `TerrainId`, `GridShapes` | `GridMap`, `OccupancyIndex` | `TerrainCatalog`, `BoardView`, `CellView` | ✅ |
| F02 | Đơn vị & chỉ số | `UnitDefinition`, `UnitState`, `IUnitRegistry` | `UnitRegistry` | `UnitDataAsset`, `UnitView` | ✅ |
| F03 | Lượt, pha & AP | `ITurnManager`, `TurnPhase`, `ActionPoint` | `TurnManager`, `InitiativePolicy` | `DefaultGameRules`, `TurnBannerView` | ✅ |
| F04 | Di chuyển & A* | `IPathfinder`, `PathRequest/Result`, `ReachableSet` | `AStarPathfinder`, `MovementRangeCalculator` | `BoardHighlighter` | ✅ |
| F05 | Sát thương & hệ | `IDamageCalculator`, `DamageRequest`, `ElementMatrix` | `DamageCalculator`, `CombatMath` | `ElementMatrixAsset`, damage number | ✅ |
| F06 | Kỹ năng & effect pipeline | `IGameAction`, `SkillAction`, `IGameEffect`, `EffectQueue` | `ActionValidator`, `GameActionExecutor`, `EffectQueue` | `ISkillBehaviour` (Content) | ✅ |
| F07 | AI & Telegraph | `IEnemyBrain`, `EnemyIntent`, `ITelegraphService` | `TelegraphService` | `MeleeRusherBrain`, `RangedLurkerBrain` | ✅ |
| F08 | Clash pha 1 (trên bàn cờ) | `IClashResolver`, `ClashPrediction`, `ClashRuleTable` | `ClashResolver`, `ClashMath` | 5 `ClashDefinition`, `ClashScreenView` | ✅ |
| F09 | Trạng thái | `IStatusEffect`, `StatusSet` | `StatusContext`, `StatusSet`, 5 status | icon trên `UnitView` | ✅ |
| F10 | Chết & tua ngược / RESET | `GameSnapshot`, `ISnapshotStore`, `SnapshotTrigger` | `SnapshotService`, `SnapshotStore` | `IRunKnowledge`, nút RESET | ✅ |
| F11 | Màn chơi, mục tiêu, thắng–thua | `StageDefinition`, `IObjectiveEvaluator`, `StageResult` | `ObjectiveEvaluator` | 5 `StageDataAsset`, ending | ✅ |
| F12 | Cốt truyện, hội thoại, 2 kết thúc | `DialogueNode`, `DialogueGraph`, `DialogueRunner` | `DialogueRunner` | `DialogueAsset`, `DialogueView` | ✅ |
| F13 | HUD & bảng lệnh | chỉ event | — | `HudView/Presenter`, `CommandBarView` | ⬜ |
| F14 | Clash/Duel UI | `ClashPrediction`, `ClashOption` | — | `ClashScreenView`, `DuelScreenView`, badge | ⬜ |
| F15 | Menu & cài đặt | `ISettingsStore` (Unity) | — | `SettingsView`, `PlayerPrefsSettingsStore` | ⬜ |
| F16 | Âm thanh BGM/SFX | `IAudioService` (Unity) | — | `UnityAudioService`, `AudioCatalogAsset` | ⬜ |
| F17 | Điều khiển chuột + phím | `IInputSource` (Unity) | — | `UnityInputSource`, `SelectionController` | ⬜ |
| F18 | Hình ảnh, hoạt ảnh, VFX | event | — | `SpriteAnimator`, `VfxPlayer`, `BattleCamera` | ⬜ |
| F19 | Lưu & tải | `ISettingsStore` | — | — (không lưu tiến trình theo ADR-05) | ⬜ |
| F20 | Công cụ Editor & test | — | — | `DataValidator`, `StageEditorWindow` | ⬜ |
| F21 | **Duel Phase 2 (đấu chiêu 1v1)** | `IClashDuel`, `DuelDefinition`, `DuelState`, `ClashMath` | `ClashDuel` | `DuelCatalog`, `DuelScreenView` | ✅ |

### 4.2 Ghi chú thiết kế cho các tính năng then chốt

**F01 — Lưới nhiều tầng thông tin, không chỉ "ô trống/ô tường".**
Mỗi `CellState` chứa `Terrain`, `Flags`, `HazardDamagePerTurn`, `TurnsUntilCollapse`, `MovementCost`, `Occupant`, `IsLocked`.
Ô "sàn nứt" không chặn đi nhưng sập sau N lượt (`ArmCollapse` → `TickCollapse` biến thành `TerrainId.Pit` + `BlocksMove` + `CellCollapsedEvent`).
Hành vi địa hình đặc biệt do `ITerrainBehaviour` (Content) quyết định, **không** rải `if (kind == Cracked)` trong Core.

**F02 — Đơn vị có footprint.**
Malkor 2×2. `UnitState.Footprint` trả các ô theo `SizeX/SizeY`; **neo = góc min (X nhỏ nhất, Y nhỏ nhất)**, kéo theo +X/+Y. `IUnitRegistry.Add/MoveTo` kiểm `CanPlace`; `OccupancyIndex` map ô → `UnitId`. `GridShapes.Footprint` và các hình AoE giao với footprint, không giả định 1 unit = 1 ô.

**F03 — Bốn nhịp, không phải hai pha.**

```
TurnIndex N:
  1. Telegraph  : AI lập EnemyIntent + phát danger cells (chưa gây sát thương)
  2. Player     : người chơi dùng 2 AP (1 Move + 1 Action, thứ tự tự do)
  3. Enemy      : thi hành đúng intent đã công bố (không tính lại AI)
  4. TurnEnd    : tick status → tick hazard/collapse → TurnIndex++ → objective → checkpoint
```

Thi hành đúng intent đã báo là điều kiện để người chơi "tin" telegraph — và để test tự động.

**F07 — Telegraph là dữ liệu, không phải animation.**
`EnemyIntent` gồm `Unit`, `SkillId`, `TargetCell`, `DangerCells`, `Effects`, `Priority`.
`GameSession.PlanEnemyIntents()` công bố intent **trước** pha Player; pha Enemy chỉ resolve intent đã công bố.
Nhờ vậy UI vẽ viền đỏ từ dữ liệu; test khẳng định ô báo = ô bị đánh; snapshot khôi phục intent như state.

**F08 — Clash pha 1 tất định, có dự báo.**
Không RNG. `Predict()` và `Resolve()` dùng chung `ClashMath.EffectivePower` (power = `skill.Power + bonus`), so với `BossPower`: `>` Win, `==` Draw, `<` Lose.
`ClashRuleTable` khoá theo cặp `(playerSkillId, bossSkillId)` → fallback cặp element → `DefaultBonus = 3`.
`StageDefinition.Clash` giữ dữ liệu; `ActionRequest.ClashSkill` chỉ hợp lệ khi `TurnPhase.Clash`.

**F21 — Phase 2 là Duel mode, KHÔNG còn bàn cờ (quyết định mới).**
Khi boss xuống dưới ngưỡng (`Rules.Phase2Threshold`, mặc định 0.70):
`GameSession.EnterDuel()` → clear telegraph → `BossPhase.EnterPhase2` (đánh dấu pha + event) → `ClashDuel.Start(...)` → `Turn.BeginDuel()` (`TurnPhase.Duel`).
Trong Duel:

- Không lưới, không AP, không di chuyển, không End Turn; player bên trái, boss bên phải trên background.
- Mỗi vòng boss ra 1 chiêu (chu kỳ `BossSkillIds`); người chơi chọn 1 trong các chiêu Clash của mình.
- Win: boss ăn `WinEffect` (mặc định Stagger) → **tự đánh finisher** (`FinisherCount` đòn, sát thương = ATK × hệ số trạng thái, Stagger ×2) → tự sang vòng; không có nút Attack/Kết liễu.
- Draw: mỗi bên mất `round(power × DrawChipRatio)` (mặc định **10%**), bỏ qua giáp.
- Lose: người chơi nhận đúng `BossPower` + `LoseEffect`.
- HP mang theo từ pha 1; status 2 bên được clear khi vào Duel; hết vòng thì sang vòng kế; ai chết trước thì thua.
- `RoundLimit` > 0 là tuỳ chọn; mặc định 0 = không giới hạn vòng.

**F10 — Snapshot, không phải replay.**
Turn-based nên state nhỏ; chụp ảnh rẻ hơn replay (vốn đòi tất định tuyệt đối cả animation).
`GameSnapshot` gồm cells, units, status, AP, phase, result, RNG state, intents, **DuelSnapshot**.
Checkpoint: `StageStart` (đầu màn), `TurnStart` (mỗi lượt), `ClashStart` (khi vào Clash pha 1). `BeginDuel` hiện chỉ đổi phase, không đẩy checkpoint riêng; UI có thể tự `PushCheckpoint` trước khi gọi `EnterDuel` nếu muốn rewind về trước Duel.
`Restore(Capture())` phải bằng nhau từng trường (test bắt buộc).

**F11 — Thắng thua là hàm thuần.**
`ObjectiveEvaluator.Evaluate(state, stage)`: player chết → `DefeatPlayerDead`; boss chết → `Victory`; quá `TurnLimit` → `DefeatTurnLimit` (bỏ qua khi `TurnPhase.Duel`); ngược lại `None`.

**F12 — Hội thoại là dữ liệu.**
`DialogueNode` (Id, Speaker, Text, NextId, Choices, Ending), `DialogueGraph` (StartId, Add/TryGet/Validate), `DialogueRunner` (Start/Advance/Choose/Current/Ending). Unity chỉ hiển thị; ending A/B là lựa chọn dialogue.

**F13/F14 — UI chỉ bind.**
Presenter nghe event và set text/sprite; không đọc `UnitState` mỗi frame (trừ animation thanh máu), không tự tính AP/damage.

---

## 5. Hợp đồng Core (đối chiếu API thật)

> Cách đọc: `Ten(...)` = hàm; `Ten` = thuộc tính/field. Chữ ký đầy đủ xem `CORE_API_TONG_HOP.md`.

### 5.1 Grid & Terrain — Core/Grid

| Kiểu | Thành viên chính | Ai dùng |
|------|------------------|---------|
| `GridCoord` | `X, Y`, `None`, `IsValid`, `Manhattan(a,b)`, `Chebyshev(a,b)`, `+ - == !=`, `North/South/East/West` | tất cả |
| `TerrainId` | ID chuỗi: `Floor/Wall/Pit/Cracked/Poison/Lava/Chest/None`; so sánh OrdinalIgnoreCase; `(TerrainId)"id"` | GridMap, Content |
| `TerrainFlags` | `None, BlocksMove, BlocksLineOfSight, IsHazard, IsCover, IsInteractive` | GridMap, damage |
| `CellState` | `Coord, Terrain, Flags, HazardDamagePerTurn, TurnsUntilCollapse, MovementCost, Occupant, IsLocked`; `IsOccupied/BlocksMove/BlocksLineOfSight/IsHazard/IsInteractive` | mọi nơi đọc bàn cờ |
| `IGridMap` / `GridMap` | `Width/Height`, `InBounds`, `Get/TryGet`, `IsBlocked`, `IsWalkable(c, profile)`, `HasLineOfSight`, `MovementCost`, `IsLocked`, `SetTerrain(c, terrain, flags, movementCost, hazardDamage)`, `ArmCollapse`, `TickCollapse`, `LockArea/UnlockArea`, `SetOccupant`, event C# `CellChanged` | pathfinder, skill, AI |

**Hình học — `GridShapes` (static):** `Directions4`, `Neighbours4`, `Line` (Bresenham, gồm 2 đầu), `Cross` (Manhattan), `Square` (Chebyshev), `Cone(origin, dirX, dirY, length)` (|lateral| ≤ forward), `Row`, `Footprint(anchor, sizeX, sizeY)`, `Contains`.

**Bất biến:** không có `if (kind == Cracked)` rải rác — hành vi ô do `ITerrainBehaviour` (Content) quyết định.

### 5.2 Đơn vị & chỉ số — Core/Units

| Kiểu | Thành viên chính | Ai dùng |
|------|------------------|---------|
| `UnitId` | int, `None = 0`, `IsValid = Value > 0` | khắp nơi |
| `UnitFaction` / `UnitArchetype` | Player/Ally/Enemy/Neutral; Hero/Ally/Minion/Elite/StageBoss/FinalBoss | registry, brain |
| `MoveProfile` | `MovePoints, CanFly, IgnoresHazard, CanPassUnits`; `Walk(n)`, `Fly(n)` | pathfinder, GridMap |
| `UnitDefinition` (POCO settable) | `Id, DisplayName, Faction, Archetype, MaxHp, Attack, Armor, Initiative, SizeX/SizeY, Move, NativeElement, SkillIds, BrainId, PortraitId`; `IsBoss` | registry, SO loader |
| `UnitState` | `Id, Definition, Faction, Archetype, IsBoss, MaxHp, Hp, HpRatio, IsAlive, Position, ActionPoints, HasMovedThisTurn, HasActedThisTurn, Statuses, SizeX/SizeY, Footprint, Center, MovePoints, Move, IsStaggered/IsRooted/IsGuarding/IsFlanked, InCells(cells)` | action, AI, UI |
| `IUnitRegistry` / `UnitRegistry` | `All, Player, Boss, Alive(faction)`, `Get/TryGet/At`, `IsOccupied`, `CanPlace(def,anchor[,ignore])`, `Add/Remove/MoveTo`, `UnitsIn(cells)`, `AdjacentTo(cell)`, `Kill(id)` | action, AI, view |
| `OccupancyIndex` | map ô → `UnitId`; `Add/Remove/Move/TryGet/IsOccupied/At/Clear` | registry |

**Bất biến:** `UnitState` chỉ bị sửa bởi action/effect; UI không gán HP.

### 5.3 Lượt, pha & AP — Core/Turn

| Kiểu | Thành viên chính | Ghi chú |
|------|------------------|---------|
| `TurnPhase` | `None, StageIntro, Telegraph, Player, Enemy, TurnEnd, Clash, StageEnded, `**`Duel`** | 4 nhịp chính + Clash/Duel/StageEnded |
| `ActionPoint` | `Current, Max, IsEmpty, CanSpend(cost), Spend(cost)` | 2 AP/lượt = 1 Move + 1 Action |
| `IInitiativePolicy` / `InitiativePolicy` | `Order(units)` — Initiative ↓, hoà UnitId ↑, bỏ unit chết | tất định |
| `ITurnManager` / `TurnManager` | `Phase, TurnIndex, TurnLimit, ActiveUnit, EnemyOrder`, `StartStage(stage)`, `BeginTelegraph`, `BeginPlayerPhase`, `EndPlayerPhase`, `BeginEnemyPhase`, `EndEnemyPhase`, `EndTurn`, `BeginClash`, **`BeginDuel`**, `FinishStage(result)`, `CanSpend`, `TrySpend`, `ResetActionPoints`, `NextEnemy` | `EndTurn` chặn khi `Duel`/`IsOver` |

**Thứ tự `EndTurn()` (đúng như code):**

```
SetPhase(TurnEnd)
→ TickUnits: mỗi unit sống: StatusSet.TickTurnEnd → TickCooldowns → nếu còn sống TickTurnStart
→ TickTerrain: hazard trên footprint (bỏ IgnoresHazard) → Damage.Apply(ignoresArmor) → Grid.TickCollapse
→ TurnIndex++ + TurnAdvancedEvent
→ ObjectiveEvaluator.Evaluate
→ nếu StageResult != None: FinishStage
→ ngược lại: ResetActionPoints → PushCheckpoint(TurnStart) → BeginTelegraph
```

`GameSession` mới là nơi công bố intent + mở pha Player (xem §6.1).

### 5.4 Command / Action — Core/Actions

| Kiểu | Thành viên chính |
|------|------------------|
| `ActionKind` | Move, Attack, Skill, Guard, Wait, EndTurn, Reset, ClashSkill |
| `ActionError` | `None, UnknownActor, UnitDead, NotPlayerPhase, NotEnemyPhase, NoActionPoints, OutOfRange, TargetEmpty, NoLineOfSight, CellBlocked, CellOccupied, NotEnoughMovePoints, SkillNotReady, SkillOnCooldown, CannotMoveRooted, ActorStunned, StageEnded, InvalidTarget, ClashNotAvailable, ObjectiveAlreadyEnded, AlreadyMoved, AlreadyActed, `**`WrongPhase`** |
| `ActionRequest` | `Actor, Kind, TargetCell, SkillId, MoveTo`; factory `Move/Attack/Skill/Guard/EndTurn/ClashSkill` |
| `ValidationResult` | `IsValid, Error, Message`; `Ok()/Fail(error,message)` |
| `ActionResult` | `Success, Error, Message`; `Ok()/Fail(...)` |
| `IGameAction` | `Id, Kind, Actor, Target`; `MoveAction`, `AttackAction`, `SkillAction`, `GuardAction`, `EndTurnAction` |
| `IActionValidator` / `ActionValidator` | `Validate(request, state)` thuần; `GetValidTargets(request, state)`. Khi `TurnPhase.Duel` → `ValidateDuel` |
| `IGameActionExecutor` / `GameActionExecutor` | `Execute(request, state) → ActionResult`; publish event sau khi state ổn định; Duel chỉ có `ExecuteDuelClash` (finisher nằm trong `ClashDuel.Choose`) |

**Bất biến:** UI gọi `Validate` trước, chỉ khi `IsValid` mới gửi request; mọi thay đổi đi qua effect queue.

### 5.5 Hiệu ứng — Core/Combat

| Kiểu | Thành viên chính |
|------|------------------|
| `IGameEffect` | `Priority, Resolve(ctx)` |
| `EffectQueue` | `Enqueue`, `Run(ctx)` (stable theo priority, dừng nếu source chết), `Clear`, `Count` |
| `EffectContext` | `State, Queue, Units, Events, Random, Source, Origin, Target, TargetCell, Skill, Power, Element, TargetUnit, ResolvedCell, UnitAt(cell)` |
| effect có sẵn | `DamageEffect` (10), `HealEffect` (12), `ApplyStatusEffect` (20), `RemoveStatusEffect` (21), `PushEffect` (30), `TeleportEffect` (35), `SpawnTerrainEffect` (40), `ArmCollapseEffect` (41), `LockArenaEffect` (45), `CameraShakeEffect` (90) |
| `ISkillBehaviour` | `BuildEffects(ctx)` — chỉ xếp effect, không tự trừ HP |

**Bất biến:** effect không gọi view; hiển thị đi qua event.

### 5.6 Sát thương & ma trận hệ — Core/Combat

| Kiểu | Thành viên chính |
|------|------------------|
| `ElementId` | struct chuỗi: `None/Physical/Fire/Holy/Necrotic/Piercing/Temporal`, `==`, `IsEmpty` |
| `ElementMatrix` | `CreateDefault()`, `Set(attack, defend, mult)`, `GetMultiplier`, `Clone` |
| `DamageRequest` | `Source, Target, BasePower, Element, IsSkill, IgnoresArmor, HitFrom` |
| `DamageResult` | `FinalDamage, Multiplier, WasStaggered, WasGuarded, WasFlanked, Killed, IsZero` |
| `IDamageCalculator` / `DamageCalculator` | `Compute(request, state)`, `Apply(request, state)` — apply trừ HP, `NotifyDamaged`, publish `DamageDealtEvent`, gọi `Units.Kill` nếu chết |
| `CombatMath` | `ComputePower(base, armor, elementMult, ignoresArmor, statusMult)`, `CombineMultipliers` |

Công thức: `max(1, round(max(1, base × hệ − giáp) × statusMult))`; nếu `IgnoresArmor` bỏ bước trừ giáp.

### 5.7 Trạng thái — Core/Combat

| Kiểu | Thành viên chính |
|------|------------------|
| `StatusId` / `StatusIds` | struct chuỗi; `Stagger, Flanked, Root, Poison, Guard` |
| `IStatusEffect` | `Id, Duration, DamageTakenMultiplier, PreventsAction, PreventsMovement`; `OnApply/OnTurnStart/OnTurnEnd/OnRemoved/OnDamaged` |
| `StatusSet` | `Has(id)/Has<T>`, `Get`, `GetRemaining`, `Add(effect, ctx)`, `Remove`, `Clear`, `TickTurnStart/TickTurnEnd`, `NotifyDamaged`, `DamageTakenMultiplier`, `PreventsAction/Movement`, `IsStaggered/IsRooted/IsGuarding/IsFlanked`, `Count`, `Effects` |
| 5 status | `StaggerStatus` (1 lượt, ×2, chặn action+move), `FlankedStatus` (×1.5, OnDamaged tự xoá), `RootStatus` (chặn move), `PoisonStatus(dmg/lượt, duration)` (OnTurnEnd trừ HP, event Necrotic, Kill nếu chết), `GuardStatus` (×0.2) |

**Bất biến:** duration đếm theo **lượt của chủ thể**; status gắn giữa lượt có `CanTick = false` nên không tick ngay cuối lượt đó; hết hạn phát `StatusExpiredEvent`.

### 5.8 Clash pha 1 — Core/Combat

| Kiểu | Thành viên chính |
|------|------------------|
| `ClashRule` / `ClashRuleTable` | `PlayerSkillId, BossSkillId, Bonus`; `DefaultBonus = 3`; `Add(rule)`, `AddElement(pElement, bElement, bonus)`, `GetBonus(playerSkillId, bossSkillId, pElement, bElement)` ưu tiên cặp chiêu → cặp hệ → 3 |
| `ClashDefinition` | `BossSkillId, BossPower, BossElement, Rules, WinEffect/LoseEffect (Func<IStatusEffect>), CreateWinEffect/CreateLoseEffect` |
| `IClashResolver` / `ClashResolver` | `Predict(player,boss,skill,clash)`, `Resolve(...) → ClashOutcome`, `GetOptions(player,boss,skillIds,skills,clash)`; `EffectivePower(skill, bonus)` |
| `ClashOutcome` / `ClashPrediction` / `ClashOption` | result + power 2 bên + effect; prediction có bonus; option cho UI (skillId, displayName, power, prediction) |
| `ClashMath` | `EffectivePower(skill, bonus) = skill.Power + bonus`; `Compare` (> Win, == Draw, < Lose) |

**Bất biến:** `Predict` == `Resolve` với cùng state; không RNG.

### 5.9 Duel Phase 2 — Core/Combat ⭐

| Kiểu | Thành viên chính |
|------|------------------|
| `DuelDefinition` | `Id, BackgroundId, MusicId, TransitionId, BossSkillIds, PlayerSkillIds, RoundLimit, DrawChipRatio (0.10), FinisherCount (1), Rules, WinEffect/LoseEffect, CreateWinEffect/CreateLoseEffect` |
| `DuelState` | `DuelId, Round, BossSkillIndex, BossSkillId, BossPower, BossElement, LastResult, IsOver, Winner, SeenBossSkills` |
| `IClashDuel` / `ClashDuel` | `State, Definition, IsActive`, `Start(state, def, player, boss)`, `GetOptions()`, `Choose(playerSkillId) → ClashRoundResult` (resolve + tự finisher + sang vòng), `Capture()`, `Restore(snapshot)`, `Clear()` |
| `ClashRoundResult` | `Result, PlayerPower, BossPower, DamageToPlayer, DamageToBoss, Applied` |
| Event | `DuelRoundStartedEvent(round, bossSkillId, bossPower)`, `DuelEndedEvent(lastResult, winner)` |

Luật (đã chốt — xem `Docs/QUYET_DINH.md`):

- Win → `WinEffect` (mặc định Stagger) lên boss + **tự đánh `max(1, FinisherCount)` đòn kết liễu** ngay trong `Choose`, rồi sang vòng.
- **Draw → mỗi bên mất 10% uy lực** (`round(power × DrawChipRatio)`, bỏ giáp, element None).
- Lose → người chơi nhận đúng `BossPower` + `LoseEffect` (nếu có).
- Finisher tự động: đòn đánh thường của player lên boss (giáp vẫn áp dụng; Stagger ×2), lặp `FinisherCount` lần — không cần người chơi gửi `Attack`.
- Cuối mỗi vòng, `ClashDuel` gọi `EndRound()` tick status 2 bên theo đúng nhịp `TurnManager.EndTurn` (`TickTurnEnd` → `TickTurnStart`), rồi mới `EnterRound` kế: Poison gây 3 HP/lượt, Flanked/Guard/Stagger hết hạn; status gắn trong vòng không tick ngay ở vòng đó.
- Boss ra chiêu theo chu kỳ `BossSkillIds[(round−1) % n]`; `SeenBossSkills` + `RunKnowledge("boss-skill:<id>")` ghi nhận chiêu đã thấy.
- HP mang theo; status 2 bên clear lúc `Start`; không turn limit/AP/End Turn.

### 5.10 AI & Telegraph — Core/Ai

| Kiểu | Thành viên chính |
|------|------------------|
| `IEnemyBrain` | `Id`, `Plan(self, state) → EnemyIntent` (thuần, tất định) |
| `EnemyIntent` | `Unit, SkillId, TargetCell, DangerCells, Effects, Priority, IsEmpty`; `Empty(unit)` |
| `ITelegraphService` / `TelegraphService` | `All, DangerCells`, `TryGet(unit, out intent)`, `Declare(intent)`, `Clear()` |
| `IEnemyBrainLibrary` / `EnemyBrainLibrary` | `Register`, `TryGet`, `Get`, `All` |
| `IBossPhaseController` / `BossPhaseController` | `Phase`, `ShouldEnterPhase2(hp, maxHp)` (Phase==1 && hp ≤ maxHp × rules.Phase2Threshold), `EnterPhase2(state)` → Phase=2, `LockArea` nếu có cells, `BossPhaseChangedEvent` |

### 5.11 Tìm đường — Core/Units

| Kiểu | Thành viên chính |
|------|------------------|
| `IPathfinder` | `FindPath(PathRequest)`, `ComputeReachable(start, profile, maxCost, self)` |
| `PathRequest` / `PathResult` | `Start, Goal, Profile, MaxCost, IgnoreUnit` / `Status, Path, Cost, IsFound, Goal`; `Found(...)`, `Fail(status)` |
| `PathStatus` | Found, NoPath, OutOfRange, InvalidStart, InvalidTarget |
| `ReachableSet` | `Origin, Count, Cells (sort Y rồi X), Costs, Contains(c), CostTo(c)` |
| `AStarPathfinder` | A* 4 hướng, không chéo; cost theo `MovementCost` (min 1); tie-break f → h → index; tôn trọng `IgnoreUnit`; bay qua ô chỉ `BlocksMove` nếu không `BlocksLineOfSight` |
| `MovementRangeCalculator` | `Compute(pathfinder, grid, start, profile, movePoints, self)` |

### 5.12 Snapshot / Rewind — Core/Run

| Kiểu | Thành viên chính |
|------|------------------|
| `GameSnapshot` | `TurnIndex, Phase, Result, RandomState, Cells, LockedCells, Units, Intents, Duel`, `ContentEquals(other)` |
| `UnitSnapshot` | `Id, Definition, Position, Hp, ActionPoints, HasMovedThisTurn, HasActedThisTurn, Statuses, Cooldowns` |
| `StatusSnapshot` | `Effect, Remaining, CanTick` |
| `DuelSnapshot` | `DuelId, Round, BossSkillIndex, BossSkillId, BossPower, BossElement, LastResult, IsOver, Winner, SeenBossSkills`, `ContentEquals` |
| `ISnapshotStore` / `SnapshotStore` | ring buffer `Capacity` (mặc định 8), `Push`, `TryGetLatest`, `All`, `Clear` |
| `ISnapshotService` / `SnapshotService` | `CanRewind`, `Capture`, `Restore`, `PushCheckpoint(trigger)`, `Rewind()`, `ClearCheckpoints()` |
| `SnapshotTrigger` | StageStart, TurnStart, ClashStart |
| `IRunKnowledge` / `RunKnowledge` | `Deaths`, `Counters`, `SeenKeys`, `HasSeen`, `MarkSeen`, `GetCounter`, `Increment`, `RegisterDeath`; hằng `DeathsKey="deaths"`, `BossSkillSeenPrefix="boss-skill:"` |

**Thứ tự restore (quan trọng):** RNG state → grid cells/locked → registry (`ClearSilently` + `RestoreAdd`) → unit fields/status/cooldowns → telegraph intents → **DuelSnapshot** → `Result` → turn phase/index.

**Bất biến:** `Restore(Capture())` khớp từng trường (đang được `SnapshotTests` kiểm).

### 5.13 Event — Core/Run (28 event)

| Kiểu | Thành viên |
|------|-----------|
| `RefEventHandler<T>(in T)` | delegate không boxing, T : struct |
| `IEventBus` / `EventBus` | `Subscribe<T>(handler) → IDisposable`, `Unsubscribe<T>`, `Publish<T>(in T)`, `Clear` |

| # | Event | Trường |
|---|-------|--------|
| 1 | `StageStartedEvent` | stageId, turnLimit |
| 2 | `TurnPhaseStartedEvent` | phase, turnIndex |
| 3 | `TurnAdvancedEvent` | turnIndex |
| 4 | `UnitSpawnedEvent` | unit, position |
| 5 | `UnitMovedEvent` | unit, from, to, cost |
| 6 | `UnitActedEvent` | unit, kind, target |
| 7 | `ApChangedEvent` | unit, actionPoints |
| 8 | `DamageDealtEvent` | source, target, amount, element, wasStaggered, wasGuarded, wasFlanked, killed, at |
| 9 | `UnitDiedEvent` | unit, at |
| 10 | `StatusAppliedEvent` | unit, status, duration |
| 11 | `StatusExpiredEvent` | unit, status |
| 12 | `SkillUsedEvent` | unit, skillId, target |
| 13 | `IntentDeclaredEvent` | intent |
| 14 | `TelegraphClearedEvent` | count |
| 15 | `CellChangedEvent` | coord, terrain, flags, hazardDamagePerTurn |
| 16 | `CellCollapsedEvent` | coord |
| 17 | `HazardTriggeredEvent` | coord, unit, damage |
| 18 | `ArenaLockedEvent` | cellCount |
| 19 | `BossPhaseChangedEvent` | boss, phase |
| 20 | `ClashStartedEvent` | boss, bossSkillId |
| 21 | `ClashResolvedEvent` | boss, playerSkillId, result, playerPower, bossPower |
| 22 | `StageEndedEvent` | result, turnIndex |
| 23 | `RewindPerformedEvent` | trigger, turnIndex |
| 24 | `SnapshotPushedEvent` | trigger, turnIndex |
| 25 | `RunKnowledgeUpdatedEvent` | key, value |
| 26 | `CameraShakeHintEvent` | strength, duration |
| 27 | `DuelRoundStartedEvent` | round, bossSkillId, bossPower |
| 28 | `DuelEndedEvent` | lastResult, winner |

**Bất biến:** mỗi thay đổi state phát **đúng một** event; UI không tự suy ra state từ delta.

### 5.14 Session, Rules, RNG — Core/Run

| Kiểu | Thành viên chính |
|------|------------------|
| `GameSession` | `State, Events, Skills, Brains, Knowledge`; `RegisterStage`, `StartStage(id/definition)`, `TryExecute(request)`, `EndTurn()`, `BeginPlayerPhase()`, `EnterDuel()`, `Restart()`, `Reset()` |
| `IGameRules` / `GameRules` | `ApPerTurn 2, MovePoints 2, TurnLimit 25, Phase2Threshold 0.7, GuardMultiplier 0.2, StaggerMultiplier 2, FlankMultiplier 1.5, PoisonPerTurn 3` |
| `GameState` | `Grid, Units, Pathfinder, Turn, Rules, Events, Random, Skills, Damage, Telegraph, Clash, Duel, BossPhase, Snapshots, Knowledge, Stage, Result, IsOver` |
| `StageDefinition` | `Id, DisplayName, Width/Height, Player, PlayerSpawn, Spawns, Terrains, TurnLimit, BossId, Clash, Duel` |
| `UnitSpawnDefinition` / `TerrainOverride` | `(definition, anchor)` / `(coord, terrain, flags, movementCost, hazardDamagePerTurn, collapseInTurns)` |
| `IObjectiveEvaluator` / `ObjectiveEvaluator` | `Evaluate(state, stage) → StageResult` |
| `StageResult` | None, Victory, DefeatPlayerDead, DefeatTurnLimit, EndingA, EndingB |
| `IRandomSource` / `XorShiftRandom` | `State`, `NextInt(min, max)`, `NextFloat()`; seed cố định (0 → 0x9E3779B9) |
| `IGameStateView` | `Grid, Units, Turn, Rules` |

### 5.15 Hợp đồng cho Unity / UI (lớp 3–4)

| Interface | Lớp | Trách nhiệm |
|-----------|-----|-------------|
| `IGameStateView` | Core | Mặt đọc state: Grid/Units/Turn/Rules |
| `IInputSource` | Unity | `Poll() → ActionRequest?`; không chứa luật |
| `IAudioService` | Unity | `PlayBgm(id)`, `PlaySfx(id)`, `SetVolume(channel, v)` |
| `ISettingsStore` | Unity | `Load/Save(SettingsModel)` — chỉ settings, không lưu tiến trình |
| `SettingsModel` | Unity | MasterVolume, MusicVolume, SfxVolume, Fullscreen, Language |
| `DialogueRunner` | Core | `Start/Advance/Choose/Current/Ending` — Unity chỉ hiển thị |
| `ISkillBehaviour`, `ITerrainBehaviour` | Content | Điểm cắm mở rộng |

### 5.16 Checklist hợp đồng (trạng thái as-built)

| # | Nhóm | Trạng thái | Bằng chứng |
|---|------|-----------|-----------|
| 1 | Grid, shapes, LOS, collapse, lock | ✅ | `GridTests` 8 test |
| 2 | Units, registry, footprint 2×2 | ✅ | `GridTests.Shapes_*`, `UnitRegistry` |
| 3 | A*, reachable | ✅ | `PathfindingTests` 6 test |
| 4 | Turn 4 nhịp + AP | ✅ | `TurnFlowTests` 5 test |
| 5 | Validator + executor | ✅ | `StageObjectiveTests` 7 test |
| 6 | Damage + element matrix | ✅ | `DamageTests` 6 test |
| 7 | EffectQueue + 10 effect | ✅ | `ClashTests`, `DamageTests` |
| 8 | StatusSet + 5 status | ✅ | `TurnFlowTests`, `DamageTests` |
| 9 | Telegraph + brain/tất định | ✅ | `AiDeterminismTests` 4 test |
| 10 | Snapshot capture/restore | ✅ | `SnapshotTests` 4 test |
| 11 | Clash + rule table | ✅ | `ClashTests` 7 test |
| 12 | Boss phase | ✅ | `AiDeterminismTests` |
| 13 | ObjectiveEvaluator | ✅ | `StageObjectiveTests` |
| 14 | GameSession façade | ✅ | `StageObjectiveTests`, `DuelFlowTests` |
| 15 | RunKnowledge ngoài rewind | ✅ | `SnapshotTests` |
| 16 | **Duel Phase 2** | ✅ | `DuelFlowTests` 11 test |
| 17 | Input → ActionRequest | ⬜ | Unity layer |
| 18 | HUD / command bar | ⬜ | UI |
| 19 | Clash/Duel presenter | ⬜ | UI |
| 20 | Dialogue UI + 2 ending | ⬜ | UI |
| 21 | Settings + audio | ⬜ | Unity |

---

## 6. Luồng xử lý

### 6.1 Một lượt đấu (thực tế trong Core)

```
GameSession.Build(stage)
  ├─ EventBus, GridMap, UnitRegistry, GameState, services (Pathfinder/Damage/Telegraph/Clash/Duel/BossPhase/Knowledge)
  ├─ TurnManager, SnapshotService
  ├─ ApplyTerrains + SpawnUnits
  └─ Turn.StartStage → PushCheckpoint(StageStart) → PlanEnemyIntents → BeginPlayerPhase
                                       │
Player dùng AP: TryExecute(ActionRequest) │
  ├─ ActionValidator.Validate (thuần)      │
  ├─ GameActionExecutor.Execute            │
  │    ├─ Move: TrySpend(1), Units.MoveTo, UnitMovedEvent
  │    ├─ Attack: DamageEffect → EffectQueue.Run, TrySpend(1), HasActed, UnitActedEvent
  │    ├─ Skill: TrySpend(ApCost), SetCooldown, behaviour/effect, SkillUsedEvent
  │    ├─ Guard: TrySpend(1), GuardStatus
  │    └─ ClashSkill (chỉ pha Clash): ClashResolver.Resolve
  └─ nếu Success → TryEnterDuel (boss < ngưỡng?)
                                       │
GameSession.EndTurn()  [chỉ ở pha Player]
  ├─ Turn.BeginEnemyPhase → EnemyOrder
  ├─ RunEnemyBrains: resolve đúng intent đã Declare (không tính lại AI)
  ├─ Turn.EndEnemyPhase
  ├─ Turn.EndTurn()       → tick status/hazard/collapse → TurnIndex++ → objective → checkpoint
  ├─ PlanEnemyIntents     → telegraph cho lượt kế
  └─ Turn.BeginPlayerPhase → ResetActionPoints = 2
```

> Lưu ý: `ActionRequest.EndTurn` qua `TryExecute` **không** chạy AI (dùng cho test/UI tách nhịp);
> chỉ `GameSession.EndTurn()` chạy đủ nhịp Enemy → TurnEnd → Telegraph → Player.

### 6.2 Phase 2 — Duel mode (thay cho Clash trên bàn cờ)

```
Một action thành công → TryEnterDuel()
  ├─ điều kiện: Stage.Duel != null, boss sống, IsOver == false,
  │             Phase != Duel, boss.HpRatio < Rules.Phase2Threshold (0.70)
  └─ GameSession.EnterDuel()
        ├─ Telegraph.Clear()
        ├─ BossPhase.EnterPhase2(State)        → BossPhaseChangedEvent (phase 2)
        ├─ Duel.Start(state, stage.Duel, player, boss)
        │     ├─ clear status 2 bên
        │     ├─ ClashStartedEvent (chiêu boss đầu)
        │     └─ EnterRound(1): BossSkillIndex = (round−1) % BossSkillIds.Count
        │           → DuelRoundStartedEvent(round, bossSkillId, bossPower)
        └─ Turn.BeginDuel()                    → TurnPhase.Duel
                                              │
Người chơi xem Duel screen (background + 2 bên + 3 thẻ chiêu)
  ├─ ClashDuel.GetOptions() → ClashOption[] (mỗi option có prediction Win/Draw/Lose)
  ├─ gửi ActionRequest.ClashSkill(skillId)
  ├─ ActionValidator.ValidateDuel → GameActionExecutor.ExecuteDuelClash
  └─ ClashDuel.Choose(skillId)
        ├─ power = EffectivePower(skill, bonus) so với BossPower
        ├─ Win  → boss.WinEffect + tự đánh FinisherCount đòn (ATK × status; Stagger ×2)
        ├─ Draw → mỗi bên mất 10% power (bỏ giáp)
        ├─ Lose → player nhận BossPower (+ LoseEffect)
        └─ EndRound: tick status 2 bên (Poison damage, status hết hạn) → EnterRound kế
                                              │
Ai chết trước → Duel.End(Winner) → DuelEndedEvent
  ├─ boss chết  → ObjectiveEvaluator → Victory → FinishStage → StageEndedEvent
  └─ player chết → DefeatPlayerDead → FinishStage → StageEndedEvent
```

> Không lưới, không AP, không End Turn, không turn limit trong Duel.

### 6.3 Chết & Rewind

```
UnitDied(player) hoặc hết turn limit (chỉ ngoài Duel)
        │
        ▼
GameSession.Restore checkpoint (SnapshotService.Rewind)
        ├─ RewindPerformedEvent(trigger, turnIndex)
        ├─ Restore grid + units + status + AP + telegraph + DuelSnapshot + Result + phase
        ├─ IRunKnowledge GIỮ NGUYÊN (đã thấy chiêu boss, số lần chết)
        └─ checkpoint gần nhất: TurnStart / ClashStart / StageStart
        │
        ▼
Người chơi chơi lại từ state đã chụp
```

- `Rewind()` luôn dùng **checkpoint gần nhất** (TurnStart mỗi lượt, ClashStart khi vào Clash pha 1, StageStart ở đầu màn). Core không tự rewind khi chết — game flow/UI quyết định thời điểm gọi.
- Không lưu run ra đĩa (ADR-05).

### 6.4 Một frame của lớp Unity (không chứa luật)

```
Update()
  ├─ UnityInputSource → ActionRequest?
  ├─ SelectionController: hover ô → ActionValidator.Validate() (chỉ tô màu)
  └─ click → GameSession.TryExecute(request)
        │
        ├─ EventBus.Publish(DamageDealt / UnitMoved / DuelRoundStarted / …)
        ▼
Presenters: HudPresenter, BoardHighlighter, DamageNumberSpawner, DuelScreenPresenter
        │
        ▼
View: chỉ set sprite/text/position
```

---

## 7. Mapping dữ liệu: ScriptableObject → Core

Quy tắc: **SO ở lớp Unity, POCO ở Core**. Mỗi asset có hàm `ToDefinition()` thuần, không side effect.

| ScriptableObject (Unity) | POCO / hàm chuyển | Dùng bởi |
|--------------------------|-------------------|----------|
| `GameConfigAsset` | `GameRules` | `TurnManager`, `DamageCalculator` |
| `UnitDataAsset` | `UnitDefinition` | `IUnitRegistry.Add` |
| `SkillDataAsset` | `SkillDefinition` + `ISkillBehaviour` (Content) | `SkillAction`, `Duel.GetOptions` |
| `StageDataAsset` | `StageDefinition` (gồm `Clash`, `Duel`) | `GameSession`, `ObjectiveEvaluator` |
| `TerrainDataAsset` | `TerrainId` + `ITerrainBehaviour` | `GridMap`/Content |
| `ClashMatrixAsset` | `ClashRuleTable` | `ClashResolver` |
| `DuelDataAsset` | `DuelDefinition` | `ClashDuel` |
| `ElementMatrixAsset` | `ElementMatrix` | `DamageCalculator` |
| `AudioCatalogAsset` | `id → AudioClip` | `UnityAudioService` |
| `DialogueAsset` | `DialogueGraph` | `DialogueRunner` |

Lưu ý: `SkillDataAsset` gồm **dữ liệu thuần** (id, power, element, range, AP cost, targeting, `IsClashSkill`) và tham chiếu `ISkillBehaviour` cho phần logic đặc biệt. Skill "đánh thường" không cần code; skill như "Necrotic Ward hấp thụ độc rồi phản 3 HP/lượt" mới cần class.

---

## 8. Bảng số liệu chốt

### 8.1 Đơn vị (Phần 7.1)

| Đơn vị | Faction | HP | ATK | MOV | Tầm | Size | Ghi chú |
|--------|---------|----|-----|-----|-----|------|---------|
| Kẻ lang thang | Player | 12 | 4 | 2 | 1 | 1×1 | 2 AP, 2 skill phòng thủ |
| Eosphoros | Ally/Boss | 30 | 6 | 2 | 1–2 | 1×1 | Giáp dày, chậm, yếu sườn |
| Tiểu Quỷ (Imp) | Enemy | 6 | 2 | 3 | 1 | 1×1 | Chết 1 nhát |
| Quái Thạch (Gargoyle) | Enemy | 10 | 3 | 2 | 2–3 | 1×1 | Bay, miễn bẫy/vực |
| Đao Phủ Tro Tàn | Boss 1 | 20 | 5 | 1 | 1 | 1×1 | Earth Crack |
| Vu Nữ Morbida | Boss 2 | 18 | 4 | 2 | 2 | 1×1 | Poison Miasma |
| Kỵ Sĩ Hư Không | Boss 3 | 25 | 6 | 1 | 1–3 | 1×1 | Khiên phản đòn |
| Quỷ Vương Malkor | Boss 4 | 45 | 8 | 1 | toàn sân | **2×2** | Neo góc min; Stagger khi trúng |
| Eosphoros sa ngã | Boss 5 | 30 | 6 | 2 | 1–2 | 1×1 | Thánh quang |

### 8.2 Clash pha 1 (Phần 7.2) — dữ liệu cho `ClashRuleTable`

| Ải | Tuyệt kỹ boss | Uy lực | Hệ | Skill thắng (base + bonus) | Kết quả |
|----|---------------|--------|-----|----------------------------|---------|
| 1 | Tro Tàn Cuồng Nộ | 15 | Physical | Protect 15+3=18, Swift Slash 14+3=17 | Win → Stagger 1 lượt |
| 2 | Bão Khí Độc Diệt Thế | 14 | Necrotic | Necrotic Burst 13+3=16 | Win → Vu Nữ tự trúng độc 3 HP/lượt ×2 |
| 3 | Tia Thương Hư Không | 16 | Piercing | Dodge 14+3=17, Swift Slash 14+3=17 | Win → Flanked |
| 4 | Cự Kiếm Hỏa Ngục | 18 | Fire | Temporal Step 16+3=19, Dodge 14+**5**=19 | Win → Stagger |
| 5 | Thánh Quang Trảm | 18 | Holy | Swift Slash 16+3=19, Protect 15+**4**=19 | Win → Stagger |

> Bonus mặc định +3 nhưng cặp cụ thể có thể +4/+5 → `ClashRule` khoá theo cặp `(playerSkillId, bossSkillId)`, fallback theo element.

### 8.3 Duel Phase 2 (đã chốt)

| Ải | Boss | Boss power | Đòn thường | Hiệu ứng Win |
|----|------|-----------|------------|--------------|
| 1 | Đao Phủ Tro Tàn | 15 | 10 | Stagger |
| 2 | Vu Nữ Morbida | 14 | 9 | Poison ngược 3 HP/lượt ×2 |
| 3 | Kỵ Sĩ Hư Không | 16 | 11 | Flanked |
| 4 | Malkor | 18 | 12 | Stagger + finisher |
| 5 | Eosphoros sa ngã | 18 | 12 | Stagger |

Luật chung: Draw mỗi bên mất **10%** uy lực; Win tự đánh **`FinisherCount` đòn** kết liễu (ATK × status, Stagger ×2) rồi sang vòng; không giới hạn vòng (mặc định), boss lặp chu kỳ 2 chiêu; HP mang theo, clear status khi vào Duel.

### 8.4 Hằng số luật chơi

| Hằng số | Giá trị | Nguồn |
|---------|---------|-------|
| AP/lượt | 2 (1 Move + 1 Action) | Phần 3.1.2 |
| Move/lượt | 2 ô, 4 hướng, không chéo | Phần 3.2.1 |
| Melee range | 1 | Phần 3.2.1 |
| Ranged range | 2–3, cần LOS | Phần 3.2.1 |
| Turn limit | 20–25 / màn (Duel: không áp) | Phần 3.4 |
| Ngưỡng Phase 2 | HP < 70% | Phần 3.1.3 |
| Stagger | 1 lượt, ×2 sát thương | Phần 3.1.3(d) |
| Guard | −80% sát thương | Phần 3.2.3 |
| Flanked | ×1.5 đòn kế tiếp (chỉ từ status/Clash, không tính facing) | Phần 3.2.3 |
| Poison miasma | 3 HP/lượt | Phần 7.2 |
| Duel Draw | 10% uy lực mỗi bên | Quyết định 2026-11-15 |
| Duel finisher | 1 đòn ATK × status | Quyết định 2026-11-15 |
| Bảng màu | #E5C158, #2C4270, #6B3BA7, #8B1E1E, #3A3E45 | Phần 6.1 |

### 8.5 Năm màn

| Ải | Kích thước | Boss | Ghi chú |
|----|-----------|------|---------|
| 1 | 8×8 | Đao Phủ Tro Tàn | Oakhaven, tutorial |
| 2 | 9×9 | Vu Nữ Morbida | poison, Grave Hands |
| 3 | 9×9 | Kỵ Sĩ Hư Không | Living Eye Shield |
| 4 | 10×10 | Malkor (2×2) | Hellfire, Brimstone |
| 5 | 8×8 | Eosphoros sa ngã | Holy, 2 ending |

---

## 9. Công thức mở rộng (không sửa Core)

### 9.1 Thêm kỹ năng
1. Class `MySkill : ISkillBehaviour` trong `Content/Skills/`.
2. Override `BuildEffects(EffectContext)` — chỉ xếp effect.
3. Tạo `SkillDataAsset` (id, power, element, range, targeting, AP cost, cooldown, `IsClashSkill`), gán behaviour.
4. Thêm id vào `UnitDataAsset.SkillIds`.
5. Nếu là chiêu Clash: thêm vào `DuelDefinition.PlayerSkillIds` / `ClashRuleTable`.
→ Không sửa file nào trong `Core/`.

### 9.2 Thêm quái
1. `UnitDataAsset` mới (HP/ATK/MOV/brain id).
2. Hành vi mới: `MyBrain : IEnemyBrain` trong `Content/Enemies/`, đăng ký `BrainId`.
3. Đặt vào `StageDataAsset.Spawns`.
Hành vi cũ chỉ cần chọn brain — không code.

### 9.3 Thêm boss / màn
1. `MyBossBrain : BossBrainBase` implement `IEnemyBrain` (+ `IBossPhaseController` nếu đổi pha khác).
2. `StageDataAsset`: kích thước, spawn, terrain override, turn limit, boss id, `Clash`, `Duel`.
3. `DuelDataAsset`: background, music, `BossSkillIds`, `PlayerSkillIds`, `DrawChipRatio`, `FinisherCount`, win/lose effect, rules.
4. Thêm dòng `ClashRuleTable` cho các cặp chiêu.
5. `DialogueAsset` nếu có thoại.
→ Màn 6+ không cần sửa Core.

### 9.4 Thêm địa hình
1. `MyTerrain : ITerrainBehaviour` (Content) — `BlocksMovement`, `OnEnter`, `OnTurnStart`, `OnDamaged`.
2. Đăng ký `TerrainId → behaviour` trong `TerrainCatalog`.
3. `TerrainDataAsset` + sprite.
> `TerrainId` là chuỗi nên không phải sửa enum/Core.

### 9.5 Thêm kết thúc / nhánh truyện
1. Thêm node `DialogueAsset` + `DialogueChoice` điều kiện `IRunKnowledge`.
2. Chỉ thêm `StageResult` mới khi luật thật sự mới; thoại khác thì dùng `DialogueGraph`.

### 9.6 Thêm một Duel mới
1. `DuelDataAsset` mới (xem 9.3) — không cần class mới nếu luật chuẩn.
2. Gán vào `StageDataAsset.Duel`.
3. Thêm 2 chiêu boss vào `SkillLibrary` (`IsClashSkill = false` cho chiêu boss).
4. Test bằng `DuelFlowTests`-style: Start → Choose (tự finisher + sang vòng) → kiểm Result/HP.
> Chỉ cần class mới khi muốn luật Duel khác (ví dụ finisher nhiều đòn, chip máu khác).

---

## 10. Lộ trình

### 10.1 Đã xong (Core, 2026-11-15)

| Hạng mục | Kết quả |
|----------|---------|
| Hạ tầng | asmdef Core (noEngineReferences), asmdef Tests (Editor-only), `InternalsVisibleTo`, harness dotnet ngoài Unity |
| Core P0–P3 | Grid, Units, Turn, Actions, Damage/Effect/Status, Clash, AI/Telegraph, Snapshot/RunKnowledge, Objective, GameSession, Dialogue — 163 file |
| Duel Phase 2 | `ClashDuel` + 6 file dữ liệu/kết quả + tích hợp Session/Validator/Executor/Snapshot/Objective/Turn |
| Test | 65/65 xanh trên `dotnet test` (~155 ms) và Unity 6000.6.7f1 EditMode |
| Docs | KIEN_TRUC (bản này), CORE_API_TONG_HOP, THIET_KE_PHASE2_DUEL, QUYET_DINH, KE_HOACH_HOAN_THANH_GAME |

### 10.2 Còn lại (theo `Docs/KE_HOACH_HOAN_THANH_GAME.md`)

| Pha | Nội dung | Ước lượng |
|-----|----------|-----------|
| P0 | asmdef Content/Game/UI/Editor, font TMP tiếng Việt, palette, scene Boot | 0,5–1 ngày |
| P1 | Content ải 1: `UnitCatalog`, `SkillCatalog`, `StageCatalog`, `DuelCatalog`, brain Imp/Gargoyle, skill behaviour, test simulation | 2–3 ngày |
| P2 | Unity presentation: BoardView, UnitView, InputController, Presenter, CameraRig, PlayMode smoke | 2–3 ngày |
| P3 | UI: HUD, command bar, Clash/Duel screen, dialogue, menu, settings | 2–3 ngày |
| P4 | Flow: Duel trigger, 5 ải, rewind, 2 ending (ally đã cắt) | 1–1,5 ngày |
| P5 | Art/audio: tách sheet, animation, tileset, VFX, mixer | 2–3 ngày |
| P6 | QA + build Windows/WebGL | 1–2 ngày |

Tổng: demo ải 1 ~1 tuần; 3 ải ~2,5 tuần; đủ 5 ải ~4–5 tuần part-time.

### 10.3 Definition of Done (mỗi tính năng)
1. Có interface/POCO trong Core (nếu là luật) — không cần nếu chỉ là nội dung.
2. Có unit test thuần C# cho phần Core.
3. UI không tính toán luật.
4. Thêm nội dung mới không sửa Core.
5. Có tên trong bảng 4.1 và test tương ứng.

---

## 11. Kiểm thử & bảo trì

### 11.1 Test matrix — 65 test / 10 bộ (EditMode, không cần scene)

| Bộ | Số test | Khẳng định chính |
|----|---------|------------------|
| `GridTests` | 8 | Manhattan/Chebyshev; tường chặn cả walk+fly; bay qua pit nhưng không qua tường; LOS; collapse→Pit+event; lock/unlock; CellChangedEvent; shapes Line/Neighbours/Cone/Footprint |
| `PathfindingTests` | 6 | path gồm start + cost; né tường; né unit; corridor bị chặn→NoPath; budget→OutOfRange; reachable Manhattan≤2 = 12 ô |
| `TurnFlowTests` | 5 | initiative ↓ + hoà UnitId ↑; AP=2 + ApChangedEvent; Stagger không spend; EndTurn tăng TurnIndex, Poison chỉ gây damage từ lượt kế; BeginEnemyPhase reset AP + NextEnemy |
| `DamageTests` | 6 | giáp sau hệ; Guard 4→1; Stagger/Flanked ×2/×1.5; Holy vs Necrotic ×1.5; IgnoresArmor; lethal → HP 0 + UnitDiedEvent |
| `ClashTests` | 7 | rule pair→element→default; Predict==Resolve; Win gắn Stagger boss; Lose gắn effect player; Draw không gắn; GetOptions chỉ chiêu Clash |
| `AiDeterminismTests` | 4 | DangerCells sort Y→X; Declare thay thế + Clear count; BossPhase threshold + lock arena + event; Initiative ổn định |
| `SnapshotTests` | 4 | Restore(Capture()) khớp state; rewind về checkpoint gần nhất; RunKnowledge không mất; store giữ capacity |
| `StageObjectiveTests` | 7 | start pha Player/AP; Move trừ AP + đổi ô; vào tường→CellBlocked; attack xa→OutOfRange; giết boss→Victory; hết lượt→DefeatTurnLimit; player chết→DefeatPlayerDead |
| `DuelFlowTests` | 11 | enter khi boss dưới ngưỡng + clear status; options 3 chiêu + badge; Win + finisher tự động ×2 + sang vòng; Draw 10% + round kế; Lose chết→DefeatPlayerDead; giết boss→Victory; Duel path bỏ qua Stage.Clash + ghi RunKnowledge; snapshot giữ DuelState; rule table ưu tiên cặp; Attack trong Duel bị chặn |

### 11.2 Lệnh chạy test

```powershell
# Ngoài Unity (nhanh, dùng dotnet bundled của Unity 6000.6.7f1)
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" test "D:\test\Behind the hero\Tools\CoreTests\CoreTests.csproj"

# Trong Unity (EditMode, batchmode) — phải dùng bản copy nếu project gốc đang mở
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "D:\test\_core_check" -runTests -testPlatform EditMode `
  -assemblyNames BTH.Core.Tests -testResults "D:\test\_core_check\results.xml" -logFile "D:\test\_core_check\unity.log"
```

> Unity có thể trả exit 0 trước khi test xong — chờ file `results.xml` xuất hiện rồi đọc `total`/`passed`.

### 11.3 Checklist review PR

- [ ] Không có `using UnityEngine` trong `Core/`.
- [ ] Không có `Random.`, `Time.`, `FindObjectOfType`, `Instantiate` trong `Core/`.
- [ ] Con số cân bằng mới nằm trong `IGameRules`/definition, không hard-code.
- [ ] Mọi thay đổi state phát đúng 1 event; UI không tự sửa state.
- [ ] Tính năng mới có tên trong bảng 4.1 và test tương ứng.
- [ ] Không thêm phụ thuộc asmdef trái chiều (Core → Unity).
- [ ] `ActionValidator` từ chối đúng `ActionError`, UI hiện được lý do.
- [ ] Test mới chạy xanh cả dotnet và Unity EditMode.

### 11.4 Anti-pattern phải tránh

| Sai | Đúng |
|-----|------|
| `unit.Hp -= dmg` trong `UnitView` | `DamageCalculator.Apply()` → `DamageDealtEvent` → view cập nhật |
| `if (unit.Id == "malkor") ...` trong Core | `IBossPhaseController`/brain theo từng boss |
| AI tính lại ở Enemy Phase | thi hành `EnemyIntent` đã công bố |
| `GameObject.Find("Player")` | inject qua `GameSession`/`IUnitRegistry` |
| Tự tính damage trong UI để hiện dự báo | dùng `ClashResolver.Predict`/`ClashDuel.GetOptions` |
| Lưu tiến trình vào PlayerPrefs | chỉ lưu `SettingsModel` |
| Coroutine trong Core | Core thuần, Unity animate theo event |
| Thêm `if (phase == Duel)` khắp nơi | đi qua `IClashDuel` + route trong Validator/Executor |

---

## 12. ADR & quyết định

| ID | Quyết định | Lý do | Hệ quả |
|----|-----------|-------|--------|
| ADR-01 | Rewind bằng **snapshot**, không replay command | Turn-based, state nhỏ; replay đòi tất định tuyệt đối ở animation/âm thanh | Tốn RAM vài chục KB/checkpoint; giữ command log để debug |
| ADR-02 | **Tất định** + RNG có seed | Rewind và test cần cùng input → cùng output | Cấm random trong Clash/Duel; AI sort ổn định |
| ADR-03 | ID dạng **chuỗi** (TerrainId/ElementId/SkillId) | Thêm nội dung không phải sửa enum/Core | Mất kiểm tra compile-time, bù bằng `DataValidator` |
| ADR-04 | 4 lớp + **asmdef** | Ngăn Core lén dùng UnityEngine | Phải khai báo asmdef khi thêm thư mục |
| ADR-05 | **Không lưu tiến trình**, chỉ lưu settings | Thiết kế: chết là mất đồ, phải kiếm lại | `ISettingsStore` chỉ chứa `SettingsModel` |
| ADR-06 | Telegraph là **intent object** | Người chơi phải thấy đúng điều sẽ xảy ra | AI không được "đổi ý" sau khi công bố |
| ADR-07 | **Phase 2 = Duel mode**, không còn bàn cờ | Thiết kế mới: chỉ đấu chiêu 1v1, hai bên đứng 2 phía trên background, có chuyển cảnh | Thêm `TurnPhase.Duel`, `IClashDuel`/`ClashDuel`, DuelSnapshot; TurnManager/Validator/Executor rẽ nhánh theo phase |

### 12.1 Những điểm đã chốt (trước đây là câu hỏi mở)

| Vấn đề | Quyết định |
|--------|-----------|
| Rewind về đâu | Dùng checkpoint gần nhất: `TurnStart` mỗi lượt, `ClashStart` khi vào Clash pha 1, `StageStart` ở đầu màn; Core không tự rewind, game flow/UI gọi `Rewind()` |
| Eosphoros ally | **Cắt khỏi gameplay** (chỉ còn trong truyện/portrait); không tốn AP, không AllyBrain |
| MP trên HUD | **Trang trí** — Core không có tài nguyên MP |
| Malkor 2×2 | Neo **góc min (dưới-trái)**, footprint +X/+Y; "đánh sau lưng" không tính theo facing — Flanked chỉ đến từ status/Clash |
| Lưu run | **Không** (ADR-05) |
| Duel: Draw | Mỗi bên mất **10%** uy lực (đã sửa từ 25%) |
| Duel: hiệu ứng thắng | Giữ riêng từng ải: Stagger / Poison ngược / Flanked / Stagger+finisher / Stagger |
| Duel: HP & status | HP mang theo; status clear khi bắt đầu Duel; boss lặp chu kỳ 2 chiêu |
| Duel: turn limit | Không áp dụng; `RoundLimit = 0` mặc định |

### 12.2 Còn mở (không chặn code)

1. Deadline nộp bài và số người thực tế tham gia code.
2. Nền tảng build cuối: Windows, WebGL hay cả hai.
3. Phạm vi: làm trọn 5 ải hay demo 1 ải trước.
4. Có cần cutscene/hiệu ứng chuyển cảnh riêng cho từng boss hay dùng 1 prefab chung.

---

*Hết tài liệu. Khi sửa kiến trúc, cập nhật bảng 4.1 + §5 tương ứng; khi sửa API, cập nhật `CORE_API_TONG_HOP.md`.*
