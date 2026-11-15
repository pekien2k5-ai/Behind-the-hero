# BEHIND THE HERO — Kiến trúc mã nguồn Unity

> Tài liệu kiến trúc & phân rã tính năng, dựng từ thiết kế game trong `65CNTT_Nhom10_Tai_lieu_thiet_ke.docx`
> (Nhóm 10 – 65CNTT: Ngô Quý Kiên, Nguyễn Tuấn Kiệt, Nguyễn Xuân Lăng, Phạm Thị Huyền Linh).
> Mục đích: chia nhỏ hệ thống thành các file **Core / Logic / UI** tách biệt để code Unity dễ mở rộng và bảo trì.
> Đối chiếu: Phần 3 (gameplay), Phần 5 (5 màn), Phần 6 (UI), Phần 7 (ma trận).
> Bản rút gọn: cây thư mục 2 cấp (mục 3), **không kèm code mẫu** — chữ ký hàm sẽ chốt khi vào project.

---

## 0. Mục lục

| # | Chương | Nội dung |
|---|--------|----------|
| 1 | Nguyên tắc kiến trúc | 12 quy tắc bắt buộc, lý do |
| 2 | Kiến trúc 4 lớp | Core → Content → Unity → UI, asmdef, luật phụ thuộc |
| 3 | Cây thư mục | Cây 2 cấp, quy ước đặt tên, bảng tra nhanh "sửa X ở đâu" |
| 4 | Phân rã tính năng | F00–F20: mỗi tính năng gồm file Core / triển khai / logic / UI |
| 5 | Hợp đồng Core | Danh sách kiểu/hàm core cần làm — chỉ tên, trách nhiệm, ai gọi; không code mẫu |
| 6 | Luồng xử lý | Vòng lượt, Clash, Rewind, chết & hồi sinh |
| 7 | Mapping dữ liệu | ScriptableObject ↔ POCO của Core |
| 8 | Công thức & hằng số | Bảng số liệu cân bằng lấy từ tài liệu |
| 9 | Công thức mở rộng | Thêm skill / quái / boss / địa hình / màn không sửa Core |
| 10 | Lộ trình | 6 sprint, thứ tự code core, Definition of Done |
| 11 | Kiểm thử & bảo trì | Test matrix, checklist review, anti-pattern |
| 12 | ADR | 6 quyết định kiến trúc kèm lý do |

---

## 1. Nguyên tắc kiến trúc (bắt buộc)

| # | Nguyên tắc | Lý do / cách thực thi |
|---|-----------|------------------------|
| 1 | **Core là C# thuần**, không `using UnityEngine` | Chạy được trong EditMode test và CI; không kéo theo scene/prefab; đổi engine không phải viết lại luật chơi. |
| 2 | **Một chiều phụ thuộc**: UI → Unity → Content → Core | Core không được biết lớp trên. Kiểm tra bằng asmdef, không bằng lời hứa. |
| 3 | **Mọi thay đổi trạng thái đi qua Command** (`IGameAction`) | Có log → replay, undo/rewind, test dễ, không có "code chui" sửa state từ UI. |
| 4 | **Tất định (deterministic)**: RNG có seed, AI sắp thứ tự ổn định | Clash không dùng may rủi; cùng input → cùng output, nếu không thì rewind sẽ lệch. |
| 5 | **Không dùng random/Time/Find trong Core** | Cấm `Random.Range`, `Time.deltaTime`, `FindObjectOfType`, `Instantiate` trong `BTH.Core`. |
| 6 | **Dữ liệu là asset, không phải code** | Chỉ số quái, skill, map, ma trận khắc chế nằm trong ScriptableObject; Core chỉ thấy `StageDefinition`, `UnitDefinition`... |
| 7 | **UI nghe event, không query ngược** | `IEventBus` phát `UnitMoved`, `DamageDealt`... Presenter tự cập nhật view. |
| 8 | **Một composition root duy nhất** (`GameBootstrap`) | Không singleton rải rác; service được inject qua constructor. |
| 9 | **Một tính năng = một thư mục**, một file = một type chính | Tìm code theo tính năng, không theo "loại file". |
| 10 | **ID dạng chuỗi (ElementId, TerrainId, SkillId)** thay vì enum | Thêm hệ/địa hình/skill mới = thêm asset, không sửa Core (Open/Closed). |
| 11 | **Hàm core phải thuần hoặc nhận state tường minh** | Không đọc static/global; dễ test và dễ snapshot. |
| 12 | **Mọi con số cân bằng nằm trong `IGameRules`** | Không hard-code 0.7 / 2 / 200% trong logic. |

### Phân biệt 3 loại "core" trong tài liệu này

| Loại | Nghĩa | Ví dụ |
|------|-------|-------|
| **Core (hợp đồng)** | Interface + POCO + thuật toán nền, không biết nội dung cụ thể | `IGridMap`, `IClashResolver`, `GameSession` |
| **Triển khai chi tiết của Core (Implementation)** | Lớp cụ thể hoá hợp đồng, vẫn C# thuần | `AStarPathfinder`, `SnapshotService`, `ClashResolver` |
| **Logic / Content** | Nội dung gameplay: skill nào, boss nào, địa hình nào | `SwiftSlashSkill`, `MorbidaPhase2`, `CrackedFloorTerrain` |
| **UI / Presentation** | MonoBehaviour, view, presenter, hiệu ứng | `HudPresenter`, `ClashScreenView`, `UnitView` |

> Quy tắc vàng: **thêm nội dung mới chỉ động vào Content/UI; thêm luật chơi mới mới động vào Core.**

---

## 2. Kiến trúc 4 lớp

```
┌─────────────────────────────────────────────────────────────┐
│ 4. BTH.UI          HUD, Clash UI, Menu, Settings             │  uGUI + TMP
│                    Presenter (MonoBehaviour)                 │
├─────────────────────────────────────────────────────────────┤
│ 3. BTH.Unity       Bootstrap, Adapter, View, Input, Audio    │  Unity runtime
│                    ScriptableObject ↔ POCO                   │
├─────────────────────────────────────────────────────────────┤
│ 2. BTH.Content     Skill, Terrain, Brain, Boss, Stage data   │  nội dung game
│                    (implementation of Core contracts)        │
├─────────────────────────────────────────────────────────────┤
│ 1. BTH.Core        Grid, Turn, Action, Combat, Clash,        │  C# thuần 100%
│                    AI contract, Rewind, Event, Rules         │
└─────────────────────────────────────────────────────────────┘
         ▲ mũi tên phụ thuộc chỉ đi lên, không bao giờ đi xuống
```

### 2.1 Trách nhiệm từng lớp

| Lớp | Được làm | Không được làm |
|-----|----------|----------------|
| **Core** | Luật chơi, state, thuật toán, event | Biết prefab, sprite, âm thanh, input, coroutine |
| **Content** | Cài đặt cụ thể skill/AI/terrain/boss; đọc `StageDefinition` | Gọi `GameObject`, chạm UI |
| **Unity** | Chuyển event → hình ảnh/âm thanh; đọc input → `ActionRequest`; chuyển SO → POCO | Chứa luật chơi, tự sửa `Hp`, tự tính sát thương |
| **UI** | Hiển thị, chọn lệnh, dự báo Clash | Tự quyết định kết quả, tự trừ máu |

### 2.2 Assembly Definition

```
Assets/_Project/
  Core/      BTH.Core.asmdef        → noEngineReferences = true, autoReferenced = false
  Content/   BTH.Content.asmdef     → refs: BTH.Core
  Game/      BTH.Unity.asmdef       → refs: BTH.Core, BTH.Content, Unity.InputSystem
  UI/        BTH.UI.asmdef          → refs: BTH.Core, BTH.Unity, Unity.TextMeshPro
  Editor/    BTH.Editor.asmdef      → refs: tất cả, includePlatforms: [Editor]
  Tests/     BTH.Core.Tests.asmdef  → refs: BTH.Core, BTH.Content, nunit.framework
```

Bật `noEngineReferences` cho `BTH.Core` là **cách rẻ nhất để cấm** lớp 1 lén dùng `UnityEngine`.

### 2.3 Ai giữ "sự thật"?

| Dữ liệu | Nơi giữ | Ghi chú |
|---------|---------|---------|
| Máu, AP, vị trí, trạng thái | `UnitState` (Core) | View chỉ đọc để vẽ |
| Địa hình, vật cản | `CellState` trong `IGridMap` | Đổi địa hình phát `TerrainChanged` |
| Ý định của quái (telegraph) | `EnemyIntent` (Core) | Sinh ở pha Telegraph, thi hành ở pha Enemy |
| Kết quả Clash | `ClashResolver` (Core) | UI chỉ hiển thị dự báo do Core trả về |
| Seed RNG | `GameSnapshot` | Rewind phải khôi phục cả seed |
| Cài đặt âm thanh/hiển thị | `SettingsModel` (Unity/Persistence) | Không nằm trong snapshot |
| Kiến thức về boss (codex) | `IRunKnowledge` (Core, **ngoài snapshot**) | Chết vẫn nhớ chiêu boss — đúng cốt truyện |

---


## 3. Cây thư mục — chuẩn hóa, tối đa 2 cấp

Cây cũ chia quá nhỏ (mỗi tính năng một nhánh riêng, sâu 4–5 cấp) → khó nhớ, khó tìm chỗ sửa. Bản chuẩn hóa dưới đây gộp theo **miền trách nhiệm**, không theo tính năng:

```
Assets/_Project/
├── Core/                # asmdef BTH.Core — C# thuần, KHÔNG UnityEngine
│   ├── Grid/            # lưới, ô, địa hình, tìm đường (A*)
│   ├── Units/           # đơn vị, registry, footprint, chỉ số
│   ├── Turn/            # pha, AP, thứ tự lượt
│   ├── Actions/         # command, validator, executor
│   ├── Combat/          # sát thương, effect, status, clash
│   ├── Ai/              # brain, intent, telegraph, boss phase
│   └── Run/             # session, stage, objective, snapshot, event, rng
│
├── Content/             # asmdef BTH.Content — dữ liệu + impl nội dung
│   ├── Data/            # *.asset: Unit, Skill, Stage, Terrain, Clash, Dialogue
│   ├── Skills/          # class kỹ năng đặc biệt
│   ├── Enemies/         # brain quái
│   ├── Bosses/          # 5 boss + phase controller
│   ├── Terrain/         # ITerrainBehaviour
│   └── Story/           # dialogue graph
│
├── Game/                # asmdef BTH.Unity — runtime Unity (view/input/audio/vfx)
├── UI/                  # asmdef BTH.UI — HUD, Clash, Dialogue, Menu
├── Editor/              # asmdef BTH.Editor — tool + DataValidator
├── Tests/               # asmdef BTH.Core.Tests — EditMode thuần C#
│
├── Art/                 # sprite, tileset, portrait, font
├── Audio/               # bgm, sfx
├── Scenes/              # Boot, MainMenu, Battle
└── Prefabs/             # Board, Units, UI, Vfx
```

**Quy ước để không phải nhớ cây:**

1. **Tối đa 2 cấp** dưới `Assets/_Project`. Muốn thêm cấp 3 phải có lý do ghi trong PR.
2. **1 file = 1 kiểu** (class/interface/struct), tên file trùng tên kiểu → tìm bằng Ctrl+, hoặc grep tên kiểu là ra.
3. Folder nào **dưới 3 file thì gộp lên folder cha**; file nào **trên ~400 dòng mới được tách**.
4. Không tạo folder theo tính năng (F01, F02…) — bảng 4.1 là bản đồ tính năng, không phải cây thư mục.
5. Code Unity không bao giờ nằm trong `Core/`; dữ liệu không bao giờ nằm trong `Game/`.
6. Đặt tên file theo **tiền tố miền** khi cần gom nhóm: `GridMap.cs`, `TurnManager.cs`, `ClashResolver.cs`, `BossMalkor.cs`.

**Bảng tra nhanh — "muốn sửa X thì vào đâu":**

| Muốn sửa | Mở folder | File tiêu biểu |
|----------|-----------|----------------|
| Kích thước bàn, tường, ô nứt, LOS | `Core/Grid` | `GridMap.cs`, `CellState.cs` |
| Đường đi, vùng di chuyển | `Core/Grid` | `AStarPathfinder.cs` |
| HP/ATK/MOV, boss 2×2 | `Core/Units` + `Content/Data` | `UnitRegistry.cs`, `UnitDataAsset` |
| Thứ tự lượt, AP, turn limit | `Core/Turn` | `TurnManager.cs`, `DefaultGameRules.cs` |
| Nút bấm nào hợp lệ, vì sao bị chặn | `Core/Actions` | `ActionValidator.cs` |
| Sát thương, hệ, Stagger/Guard | `Core/Combat` | `DamageCalculator.cs`, `StatusSet.cs` |
| Chiêu boss, Clash, Phase 2 | `Core/Ai` + `Core/Combat` + `Content/Bosses` | `ClashResolver.cs`, `BossMalkor.cs` |
| Chết/tua lại, RESET | `Core/Run` | `SnapshotService.cs` |
| Thắng thua, mục tiêu màn | `Core/Run` | `ObjectiveEvaluator.cs` |
| HUD, Clash UI, hội thoại | `UI` | `HudPresenter.cs`, `ClashPresenter.cs` |
| Nhập chuột/phím, camera, âm thanh | `Game` | `UnityInputSource.cs`, `UnityAudioService.cs` |

## 4. Phân rã tính năng

Cột **Core** = file/hợp đồng thuộc lớp 1. **Triển khai** = lớp cụ thể của Core. **Logic** = nội dung gameplay. **UI** = hiển thị/điều khiển.

### 4.1 Bảng tổng hợp

| ID | Tính năng | Core (hợp đồng + thuật toán) | Triển khai chi tiết | Logic / Content | UI / Presentation |
|----|-----------|------------------------------|---------------------|-----------------|-------------------|
| F00 | Khung game & composition root | `GameSession`, `IGameStateView` | `GameSession.cs` | `ServiceInstaller` | `GameBootstrap.cs` (Unity) |
| F01 | Bàn cờ ô lưới & địa hình | `IGridMap`, `GridCoord`, `CellState`, `TerrainId`, `GridShapes` | `GridMap`, `OccupancyIndex`, `TerrainCatalog` | `CrackedFloor/Pit/PoisonMiasma/Lava/Chest` | `BoardView`, `CellView`, `BoardHighlighter` |
| F02 | Đơn vị & chỉ số | `UnitDefinition`, `UnitState`, `IUnitRegistry` | `UnitRegistry` | `UnitDataAsset` (Wanderer, Eosphoros, Imp, Gargoyle, 5 boss) | `UnitView`, `HpPipsView` |
| F03 | Lượt, pha & điểm hành động | `ITurnManager`, `TurnPhase`, `ActionPoint` | `TurnManager`, `SpeedInitiativePolicy` | `DefaultGameRules` (2 AP, 20–25 lượt) | `TurnBannerView`, `ApIndicatorView`, `CommandBarView` |
| F04 | Di chuyển & tìm đường A* | `IPathfinder`, `PathRequest/Result`, `ReachableSet` | `AStarPathfinder`, `MovementRangeCalculator` | `MoveProfile` theo đơn vị | `BoardHighlighter` (ô xanh + mũi tên) |
| F05 | Đòn đánh, sát thương, ma trận hệ | `IDamageCalculator`, `DamageRequest`, `ElementMatrix` | `DamageCalculator`, `CombatMath` | `ElementMatrixAsset` (Phần 7.3), `SkillDataAsset` | `DamageNumberSpawner`, tooltip |
| F06 | Kỹ năng & pipeline hiệu ứng | `IGameAction`, `SkillAction`, `IGameEffect`, `EffectQueue` | `ActionValidator`, `EffectQueue` | `SwiftSlashSkill`, `NecroticBurstSkill`, `ProtectSkill`, `DodgeSkill`, `TemporalStepSkill`... | `CommandBarView`, `TooltipController` |
| F07 | AI & báo trước đòn (Telegraph) | `IEnemyBrain`, `EnemyIntent`, `ITelegraphService` | `TelegraphService` | `MeleeRusherBrain`, `RangedLurkerBrain`, `BossBrainBase` | `CellView` viền đỏ, `DangerZoneLegend` |
| F08 | Pha Boss & Đấu kỹ Clash | `IClashResolver`, `ClashPrediction`, `IBossPhaseController` | `ClashResolver`, `ClashRule` | 5 class boss + `ClashMatrixAsset` (Phần 7.2) | `ClashScreenView`, `ClashPresenter`, `PredictionBadgeView` |
| F09 | Trạng thái (Stagger/Flank/Root/Poison/Guard) | `IStatusEffect`, `StatusSet` | `StatusContext`, `StatusSet` | `StaggerStatus`, `FlankedStatus`, `RootStatus`, `PoisonStatus`, `GuardStatus` | Icon trạng thái trên `UnitView` |
| F10 | Chết & tua ngược / RESET | `GameSnapshot`, `ISnapshotStore`, `SnapshotTrigger` | `SnapshotService`, `SnapshotStore` | `IRunKnowledge` (nhớ chiêu boss) | Nút RESET, `ClashAnimator`/VFX đồng hồ vỡ |
| F11 | Màn chơi, mục tiêu, thắng–thua, giới hạn lượt | `StageDefinition`, `IObjectiveEvaluator`, `StageResult` | `StageLoader`, `ObjectiveEvaluator` | `Stage_01..05` asset | `StageSelectView`, `EndingView` |
| F12 | Cốt truyện, hội thoại, 2 kết thúc | `DialogueNode`, `DialogueRunner` | `DialogueRunner` | `DialogueAsset` (Hồi I–III, Ending A/B) | `DialogueView`, `DialoguePresenter` |
| F13 | HUD & bảng lệnh | chỉ event | — | — | `HudView/Presenter`, `CommandBarView` |
| F14 | Đấu kỹ UI | `ClashPrediction` | — | — | `ClashScreenView`, `SkillCardView` |
| F15 | Menu & cài đặt | `ISettingsStore` | `PlayerPrefsSettingsStore` | `GameConfigAsset` | `SettingsView`, `PauseView` |
| F16 | Âm thanh BGM/SFX | `IAudioService` (interface ở Unity) | `UnityAudioService` | `AudioCatalogAsset` | — |
| F17 | Điều khiển chuột + phím | `IInputSource` (interface) | `UnityInputSource` | — | `InputRouter`, `SelectionController` |
| F18 | Hình ảnh, hoạt ảnh, VFX | event | — | `AudioCatalog`/VFX prefab | `SpriteAnimator`, `VfxPlayer`, `BattleCamera` |
| F19 | Lưu & tải | `ISettingsStore` | — | — | — (không lưu tiến trình theo thiết kế) |
| F20 | Công cụ Editor & test | — | — | `DataValidator` | `StageEditorWindow` |

### 4.2 Ghi chú thiết kế cho các tính năng then chốt

**F01 — Lưới nhiều tầng thông tin, không chỉ "ô trống/ô tường".**
Mỗi `CellState` phải chứa: `TerrainId`, cờ `BlocksMove / BlocksLos / IsHazard`, `HazardDamage`, bộ đếm `TurnsUntilCollapse` (ô nứt sập sau 1 lượt), và `Occupant`. Ô "sàn nứt" không chặn đi nhưng đổi hành vi khi bị đánh → do `ITerrainBehaviour` quyết định, không phải `if (kind == Cracked)` rải rác trong Core.

**F02 — Đơn vị có footprint.**
Quỷ Vương Malkor là 2×2 (Phần 7.1: "kích thước 2x2 dễ trúng đòn"). Vì vậy `IUnitRegistry` + `OccupancyIndex` phải hỗ trợ đơn vị chiếm nhiều ô, và `GridShapes` phải trả về **tập ô giao nhau** giữa đòn AoE và footprint. Đừng giả định 1 đơn vị = 1 ô.

**F03 — Bốn nhịp, không phải hai pha.**
Tài liệu mô tả Telegraph trước, Player sau, Enemy thi hành. Mô hình đúng:

```
TurnIndex N:
  1. Telegraph  : AI lập EnemyIntent + phát ô nguy hiểm (chưa gây sát thương)
  2. Player     : người chơi dùng 2 AP (1 Move + 1 Action, thứ tự tự do)
  3. Enemy      : thi hành đúng intent đã báo (không tính lại AI)
  4. TurnEnd    : tick status, tick địa hình, kiểm tra giới hạn lượt, +1 TurnIndex
```

Thi hành đúng intent đã báo là điều kiện để người chơi "tin" được telegraph — cũng là điều kiện để test tự động.

**F07 — Telegraph là dữ liệu, không phải animation.**
`EnemyIntent` gồm `SkillId`, `TargetCell`, `DangerCells`, `Effects`, `Priority`. Pha Enemy chỉ `foreach (intent) Resolve(intent)`. Nhờ vậy: (a) UI vẽ viền đỏ chỉ từ dữ liệu; (b) test khẳng định "ô báo đỏ = ô thực sự bị đánh"; (c) rewind khôi phục intent như một phần state.

**F08 — Clash tất định, có dự báo trước.**
Không có RNG trong Clash. `Predict()` và `Resolve()` dùng **cùng một hàm tính uy lực** (`EffectivePower`), nên dự báo không bao giờ lệch kết quả. UI gọi `GetOptions()` để hiện 3 badge Win/Draw/Lose cho từng skill trước khi người chơi bấm.

**F10 — Snapshot, không phải replay.**
Turn-based nên state nhỏ; chụp ảnh trạng thái ở đầu mỗi pha rẻ hơn nhiều so với replay command (vốn đòi hỏi tất định tuyệt đối ở mọi thứ, kể cả animation). Nhưng vẫn **giữ command log** để debug/tái hiện bug.

**F11 — Thắng thua là hàm thuần.**
`ObjectiveEvaluator.Evaluate(state, stage)` chỉ đọc state và trả `StageResult`. Không chạm UI, không coroutine → test được toàn bộ 5 màn bằng unit test.

**F12 — Hội thoại là dữ liệu, không phải cutscene code.**
`DialogueNode` (id, speaker, text, next, điều kiện). `DialogueRunner` chạy graph; Unity chỉ hiển thị. Ending A/B chọn theo `DialogueChoiceNode` → `StageResult`.

**F13/F14 — UI chỉ bind.**
`HudPresenter` nghe event và set text/sprite. Không đọc `UnitState` mỗi frame (trừ thanh máu animation), không tự tính AP.

---


## 5. Hợp đồng Core — danh sách hàm phải làm

> Phần này chốt **tên · trách nhiệm · ai gọi**. Chữ ký chi tiết (kiểu tham số, event, generic) sẽ viết khi vào code; miễn giữ đúng ranh giới dưới đây là kiến trúc không vỡ. Không có code mẫu — cố ý.

Cách đọc: cột **Thành viên chính** ghi `Tên(...)` = hàm, `Tên` = thuộc tính/dữ liệu. `P0…P3` là thứ tự code (xem 5.15).

### 5.1 Grid & Terrain — Core/Grid

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `GridCoord` (P0) | Toạ độ ô nguyên. `X, Y`, `None`, `IsValid`, `Manhattan(a,b)`, `Chebyshev(a,b)`, phép `+ - == !=` | tất cả |
| `TerrainId` | ID địa hình **dạng chuỗi** (floor/wall/pit/cracked/poison/lava/chest) → thêm địa hình không sửa enum | GridMap, TerrainCatalog |
| `TerrainFlags` | Cờ bit: `BlocksMove`, `BlocksLineOfSight`, `IsHazard`, `IsCover`, `IsInteractive` | GridMap, DamageCalculator |
| `CellState` | Dữ liệu một ô: `Terrain`, `Flags`, `HazardDamagePerTurn`, `TurnsUntilCollapse`, `Occupant`, `IsOccupied` | mọi nơi đọc bàn cờ |
| `IGridMap` (P0) | Truy vấn: `Width/Height`, `InBounds`, `Get/TryGet`, `IsBlocked/IsWalkable(c, profile)`, `HasLineOfSight`, `MovementCost` | pathfinder, skill, AI |
| — hình học | `Neighbours4(c)`, `Line(from,to)` (Bresenham + chặn vật cản), `Cross(c,r)`, `Square(c,r)`, `Cone(origin,dir,len)`, `Row` — **dùng chung** cho skill, AoE, telegraph | skill, AI, UI highlight |
| — thay đổi | `SetTerrain`, `ArmCollapse(c,turns)`, `TickCollapse()` (TurnEnd), `LockArea(cells)/UnlockArea()/IsLocked()` (khoá đấu trường Phase 2), event `CellChanged` | TurnManager, boss |
| `GridShapes` | Hàm thuần tính tập ô cho từng hình; phải **giao với footprint** đơn vị 2×2 | skill, damage |

**Bất biến:** không có `if (kind == Cracked)` rải rác — hành vi ô do `ITerrainBehaviour` (Content/Terrain) quyết định.

### 5.2 Đơn vị & chỉ số — Core/Units

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `UnitId` | ID nguyên, `None`, `IsValid` | khắp nơi |
| `UnitFaction` / `UnitArchetype` | Player/Ally/Enemy/Neutral; Hero/Ally/Minion/Elite/StageBoss/FinalBoss | registry, brain |
| `MoveProfile` | `MovePoints`, `CanFly` (bỏ qua bẫy/vực), `IgnoresHazard`, `CanPassUnits`; factory `Walk(n)`, `Fly(n)` | pathfinder, GridMap |
| `UnitDefinition` (POCO) | Hồ sơ bất biến: `Id, DisplayName, Faction, Archetype, MaxHp, Attack, Armor, Initiative, SizeX/SizeY, Move, NativeElement, SkillIds, BrainId, PortraitId` | registry, SO loader |
| `UnitState` (P0) | Trạng thái runtime: `Position`, `Hp/MaxHp/HpRatio/IsAlive`, `ActionPoints`, `HasMovedThisTurn/HasActedThisTurn`, `Statuses`, `IsStaggered/IsRooted/IsGuarding`, `Footprint`, `IsBoss` | action, AI, UI |
| `IUnitRegistry` (P0) | `All/Alive(faction)`, `Player`, `Boss`, `Get/TryGet/At(cell)`, `Add(def, anchor)/Remove/MoveTo`, `InCells(cells)/AdjacentTo(cell)`, `CanPlace(def, anchor, ignore)` | action, AI, view |
| `OccupancyIndex` | Map ô → UnitId cho footprint nhiều ô; cập nhật khi move/spawn/die | registry |

**Bất biến:** `UnitState` chỉ bị sửa bởi action/effect; UI không gán HP.

### 5.3 Lượt, pha & AP — Core/Turn

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `TurnPhase` | `None, StageIntro, Telegraph, Player, Enemy, TurnEnd, Clash, StageEnded` — **4 nhịp chính**, không phải 2 pha | toàn hệ |
| `ActionPoint` (P0) | Hằng số: mỗi lượt 2 AP = 1 Move + 1 Action | TurnManager, UI |
| `ITurnManager` (P0) | `Phase`, `TurnIndex/TurnLimit`, `ActiveUnit`; điều khiển: `StartStage(stage)`, `BeginTelegraph()`, `BeginPlayerPhase()`, `EndPlayerPhase()`, `BeginEnemyPhase()`, `EndEnemyPhase()`, `EndTurn()`, `BeginClash()`, `FinishStage(result)` | GameSession, UI |
| — AP | `CanSpend(unit,ap)`, `TrySpend`, `ResetActionPoints(faction)` | action layer |
| — thứ tự | `EnemyOrder` sắp theo Initiative ↓ rồi UnitId ↑ (tất định) | EnemyPhase |
| `IInitiativePolicy` | `Order(enemies)` — tách để đổi luật thứ tự không sửa TurnManager | TurnManager |

**Bất biến:** `EndTurn()` chạy đúng thứ tự: tick status → tick địa hình (ô nứt sập) → evaluate objective → `TurnIndex++` → push snapshot.

### 5.4 Command / Action — Core/Actions

Cửa **duy nhất** để đổi state.

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `ActionKind` | Move, Attack, Skill, Guard, Wait, EndTurn, Reset, ClashSkill | input, UI |
| `ActionError` | Lý do từ chối: NotPlayerPhase, NoActionPoints, OutOfRange, TargetEmpty, NoLineOfSight, CellBlocked, CellOccupied, NotEnoughMovePoints, SkillNotReady, SkillOnCooldown, UnitDead, CannotMoveRooted, StageEnded… | validator → UI tooltip |
| `ActionRequest` (P0) | Dữ liệu yêu cầu: `Actor, Kind, TargetCell, SkillId, MoveTo` | input → session |
| `ValidationResult` | `IsValid`, `Error`, `Message` (hiện cho người chơi) | UI |
| `IActionValidator` (P0) | `Validate(request, state)` — **thuần, không đổi state**, gọi mỗi lần hover; `GetValidTargets(request, state)` — ô để tô sáng | UI, session |
| `IGameAction` | Hợp đồng command: `Id` (log/replay), `Kind`, `Actor`, `Target` | executor |
| — command cụ thể | `MoveAction`, `AttackAction`, `SkillAction`, `GuardAction`, `EndTurnAction` | input |
| `IGameActionExecutor` | `Execute(action, state) → ActionResult`; gom event, **publish sau khi state ổn định** | GameSession |

**Bất biến:** UI gọi `Validate` trước, chỉ khi `IsValid` mới gửi command; mọi thay đổi đi qua effect queue.

### 5.5 Hiệu ứng (Effect pipeline) — Core/Combat

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `IGameEffect` | Một đơn vị thay đổi: `Priority`, `Resolve(ctx)` | EffectQueue |
| `EffectQueue` (P1) | `Enqueue(effect)`, `Run(ctx)` — chạy theo priority, dừng nếu actor chết, phát event sau mỗi effect | executor, skill |
| `EffectContext` | Tham chiếu state, registry, queue, RNG, event bus, `Source/Target` | mọi effect |
| effect có sẵn | Damage, Heal, Push (bị tường chặn), Teleport, SpawnTerrain, ArmCollapse, ApplyStatus, RemoveStatus, LockArena, CameraShakeHint… | skill, boss, terrain |
| `ISkillBehaviour` | Phần logic đặc biệt của skill: `BuildEffects(ctx)` (chỉ xếp effect, không tự trừ HP) | Content/Skills |

**Bất biến:** effect **không** gọi trực tiếp view; mọi hiển thị đi qua event.

### 5.6 Sát thương & ma trận hệ — Core/Combat

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `ElementId` / `ElementMatrix` | Hệ (Physical, Fire, Holy, Necrotic, Piercing…) và bảng nhân hệ × hệ | DamageCalculator |
| `DamageRequest` (P1) | `Source, Target, BasePower, Element, IsSkill, IgnoresArmor, HitFrom` | skill, AI |
| `DamageResult` | `FinalDamage`, `WasStaggered/WasGuarded/WasFlanked`, `Killed` — để UI hiện số | event, UI |
| `IDamageCalculator` (P1) | `Compute(request) → DamageResult`; `Apply(result)` — áp dụng, clamp HP ≥ 0, phát `DamageDealt` | executor |
| `CombatMath` | Công thức thuần: Armor, Guard ×0.2, Stagger ×2, Flanked ×1.5, Holy vs Quỷ ×1.5 | calculator |

### 5.7 Trạng thái — Core/Combat

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `StatusId` / `StatusIds` | Hằng ID: Stagger, Flanked, Root, Poison, Guard | status set |
| `IStatusEffect` | `Duration`, `OnApply/OnTurnStart/OnTurnEnd/OnRemoved/OnDamaged` | StatusSet |
| `StatusSet` (P1) | `Add/Remove/Has/Has<T>`, `TickTurnStart()`, `TickTurnEnd()` (Poison −3 HP, Guard hết hạn), phát `StatusApplied/StatusExpired` | turn, effect |
| 5 status cụ thể | `StaggerStatus` (nhận ×2, 1 lượt), `FlankedStatus` (×1.5 đòn kế), `RootStatus` (0 move), `PoisonStatus` (3 HP/lượt), `GuardStatus` (−80%) | Content/Skills hoặc Core/Combat |

**Bất biến:** duration đếm theo **lượt của chủ thể**, không theo frame; hết hạn phải phát event.

### 5.8 Đấu kỹ Clash — Core/Combat

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `ClashRule` / `ClashRuleTable` (P2) | Khoá theo **cặp** (playerSkillId, bossSkillId) → `CounterBonus`; fallback theo element; mặc định +3 | ClashResolver |
| `IClashResolver` (P2) | `Resolve(request) → ClashOutcome`; `Predict(request) → ClashPrediction`; `GetOptions()` — 3 lựa chọn skill + badge | Clash UI, boss |
| `ClashOutcome` | `Win/Draw/Lose`, `PlayerPower`, `BossPower`, `AppliedEffect` (Stagger / poison ngược / Flanked) | turn, UI |
| `EffectivePower` | **Một** hàm tính uy lực dùng chung cho cả Predict và Resolve (không RNG) | resolver |

**Bất biến:** `Predict()` luôn bằng `Resolve()` với cùng state — đây là điều kiện để boss Clash công bằng.

### 5.9 AI & Telegraph — Core/Ai

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `IEnemyBrain` (P1) | `Plan(self, state) → EnemyIntent` — thuần, tất định, không đọc Unity | Telegraph phase |
| `EnemyIntent` | `UnitId, SkillId, TargetCell, DangerCells, Effects, Priority` — **bất biến sau khi công bố** | telegraph, UI, snapshot |
| `ITelegraphService` (P1) | `Declare(intent)`, `Clear()`, `DangerCells()` — dữ liệu cho UI tô đỏ | UI, turn |
| brain có sẵn | `MeleeRusherBrain`, `RangedLurkerBrain`, `BossBrainBase`, `AllyBrain` (Eosphoros) | Content/Enemies |
| `IBossPhaseController` (P2) | `ShouldEnterPhase2(hp, maxHp)` (ngưỡng 70%), `EnterPhase2(ctx)` — khoá sân, đổi moveset | boss, turn |

### 5.10 Tìm đường — Core/Grid

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `IPathfinder` (P0) | `FindPath(request) → PathResult` (đường + cost), `ComputeReachable(request) → ReachableSet` (vùng đi được trong MOV) | validator, UI |
| `PathRequest/PathResult` | `From, To, MoveProfile, IgnoreUnits` / `Path`, `Cost`, `Status` (Found/NoPath/OutOfRange) | pathfinder |
| `ReachableSet` | Tập ô + cost; dùng cho highlight xanh và kiểm tra "ô này đi được trong 2 bước?" | UI, validator |
| `AStarPathfinder` (P0) | A* 4 hướng, **không đi chéo**, cost theo terrain, chặn bởi unit trừ khi `CanPassUnits` | mọi nơi |

### 5.11 Tua ngược / Snapshot — Core/Run

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `GameSnapshot` (P2) | Ảnh trạng thái: grid, units, status, AP, TurnIndex, RNG state, intent, phase, run knowledge | store |
| `ISnapshotService` | `Capture()`, `Restore(snapshot)`, `PushCheckpoint(trigger)` (StageStart/TurnStart/ClashStart), `CanRewind` | session, turn |
| `SnapshotTrigger` | Lý do chụp — quyết định "chết thì tua về đâu" | session |
| `ISnapshotStore` | Cất/giữ N ảnh gần nhất; không serialize ra đĩa (theo thiết kế) | service |
| `IRunKnowledge` (P3) | Tri thức giữ **qua** rewind: đã thấy chiêu boss nào, đã chết bao nhiêu lần → dùng cho thoại/UI | rewind, story |

**Bất biến:** `Restore(Capture())` phải cho state bằng nhau từng trường (test bắt buộc).

### 5.12 Event — Core/Run

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `IEventBus` (P1) | `Subscribe<T>/Unsubscribe<T>`, `Publish<T>(in T)`, `Clear()` — struct, không boxing | core, UI |
| 25 event | `StageStarted`, `TurnPhaseStarted`, `TurnAdvanced`, `UnitSpawned`, `UnitMoved`, `UnitActed`, `ApChanged`, `DamageDealt`, `UnitDied`, `StatusApplied`, `StatusExpired`, `SkillUsed`, `IntentDeclared`, `TelegraphCleared`, `CellChanged`, `CellCollapsed`, `HazardTriggered`, `ArenaLocked`, `BossPhaseChanged`, `ClashStarted`, `ClashResolved`, `StageEnded`, `RewindPerformed`, `SnapshotPushed`, `RunKnowledgeUpdated` | view, presenter |

**Bất biến:** mỗi thay đổi state phát **đúng một** event; UI không tự suy ra state từ delta.

### 5.13 Session, Rules, RNG — Core/Run

| Kiểu | Trách nhiệm & thành viên chính | Ai dùng |
|------|-------------------------------|---------|
| `GameSession` (P2) | Façade duy nhất cho Unity: `TryExecute(request) → ActionResult`, `EndTurn()`, `Reset()`, `StartStage(stageId)`, `State` | input, UI, bootstrap |
| `IGameRules` | Toàn bộ số cân bằng: `ApPerTurn`, `MovePoints`, `TurnLimit`, `Phase2Threshold`, `GuardMultiplier`, `StaggerMultiplier`, `FlankMultiplier`, `PoisonPerTurn` | turn, combat |
| `StageDefinition` (POCO) | `Id, Width/Height, PlayerSpawn, Enemies, TerrainOverrides, TurnLimit, BossId, Objective` | loader |
| `IObjectiveEvaluator` (P2) | `Evaluate(state, stage) → StageResult` — hàm thuần: player chết / hết lượt / boss chết | session |
| `StageResult` | Victory, DefeatPlayerDead, DefeatTurnLimit, `EndingA/EndingB` | turn, UI |
| `IRandomSource` | `NextInt/NextFloat/State` — seed cố định, **cấm dùng `Random.` của Unity** (Core không có UnityEngine) | mọi nơi cần random |

### 5.14 Hợp đồng cho Unity / UI (lớp 3–4)

| Interface | Thuộc lớp | Trách nhiệm & thành viên chính |
|-----------|-----------|-------------------------------|
| `IGameStateView` | Core (đọc) | Mặt đọc state cho validator/AI: `Grid`, `Units`, `Turn`, `Rules` |
| `IInputSource` | Unity | `Poll() → ActionRequest?` — chuyển chuột/phím thành command, không chứa luật |
| `IAudioService` | Unity | `PlayBgm(id)`, `PlaySfx(id)`, `SetVolume(channel, v)` |
| `ISettingsStore` | Unity | `Load/Save(SettingsModel)` — chỉ cấu hình, **không lưu tiến trình** |
| `SettingsModel` | Unity | `MasterVolume, MusicVolume, SfxVolume, Fullscreen, Language` |
| `DialogueRunner` | Core (dữ liệu) | `Start(graph)`, `Advance()`, `Choose(index)`, `CurrentNode` — Unity chỉ hiển thị |
| `ISkillBehaviour` / `ITerrainBehaviour` | Content | Điểm cắm mở rộng; xem 9.1/9.4 |

### 5.15 Checklist hàm core (thứ tự ưu tiên code)

| # | Hàm / nhóm | Folder | Ưu tiên | Tiêu chí xong |
|---|-----------|--------|---------|---------------|
| 1 | `GridCoord`, `IGridMap`, `GridShapes` | Core/Grid | P0 | Line/Cross/Cone đúng ô; LOS chặn bởi tường |
| 2 | `IUnitRegistry`, `UnitState`, footprint 2×2 | Core/Units | P0 | Malkor chiếm 4 ô, không chồng lấn |
| 3 | `AStarPathfinder`, `ComputeReachable` | Core/Grid | P0 | 8×8 có tường, cost 2, không chéo |
| 4 | `ITurnManager` 4 nhịp + AP | Core/Turn | P0 | Đúng thứ tự Telegraph→Player→Enemy→TurnEnd |
| 5 | `ActionValidator` + `MoveAction` | Core/Actions | P0 | Từ chối khi hết AP / ô bị chặn |
| 6 | `DamageCalculator` + ma trận hệ | Core/Combat | P1 | Holy vs Quỷ ×1.5, Stagger ×2, Guard ×0.2 |
| 7 | `EffectQueue` + effect cơ bản | Core/Combat | P1 | Thứ tự priority; đẩy lùi bị tường chặn |
| 8 | `StatusSet` + 5 status | Core/Combat | P1 | Trừ duration đúng lượt, hết hạn phát event |
| 9 | `TelegraphService` + 2 brain thường | Core/Ai | P1 | Cùng state → cùng intent (100 lần) |
| 10 | `SnapshotService` capture/restore | Core/Run | P2 | Restore → state bằng từng trường |
| 11 | `ClashResolver` + `ClashRuleTable` | Core/Combat | P2 | 5 cặp ở 7.2 ra đúng Win/Draw/Lose |
| 12 | `IBossPhaseController` cho 5 boss | Core/Ai + Content/Bosses | P2 | HP 69% → chuyển pha, khoá sân |
| 13 | `ObjectiveEvaluator` + `StageResult` | Core/Run | P2 | Chết / hết lượt / giết boss |
| 14 | `GameSession` façade | Core/Run | P2 | Integration 1 màn giả lập 20 lượt |
| 15 | `IRunKnowledge` | Core/Run | P3 | Rewind không xoá knowledge |
| 16 | `IInputSource` → `ActionRequest` | Game | P2 | Chơi tay được Ải 1 |
| 17 | `HudPresenter` + command bar | UI | P2 | AP/HP cập nhật đúng theo event |
| 18 | `ClashPresenter` + badge 3 màu | UI | P3 | Hiện đúng dự báo trước khi xác nhận |
| 19 | `DialogueRunner` + 2 ending | Core/Run + Content/Story | P3 | Hết Hồi I–III, mở đúng ending |
| 20 | `SettingsModel` + `SettingsView` | Game + UI | P3 | Lưu/khôi phục âm lượng, fullscreen |

> P0 = tuần 1, P1 = tuần 2–3, P2 = tuần 4–5, P3 = tuần 6.

## 6. Luồng xử lý

### 6.1 Một lượt đấu

```
Player bấm [End Turn] (hoặc hết AP)
        │
        ▼
TurnManager.EndTurn()
        │
        ├─► StatusSet.TickTurnEnd()        (Poison trừ 3 HP, Guard hết hạn…)
        ├─► IGridMap.TickCollapse()        (ô nứt sập → pit, phát CellCollapsed)
        ├─► ObjectiveEvaluator.Evaluate()  (chết? hết lượt? boss chết?)
        ├─► TurnIndex++
        └─► SnapshotService.PushCheckpoint(TurnStart)
        │
        ▼
TurnManager.BeginTelegraph()
        │
        ├─► foreach enemy: IEnemyBrain.Plan()  → EnemyIntent
        ├─► TelegraphService.Declare(intent)   → IntentDeclared event
        └─► UI tô ô đỏ (chỉ đọc DangerCells, không tự tính)
        │
        ▼
TurnManager.BeginPlayerPhase()
        │
        ├─► ResetActionPoints(Player) = 2
        └─► chờ ActionRequest từ InputRouter / CommandBar
                │
                ├─ Move  (1 AP, tối đa 2 ô)  → MoveAction
                └─ Slash/Guard/Skill (1 AP)  → SkillAction / GuardAction
        │
        ▼
TurnManager.BeginEnemyPhase()
        │
        └─► EnemyOrder (Initiative ↓, UnitId ↑)
                └─ foreach intent → EffectQueue.ResolveAll()   ← KHÔNG tính lại AI
        │
        ▼
quay lại EndTurn (vòng lặp)
```

### 6.2 Clash (Pha 2 boss)

```
DamageDealt → Boss.HpRatio < 0.70
        │
        ▼
BossPhaseController.ShouldTransition = true
        │
        ▼
EnterPhase2(state)
   ├─ IGridMap.LockArea(arena 4×4)          → BossPhaseChanged
   ├─ Boss immune to flee, player không ra ngoài
   └─ PickUltimate() → SkillDefinition (power 15/14/16/18/18)
        │
        ▼
TurnManager.BeginClash()  →  ClashStarted  →  ClashScreenView mở
        │
        ▼
ClashResolver.GetOptions(ctx)   ← Core trả sẵn (skill, prediction)
   Ví dụ Ải 1: Protect 15+3=18 → Win | SwiftSlash 14+3=17 → Win | NecroticBurst 13 → Lose
        │
        ▼
UI vẽ 3 thẻ + badge xanh/vàng/đỏ   (người chơi thấy trước khi bấm)
        │
        ▼
Confirm(skillId) → ClashResolver.Resolve()
        ├─ Win : 0 dmg nhận, SkillBreak, Boss Stagger 1 lượt (nhận 200%)
        ├─ Draw: cả hai nhận sát thương va chạm tối thiểu, bật lùi 1 ô
        └─ Lose: người chơi ăn trọn sát thương tuyệt kỹ
        │
        ▼
ClashResolved → VFX/Âm thanh → pha tiếp tục (Player hoặc Enemy)
```

### 6.3 Chết & Rewind

```
UnitDied(player)  hoặc  TurnIndex > TurnLimit
        │
        ▼
GameSession.RewindTo(SnapshotTrigger.StageStart)
        │
        ├─ RewindStarted (UI: đồng hồ La Mã vỡ, tua ngược)
        ├─ SnapshotService.Restore(snapshot)
        ├─ StageLoader.SpawnUnits()  (đồ item reset vị trí — đúng thiết kế "đồ mất, phải kiếm lại")
        ├─ IRunKnowledge GIỮ NGUYÊN  (biết chiêu boss từ vòng trước)
        └─ RewindFinished(turn = 1)
        │
        ▼
Người chơi chơi lại từ 100% HP của boss
```

### 6.4 Một frame của lớp Unity (không chứa luật)

```
Update()
  ├─ UnityInputSource → PlayerCommand
  ├─ SelectionController: hover ô → ActionValidator.Validate() (chỉ để tô màu)
  └─ nếu click: GameSession.Submit(request)
        │
        ├─ EventBus.Publish(DamageDealt / UnitMoved / …)
        ▼
Presenters: HudPresenter, BoardHighlighter, DamageNumberSpawner, VfxPlayer
        │
        ▼
View: chỉ set sprite/text/position
```

---

## 7. Mapping dữ liệu: ScriptableObject → Core

Quy tắc: **SO ở lớp Unity, POCO ở Core**. Mỗi asset có một hàm `ToDefinition()` thuần, không side effect.

| ScriptableObject (Unity) | POCO / hàm chuyển | Dùng bởi |
|--------------------------|-------------------|----------|
| `GameConfigAsset` | `DefaultGameRules` | `ITurnManager`, `DamageCalculator` |
| `UnitDataAsset` | `UnitDefinition` | `IUnitRegistry.Add` |
| `SkillDataAsset` | `SkillDefinition` + `ISkillBehaviour` (Content) | `SkillAction` |
| `StageDataAsset` | `StageDefinition` | `StageLoader`, `ObjectiveEvaluator` |
| `TerrainDataAsset` | `TerrainId` + `ITerrainBehaviour` | `TerrainCatalog` |
| `ClashMatrixAsset` | `ClashRuleTable` | `ClashResolver` |
| `ElementMatrixAsset` | `ElementMatrix` (`float[,]`) | `DamageCalculator` |
| `AudioCatalogAsset` | `id → AudioClip` | `UnityAudioService` |
| `DialogueAsset` | `DialogueGraph` | `DialogueRunner` |

Lưu ý: **`SkillDataAsset` chứa 2 phần** — dữ liệu thuần (id, power, element, range, AP cost, targeting) và một `MonoBehaviour`/`ScriptableObject` tham chiếu `ISkillBehaviour` cho phần logic đặc biệt. Nhờ vậy skill "đánh thường" không cần code, skill "Necrotic Ward hấp thụ độc rồi phản 3 HP/lượt" mới cần class.

---

## 8. Bảng số liệu chốt (lấy từ tài liệu thiết kế)

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
| Quỷ Vương Malkor | Boss 4 | 45 | 8 | 1 | toàn sân | **2×2** | Stagger khi trúng |
| Eosphoros sa ngã | Boss 5 | 30 | 6 | 2 | 1–2 | 1×1 | Thánh quang |

### 8.2 Ma trận Clash (Phần 7.2) — dữ liệu cho `ClashMatrixAsset`

| Ải | Tuyệt kỹ boss | Uy lực | Hệ | Skill thắng (base + bonus) | Kết quả |
|----|---------------|--------|-----|----------------------------|---------|
| 1 | Tro Tàn Cuồng Nộ | 15 | Physical | Protect 15+3=18, Swift Slash 14+3=17 | Win → Stagger 1 lượt |
| 2 | Bão Khí Độc Diệt Thế | 14 | Necrotic | Necrotic Burst 13+3=16 | Win → Vu Nữ tự trúng độc 3 HP/lượt ×2 |
| 3 | Tia Thương Hư Không | 16 | Piercing | Dodge 14+3=17, Swift Slash 14+3=17 | Win → Flanked |
| 4 | Cự Kiếm Hỏa Ngục | 18 | Fire | Temporal Step 16+3=19, Dodge 14+**5**=19 | Win → Stagger |
| 5 | Thánh Quang Trảm | 18 | Holy | Swift Slash 16+3=19, Protect 15+**4**=19 | Win → Stagger |

> Bonus **không** phải hằng số toàn cục: mặc định +3, nhưng cặp cụ thể có thể +4/+5. Vì vậy `ClashRule` khóa theo **cặp (playerSkillId, bossSkillId)**, fallback theo element.

### 8.3 Hằng số luật chơi

| Hằng số | Giá trị | Nguồn |
|---------|---------|-------|
| AP/lượt | 2 (1 Move + 1 Action) | Phần 3.1.2 |
| Move/lượt | 2 ô, 4 hướng, không chéo | Phần 3.2.1 |
| Melee range | 1 | Phần 3.2.1 |
| Ranged range | 2–3, cần LOS | Phần 3.2.1 |
| Turn limit | 20–25 / màn | Phần 3.4 |
| Ngưỡng Phase 2 | HP < 70% | Phần 3.1.3 |
| Stagger | 1 lượt, ×2 sát thương | Phần 3.1.3(d) |
| Guard | −80% sát thương | Phần 3.2.3 |
| Flanked | ×1.5 đòn kế tiếp | Phần 3.2.3 (Dodge) |
| Poison miasma | 3 HP/lượt | Phần 7.2 |
| Bảng màu | #E5C158, #2C4270, #6B3BA7, #8B1E1E, #3A3E45 | Phần 6.1 |

---

## 9. Công thức mở rộng (không sửa Core)

### 9.1 Thêm một kỹ năng mới
1. Tạo class `MySkill : ISkillBehaviour` trong `Content/Skills/` (kế thừa `SkillBehaviourBase`).
2. Override `BuildEffects(EffectContext)` — chỉ xếp effect, không tự sửa HP.
3. Tạo `SkillDataAsset` (id, power, element, range, targeting, AP cost) và gán behaviour.
4. Thêm id vào `UnitDataAsset.SkillIds`.
5. Nếu là skill phản đòn Clash: thêm dòng vào `ClashMatrixAsset`.
→ **Không** sửa file nào trong `Core/`.

### 9.2 Thêm quái mới
1. `UnitDataAsset` mới (HP/ATK/MOV/brain id).
2. Nếu hành vi mới: `MyBrain : IEnemyBrain` trong `Content/Enemies/`, đăng ký `BrainId`.
3. Đặt vào `StageDataAsset.Enemies`.
Hành vi cũ (đuổi theo, giữ khoảng cách) chỉ cần chọn `MeleeRusherBrain`/`RangedLurkerBrain` — không code.

### 9.3 Thêm boss / màn mới
1. `MyBoss : BossBrainBase, IBossPhaseController` — override `PickUltimate`, `EnterPhase2`.
2. `StageDataAsset` mới: kích thước, spawn, terrain override, turn limit, boss id.
3. Thêm 1 dòng `ClashMatrixAsset` cho tuyệt kỹ.
4. Thêm `DialogueAsset` nếu có thoại.
→ Stage 6 về sau không cần sửa Core.

### 9.4 Thêm địa hình mới
1. `MyTerrain : ITerrainBehaviour` (`BlocksMovement`, `OnEnter`, `OnTurnStart`, `OnDamaged`).
2. Đăng ký vào `TerrainCatalog` (dictionary `TerrainId → behaviour`).
3. Tạo `TerrainDataAsset` + tileset sprite.
> Vì `TerrainId` là chuỗi, không phải enum → không phải sửa Core, không phải rebuild enum index.

### 9.5 Thêm kết thúc / nhánh truyện
1. Thêm node vào `DialogueAsset` (`DialogueChoiceNode`).
2. Thêm `StageResult.EndingC` **chỉ khi** kết thúc mới thật sự mới về luật — nếu chỉ là thoại khác thì dùng `DialogueGraph`, không đụng `StageResult`.

---

## 10. Lộ trình code

| Sprint | Nội dung | Kết quả kiểm chứng được |
|--------|----------|--------------------------|
| **S0** (2–3 ngày) | Tạo project, asmdef, `BTH.Core` rỗng, test runner, `GameBootstrap` | `dotnet test`/`Unity Test Runner` chạy 1 test xanh; Core không compile nếu thêm `using UnityEngine` |
| **S1** (1 tuần) | F01 Grid + F02 Units + F04 Pathfinding + F03 Turn/AP | Bàn cờ 8×8 hiện, click 2 ô di chuyển được, AP trừ đúng, ô đỏ hiện |
| **S2** (1 tuần) | F05 Damage + F06 Skill/Effect + F09 Status + F07 AI thường | Đánh Imp chết 1 nhát, Gargoyle bắn 2–3 ô, telegraph khớp đòn thật |
| **S3** (1 tuần) | F08 Boss Phase + Clash + F14 Clash UI | Ải 1 chơi được trọn: P1 né rìu → P2 Clash → Stagger → thắng |
| **S4** (1 tuần) | F10 Rewind + F11 Stage/Objective + F13 HUD | Chết → tua lại, RESET, hết 20 lượt → thua, HUD đúng |
| **S5** (1 tuần) | F12 Story + F15 Settings + F16 Audio + F18 VFX | Chạy 5 ải, 2 ending, âm lượng/fullscreen lưu được |
| **S6** (1 tuần) | S6 5 màn + cân bằng + F20 Editor tool + test đủ | Build WebGL/Windows chơi từ menu → ending, không lỗi |

**Thứ tự code core bắt buộc** (theo phụ thuộc):
`GridCoord → IGridMap → UnitState → IUnitRegistry → IPathfinder → ITurnManager → IActionValidator → IGameAction → DamageCalculator → EffectQueue → StatusSet → IEnemyBrain → TelegraphService → SnapshotService → ClashResolver → IBossPhaseController → ObjectiveEvaluator → GameSession → Presenters`.

Definition of Done cho mỗi tính năng: (1) có interface trong Core; (2) có unit test thuần C#; (3) UI không tính toán luật; (4) thêm nội dung mới không sửa Core; (5) có tên trong bảng 4.1.

---

## 11. Kiểm thử & bảo trì

### 11.1 Test matrix (EditMode, không cần scene)

| Test | Khẳng định |
|------|-----------|
| `GridTests` | `Line`/`Cross`/`Cone` trả đúng ô; LOS bị chặn bởi tường; ô ngoài biên trả invalid |
| `PathfindingTests` | Không đi chéo; không xuyên tường; cost 2; `ReachableSet` khớp A* |
| `TurnFlowTests` | Thứ tự 4 pha; AP reset; enemy order theo Initiative; `TurnIndex` tăng đúng |
| `DamageTests` | Holy vs Quỷ ×1.5; Stagger ×2; Guard ×0.2; Temporal bỏ Armor; không âm HP |
| `ClashTests` | 5 cặp Phần 7.2: đúng Win/Draw/Lose; `Predict` == `Resolve`; chọn sai → Lose |
| `AiDeterminismTests` | Chạy `Plan` 100 lần cùng state → cùng intent; không cấp phát ngẫu nhiên |
| `SnapshotTests` | `Restore(Capture())` → state bằng nhau từng trường, kể cả RNG và intent |
| `StageObjectiveTests` | Chết → DefeatPlayerDead; quá 20 lượt → DefeatTurnLimit; boss chết → Victory |

### 11.2 Checklist review PR

- [ ] Không có `using UnityEngine` trong `Core/`.
- [ ] Không có `Random\.`, `Time\.`, `FindObjectOfType`, `Instantiate` trong `Core/`.
- [ ] Con số cân bằng mới nằm trong `IGameRules`/`ScriptableObject`, không hard-code.
- [ ] Mọi thay đổi state phát đúng 1 event; UI không đọc trực tiếp để tự sửa.
- [ ] Thêm tính năng mới có tên trong bảng 4.1 và test tương ứng.
- [ ] Không thêm phụ thuộc asmdef trái chiều (Core → Unity).
- [ ] `ActionValidator` từ chối đúng `ActionError`, UI hiện được lý do.

### 11.3 Anti-pattern phải tránh

| Sai | Đúng |
|-----|------|
| `unit.Hp -= dmg` trong `UnitView` | `DamageCalculator.Apply()` → `DamageDealt` → view cập nhật |
| `if (unit.Id == "malkor") ...` trong Core | `IBossPhaseController` theo từng boss |
| AI tính lại ở Enemy Phase | thi hành `EnemyIntent` đã công bố |
| `GameObject.Find("Player")` | inject qua `GameSession`/`IUnitRegistry` |
| Lưu tiến trình vào PlayerPrefs | chỉ lưu `SettingsModel`; tiến trình reset theo thiết kế |
| Coroutine trong Core | Core thuần, Unity tự animate theo event |

---

## 12. ADR — quyết định kiến trúc

| ID | Quyết định | Lý do | Hệ quả |
|----|-----------|-------|--------|
| ADR-01 | Rewind bằng **snapshot**, không replay command | Turn-based, state nhỏ; replay đòi hỏi tất định tuyệt đối ở animation/âm thanh | Tốn RAM ~vài chục KB/checkpoint; vẫn giữ command log để debug |
| ADR-02 | **Tất định** + RNG có seed | Rewind và test cần cùng input → cùng output | Cấm random trong Clash; AI phải sort ổn định |
| ADR-03 | ID dạng **chuỗi** (TerrainId/ElementId) | Thêm nội dung không phải sửa enum/Core | Mất kiểm tra compile-time, bù bằng `DataValidator` |
| ADR-04 | 4 lớp + **asmdef** | Ngăn Core lén dùng UnityEngine | Phải khai báo asmdef khi thêm thư mục |
| ADR-05 | **Không lưu tiến trình**, chỉ lưu settings | Thiết kế: chết là mất đồ, phải kiếm lại | `ISettingsStore` chỉ chứa `SettingsModel` |
| ADR-06 | Telegraph là **intent object** | Người chơi phải thấy đúng điều sẽ xảy ra | AI không được "đổi ý" sau khi công bố |

### Việc còn phải chốt với nhóm

1. **Rewind về đâu** khi chết trong Clash: đầu màn (`StageStart`) hay đầu Clash (`ClashStart`)? Tài liệu ghi "tua ngược trận đấu lại thời điểm bắt đầu trận Boss" → mặc định `StageStart`; để mở bằng `SnapshotTrigger`.
2. **Đồng minh Eosphoros** ở các ải đầu có tốn AP của người chơi hay tự hành động theo `AllyBrain`?
3. **MP** trong HUD (Phần 6.1) có dùng thật hay chỉ trang trí? Nếu dùng, thêm `Mana` vào `UnitState` + `SpendMana`.
4. **Đa kích thước boss**: Malkor 2×2 — cần thống nhất ô neo (góc trên-trái) và cách tính "đánh từ sau lưng".
5. **Lưu run**: nếu giảng viên yêu cầu lưu tiến trình thì mở rộng `ISaveStore` (đã tách sẵn).

---

*Hết tài liệu. Khi sửa kiến trúc, cập nhật bảng 4.1 + mục 5 tương ứng để tài liệu không lệch code.*
