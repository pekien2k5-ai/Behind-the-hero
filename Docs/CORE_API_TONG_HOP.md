# BEHIND THE HERO — Tổng hợp API Core (v2.0)

> Tài liệu tra cứu cho cả nhóm: **lớp `BTH.Core` đang có gì, dùng thế nào, sai chỗ nào thì sửa ở đâu.**
> Đối tượng: người viết Content (skill/quái/màn), người viết Unity/UI, người review.
>
> **Trạng thái 2026-11-16:** Core + Content hoàn tất — **163 + 30 file · 64 unit test xanh** (dotnet + Unity EditMode),
> gồm **Duel mode** cho Phase 2 với auto finisher. Unity/UI chưa có code.
> Kiến trúc & lý do thiết kế: `KIEN_TRUC_UNITY_BEHIND_THE_HERO.md`. Luật Duel đã chốt: `QUYET_DINH.md`.

---

## 0. Ba điều phải nhớ

1. **Luật nằm trong Core, không nằm trong UI.**
   UI chỉ đọc event / gọi `Validate` + `TryExecute`; không tự trừ HP, không tự tính damage, không tự quyết thắng thua.
2. **Mọi thay đổi state đi qua `ActionRequest`.**
   `ActionValidator.Validate` (thuần, chỉ đọc) → `GameActionExecutor.Execute` (sửa state) → event.
   Trong Duel cũng vậy: chỉ gửi `ActionRequest.ClashSkill`; thắng thì đòn kết liễu tự động, không gửi `Attack`.
3. **Thêm nội dung không sửa Core.**
   Unit/skill/brain/stage/duel đều là POCO do Content dựng từ ScriptableObject; Core chỉ thấy interface/POCO.

---

## 1. Bảng tra nhanh — "muốn làm X thì đọc/ở đâu"

| Muốn | Đọc | Ghi chú |
|------|-----|---------|
| Biết ô nào đi được, LOS | §3 `IGridMap`, `GridShapes` | `IsWalkable`, `HasLineOfSight` |
| Biết đường đi/chi phí | §3 `IPathfinder`, `AStarPathfinder` | `FindPath`, `ComputeReachable` |
| Spawn/move/kiểm tra quân | §4 `IUnitRegistry` | `Add`, `MoveTo`, `CanPlace` |
| Hiểu một unit runtime | §4 `UnitState` | HP/AP/status/cooldown |
| Biết lượt/pha hiện tại | §5 `ITurnManager`, `TurnPhase` | có `Duel` |
| Kiểm tra nút bấm có hợp lệ | §6 `IActionValidator` | trả `ValidationResult` + `ActionError` |
| Gửi lệnh từ UI | §6, §11 `GameSession.TryExecute` | trả `ActionResult` |
| Damage/hệ/giáp/status | §7 | `CombatMath`, `DamageCalculator`, `StatusSet` |
| Viết skill mới | §7.3 + §15.1 | `ISkillBehaviour.BuildEffects` |
| Clash pha 1 | §8 | `ClashResolver`, `ClashRuleTable` |
| Duel pha 2 | §9 | `IClashDuel`/`ClashDuel`, `DuelDefinition` |
| AI/telegraph | §10 | `IEnemyBrain`, `TelegraphService` |
| Snapshot/rewind | §11 | `SnapshotService`, `GameSnapshot` |
| Event để UI bind | §12 | 28 event, publish sau khi state ổn định |
| Chạy test | §17 | dotnet + Unity batchmode |

---

## 2. Bản đồ thư mục (163 file)

| Folder | File | Dòng | Nội dung |
|--------|------|------|----------|
| `Core/Grid` | 7 | 633 | `GridCoord`, `TerrainId`, `TerrainFlags`, `CellState`, `IGridMap`, `GridMap`, `GridShapes` |
| `Core/Units` | 18 | 1.060 | `UnitId`, faction/archetype, `MoveProfile`, `UnitDefinition`, `UnitState`, registry, `OccupancyIndex`, pathfinding (`IPathfinder`, `AStarPathfinder`, `PathRequest/Result`, `ReachableSet`, `PathNode`, `PathMinHeap`, `MovementRangeCalculator`) |
| `Core/Turn` | 6 | 431 | `TurnPhase`, `ActionPoint`, `IInitiativePolicy`, `InitiativePolicy`, `ITurnManager`, `TurnManager` |
| `Core/Actions` | 15 | 911 | `ActionKind`, `ActionError`, `ActionRequest`, `ValidationResult`, `ActionResult`, `IGameAction` + 5 command, `IActionValidator`, `ActionValidator`, `IGameActionExecutor`, `GameActionExecutor` |
| `Core/Combat` | 51 | 2.113 | Element/damage/skill/effect/status/Clash/**Duel** |
| `Core/Ai` | 8 | 268 | `EnemyIntent`, brain + library, `ITelegraphService`, `IBossPhaseController` |
| `Core/Run` | 57 | 1.946 | rules, RNG, `GameState`, `GameSession`, stage/objective, snapshot, knowledge, dialogue, `IEventBus` + **28 event** |
| `Core` (root) | 2 | — | `BTH.Core.asmdef`, `AssemblyInfo.cs` |

**File index theo folder** (để grep nhanh):

```
Grid:     GridCoord TerrainId TerrainFlags CellState IGridMap GridMap GridShapes
Units:    UnitId UnitFaction UnitArchetype MoveProfile UnitDefinition UnitState
          IUnitRegistry UnitRegistry OccupancyIndex PathStatus PathRequest PathResult
          ReachableSet PathNode PathMinHeap IPathfinder AStarPathfinder MovementRangeCalculator
Turn:     TurnPhase ActionPoint IInitiativePolicy InitiativePolicy ITurnManager TurnManager
Actions:  ActionKind ActionError ActionRequest ValidationResult ActionResult IGameAction
          MoveAction AttackAction SkillAction GuardAction EndTurnAction
          IActionValidator ActionValidator IGameActionExecutor GameActionExecutor
Combat:   ElementId ElementMatrix CombatMath DamageRequest DamageResult IDamageCalculator DamageCalculator
          SkillTargeting SkillDefinition ISkillBehaviour ISkillLibrary SkillLibrary
          IGameEffect EffectQueue EffectContext DamageEffect HealEffect ApplyStatusEffect
          RemoveStatusEffect PushEffect TeleportEffect SpawnTerrainEffect ArmCollapseEffect
          LockArenaEffect CameraShakeEffect
          StatusId StatusIds IStatusEffect StatusContext StatusEntry StatusSet
          StaggerStatus FlankedStatus RootStatus PoisonStatus GuardStatus
          ClashRule ClashRuleTable ClashDefinition ClashOutcome ClashPrediction ClashOption
          IClashResolver ClashResolver ClashResult ClashMath ClashRoundResult
          DuelDefinition DuelState IClashDuel ClashDuel
Ai:       EnemyIntent IEnemyBrain IEnemyBrainLibrary EnemyBrainLibrary
          ITelegraphService TelegraphService IBossPhaseController BossPhaseController
Run:      IGameRules GameRules IRandomSource XorShiftRandom IGameStateView GameState GameSession
          StageDefinition UnitSpawnDefinition TerrainOverride IObjectiveEvaluator ObjectiveEvaluator
          StageResult DialogueNode DialogueChoice DialogueGraph DialogueRunner
          ISnapshotStore SnapshotStore ISnapshotService SnapshotService SnapshotTrigger
          GameSnapshot UnitSnapshot StatusSnapshot DuelSnapshot IRunKnowledge RunKnowledge
          IEventBus EventBus RefEventHandler
          + 28 event file (xem §12)
```

---

## 3. Grid — lưới & địa hình

### 3.1 `GridCoord` (struct)

```csharp
new GridCoord(int x, int y)
int X, Y
static GridCoord None;          // (-1,-1)
static GridCoord North/South/East/West
bool IsValid;                   // X >= 0 && Y >= 0
static int Manhattan(GridCoord a, GridCoord b)
static int Chebyshev(GridCoord a, GridCoord b)
operator +, -, ==, !=
```

- Toạ độ là **ô**, gốc `(0,0)` ở góc dưới-trái theo quy ước của Core (không phụ thuộc Unity).
- `None` dùng cho "không có toạ độ"; đừng dùng `(0,0)` để biểu diễn "không có".

### 3.2 `TerrainId` / `TerrainFlags` / `CellState`

```csharp
// TerrainId: wrapper chuỗi, so sánh OrdinalIgnoreCase
TerrainId.Floor / Wall / Pit / Cracked / Poison / Lava / Chest / None
(TerrainId)"my-terrain"; (string)id

// TerrainFlags: [Flags]
None = 0, BlocksMove = 1, BlocksLineOfSight = 2,
IsHazard = 4, IsCover = 8, IsInteractive = 16
```

`CellState` (class, sửa được bởi Core):

| Thuộc tính | Ý nghĩa |
|-----------|---------|
| `Coord` | toạ độ ô |
| `Terrain`, `Flags` | địa hình + cờ |
| `HazardDamagePerTurn` | sát thương mỗi lượt nếu đứng trên |
| `TurnsUntilCollapse` | 0 = không sập; >0 = còn N lượt |
| `MovementCost` | mặc định 1 |
| `Occupant` | `UnitId` đang chiếm (None nếu trống) |
| `IsLocked` | ô bị khoá arena (không vào được) |
| `IsOccupied`, `BlocksMove`, `BlocksLineOfSight`, `IsHazard`, `IsInteractive` | tiện ích đọc |

### 3.3 `IGridMap` / `GridMap`

```csharp
int Width, Height
bool InBounds(GridCoord)
CellState Get(GridCoord)                 // ném ArgumentOutOfRange nếu ngoài lưới
bool TryGet(GridCoord, out CellState)
bool IsBlocked(GridCoord)                // BlocksMove || IsLocked
bool IsWalkable(GridCoord, MoveProfile)  // khoá + occupant + bẫy (xem dưới)
bool HasLineOfSight(GridCoord from, GridCoord to)   // Bresenham, bỏ 2 đầu
int  MovementCost(GridCoord)
bool IsLocked(GridCoord)

void SetTerrain(GridCoord, TerrainId, TerrainFlags, int movementCost = 1, int hazardDamagePerTurn = 0)
void ArmCollapse(GridCoord, int turns)
void TickCollapse()                       // gọi cuối lượt: ô hết hạn → Pit + BlocksMove + event
void LockArea(IEnumerable<GridCoord>)
void UnlockArea()
void SetOccupant(GridCoord, UnitId)

event Action<GridCoord> CellChanged       // C# event song song với CellChangedEvent
```

**Luật đi được (`IsWalkable`)** — thứ tự:

1. Ô ngoài lưới hoặc `IsLocked` → **không**.
2. Có unit khác và profile **không** `CanPassUnits` → **không**.
3. Ô `BlocksMove`:
   - đi bộ → **không**;
   - `CanFly` **chỉ** bay qua được nếu ô đó **không** `BlocksLineOfSight` → bay qua `Pit`/hazard được, nhưng **tường vẫn chặn**.
4. Còn lại → **được**.

**Sập ô:** `TickCollapse()` biến ô hết `TurnsUntilCollapse` thành `TerrainId.Pit` + `BlocksMove`, phát `CellCollapsedEvent`. `TurnManager.EndTurn` gọi nó sau bước hazard.

### 3.4 `GridShapes` (static)

```csharp
static GridCoord[] Directions4;                    // North, East, South, West
List<GridCoord> Neighbours4(GridCoord center)
List<GridCoord> Line(GridCoord from, GridCoord to)  // Bresenham, gồm 2 đầu
List<GridCoord> Cross(GridCoord center, int radius) // hình thoi Manhattan
List<GridCoord> Square(GridCoord center, int radius)// hình vuông Chebyshev
List<GridCoord> Cone(GridCoord origin, int dirX, int dirY, int length)
List<GridCoord> Row(GridCoord origin, int dirX, int dirY, int length)  // bỏ ô gốc
List<GridCoord> Footprint(GridCoord anchor, int sizeX, int sizeY)      // neo = góc min, +X/+Y
bool Contains(List<GridCoord> cells, GridCoord coord)
```

Lưu ý: các hàm trả **ô trong lưới hình học**, chưa lọc biên; nơi dùng phải tự lọc `grid.InBounds` nếu cần. Khi tính AoE cho unit 2×2, giao kết quả với `unit.Footprint`.

### 3.5 Pathfinding

```csharp
// PathStatus: Found, NoPath, OutOfRange, InvalidStart, InvalidTarget
struct PathRequest { Start, Goal, Profile, MaxCost = 0, IgnoreUnit = default }
struct PathResult  { Status, Path (gồm ô start khi Found), Cost; Found(...), Fail(...), IsFound, Goal }
class ReachableSet { Origin, Count, Cells (sort Y rồi X), Costs, Contains(c), CostTo(c) }
interface IPathfinder {
  PathResult FindPath(PathRequest request);
  ReachableSet ComputeReachable(GridCoord start, MoveProfile profile, int maxCost, UnitId self = default);
}
class AStarPathfinder : IPathfinder { AStarPathfinder(IGridMap grid); }
static class MovementRangeCalculator {
  ReachableSet Compute(IPathfinder, IGridMap, GridCoord start, MoveProfile, int movePoints, UnitId self);
}
```

Hành vi A*:

- 4 hướng, **không đi chéo**.
- Chi phí vào ô = `MovementCost` (tối thiểu 1).
- Tie-break: `f` → `h` → thứ tự push (để tất định).
- `MaxCost > 0`: hết ngân sách trước khi tới đích → `OutOfRange`; không có đường → `NoPath`.
- `IgnoreUnit` cho phép coi unit đó "không tồn tại" (dùng khi push/teleport).
- `ComputeReachable` là Dijkstra (không heuristic), bỏ ô start khỏi `Cells`.

---

## 4. Units — đơn vị

### 4.1 Kiểu cơ bản

```csharp
struct UnitId { int Value; static None = 0; bool IsValid => Value > 0; ==, != }
enum UnitFaction { Player, Ally, Enemy, Neutral }
enum UnitArchetype { Hero, Ally, Minion, Elite, StageBoss, FinalBoss }
struct MoveProfile { int MovePoints; bool CanFly, IgnoresHazard, CanPassUnits;
                     static Walk(n), Fly(n); ctor(movePoints, canFly, ignoresHazard, canPassUnits) }
```

`MoveProfile.Walk(2)` = 2 ô, không bay, hazard có tác dụng, không xuyên unit.
`MoveProfile.Fly(3)` = 3 ô, bay (bỏ qua pit/hazard trong path), có thể `IgnoresHazard = true` nếu muốn miễn hazard hoàn toàn.

### 4.2 `UnitDefinition` (POCO, do Content dựng)

| Thuộc tính | Ghi chú |
|-----------|---------|
| `Id` (string) | duy nhất, dùng cho `BrainId`/portrait/asset |
| `DisplayName` | hiển thị |
| `Faction`, `Archetype` | phe + loại |
| `MaxHp`, `Attack`, `Armor`, `Initiative` | chỉ số |
| `SizeX/SizeY` | mặc định 1×1; Malkor 2×2 |
| `Move` | `MoveProfile` |
| `NativeElement` | hệ gốc (ảnh hưởng damage nếu dùng) |
| `SkillIds` | danh sách id skill được dùng |
| `BrainId` | id brain cho AI |
| `PortraitId` | id portrait |
| `IsBoss` | `Archetype` là `StageBoss`/`FinalBoss` |

### 4.3 `UnitState` (runtime)

```csharp
UnitId Id; UnitDefinition Definition; UnitFaction Faction; UnitArchetype Archetype;
bool IsBoss; int MaxHp; int Hp; float HpRatio; bool IsAlive;   // IsAlive = Hp > 0
GridCoord Position; int ActionPoints;
bool HasMovedThisTurn, HasActedThisTurn;
StatusSet Statuses;
int SizeX, SizeY; IReadOnlyList<GridCoord> Footprint; GridCoord Center;
int MovePoints; MoveProfile Move;
bool IsStaggered, IsRooted, IsGuarding, IsFlanked;
bool InCells(IReadOnlyList<GridCoord> cells);
```

- `Position` là ô neo (góc min của footprint).
- `Center = (X + (SizeX-1)/2, Y + (SizeY-1)/2)`; khoảng cách tấn công dùng `Center`.
- Cooldown skill lưu theo `skillId` (string): `GetCooldown`, `SetCooldown`, `TickCooldowns` (giảm 1 mỗi lượt), `CopyCooldowns`/`RestoreCooldowns` cho snapshot.
- **Không** sửa `Hp`/`Position` từ ngoài Core: setter là `internal`, chỉ `UnitRegistry`/`DamageCalculator`/effect gọi.

### 4.4 `IUnitRegistry` / `UnitRegistry`

```csharp
IReadOnlyList<UnitState> All;
UnitState Player;   // có thể null
UnitState Boss;     // có thể null
IReadOnlyList<UnitState> Alive(UnitFaction faction);
UnitState Get(UnitId); bool TryGet(UnitId, out UnitState);
UnitState At(GridCoord);  bool IsOccupied(GridCoord);
bool CanPlace(UnitDefinition, GridCoord anchor);
bool CanPlace(UnitDefinition, GridCoord anchor, UnitId ignore);  // bỏ qua chính mình
UnitState Add(UnitDefinition, GridCoord anchor);                 // → UnitSpawnedEvent
void Remove(UnitId);                                             // despawn, không event
void MoveTo(UnitId, GridCoord anchor);                           // KHÔNG phát event
IReadOnlyList<UnitState> UnitsIn(IReadOnlyList<GridCoord> cells);
IReadOnlyList<UnitState> AdjacentTo(GridCoord coord);            // ô chạm cạnh
void Kill(UnitId);                                               // SetHp(0) + UnitDiedEvent (đúng 1 lần)
```

- `Add`/`MoveTo` ném `InvalidOperationException` nếu `CanPlace` false.
- `Player`/`Boss` có thể `null` (chưa spawn) — luôn kiểm tra.
- `Kill` có cờ `DeathPublished` để không phát `UnitDiedEvent` hai lần (kể cả khi HP đã về 0 trước đó).
- `Remove` dùng khi despawn có chủ đích (không tính là chết).

### 4.5 `OccupancyIndex`

Map `GridCoord → UnitId` cho footprint nhiều ô. `Add(unit)`, `Remove(unit)`, `Move(id, from, to)`, `TryGet`, `IsOccupied`, `At`, `Clear`, `Count`. `UnitRegistry` tự cập nhật; Content/UI hiếm khi cần dùng trực tiếp.

---

## 5. Turn — lượt, pha, AP

```csharp
enum TurnPhase { None, StageIntro, Telegraph, Player, Enemy, TurnEnd, Clash, StageEnded, Duel }

struct ActionPoint { int Current, Max; bool IsEmpty; bool CanSpend(int); ActionPoint Spend(int); }

interface IInitiativePolicy { List<UnitId> Order(IReadOnlyList<UnitState> units); }
class InitiativePolicy : IInitiativePolicy   // Initiative ↓, hoà UnitId ↑, bỏ unit chết
```

```csharp
interface ITurnManager {
  TurnPhase Phase; int TurnIndex, TurnLimit; UnitId ActiveUnit;
  IReadOnlyList<UnitId> EnemyOrder;
  void StartStage(StageDefinition stage);
  void BeginTelegraph(); void BeginPlayerPhase(); void EndPlayerPhase();
  void BeginEnemyPhase(); void EndEnemyPhase(); void EndTurn();
  void BeginClash(); void BeginDuel(); void FinishStage(StageResult result);
  bool CanSpend(UnitId unit, int cost); bool TrySpend(UnitId unit, int cost);
  void ResetActionPoints(); UnitId NextEnemy();
}
class TurnManager : ITurnManager { TurnManager(GameState, IInitiativePolicy = null, IObjectiveEvaluator = null); }
```

### 5.1 Ý nghĩa từng pha

| Pha | Ai mở | Việc xảy ra |
|-----|-------|-------------|
| `StageIntro` | `StartStage` | phát `StageStartedEvent` + `TurnPhaseStartedEvent`, rồi `BeginTelegraph` |
| `Telegraph` | `GameSession.PlanEnemyIntents` | AI công bố `EnemyIntent` (GameSession gọi trước `BeginPlayerPhase`) |
| `Player` | `BeginPlayerPhase` | reset AP = 2; nhận `ActionRequest` |
| `Enemy` | `BeginEnemyPhase` | rebuild `EnemyOrder`, reset AP cho enemy; `GameSession` resolve intent |
| `TurnEnd` | `EndTurn` | tick status → hazard → collapse → `TurnIndex++` → objective |
| `Clash` | `BeginClash` | Clash pha 1 (trên bàn cờ), đẩy checkpoint `ClashStart` |
| `Duel` | `BeginDuel` | Duel pha 2: không AP/lưới/EndTurn |
| `StageEnded` | `FinishStage` | set `Result`, phát `StageEndedEvent` |

### 5.2 Thứ tự `TurnManager.EndTurn()` (chính xác)

```
1. Nếu state.IsOver hoặc Phase == Duel → return
2. Phase = TurnEnd
3. Với mỗi unit còn sống:
      Statuses.TickTurnEnd(ctx)   // poison trừ HP, guard hết hạn; status !CanTick bị bỏ qua
      TickCooldowns()             // -1 mỗi lượt
      nếu còn sống: Statuses.TickTurnStart(ctx)   // bật CanTick cho status vừa gắn
4. Hazard: mỗi ô hazard trong footprint (bỏ qua IgnoresHazard)
      → HazardTriggeredEvent → Damage.Apply(..., ignoresArmor: true)
5. Grid.TickCollapse()            // ô nứt sập
6. TurnIndex++ + TurnAdvancedEvent
7. ObjectiveEvaluator.Evaluate
8. Nếu kết thúc → FinishStage(result)
9. Ngược lại → ResetActionPoints() → PushCheckpoint(TurnStart) → BeginTelegraph()
```

`GameSession.EndTurn()` (dùng cho gameplay) mới là bên chạy thêm pha Enemy và công bố telegraph cho lượt kế:

```
BeginEnemyPhase → RunEnemyBrains (resolve intent đã công bố) → EndEnemyPhase
→ Turn.EndTurn() → PlanEnemyIntents → BeginPlayerPhase
```

> Mẹo: `ActionRequest.EndTurn` qua `TryExecute` **không** chạy AI — dùng trong test hoặc khi UI muốn tách nhịp.

### 5.3 AP & hành động

- 2 AP/lượt; Move tốn 1 AP (và tối đa `MovePoints` ô), Attack/Skill/Guard tốn 1 AP (skill có `ApCost` riêng).
- `TrySpend` phát `ApChangedEvent`; `CanSpend` trả false nếu cost ≤ 0, unit chết hoặc `IsStaggered`.
- `HasMovedThisTurn`/`HasActedThisTurn` reset trong `BeginPlayerPhase` (qua `ResetActionPoints` + reset cờ).

---

## 6. Actions — command

```csharp
enum ActionKind { Move, Attack, Skill, Guard, Wait, EndTurn, Reset, ClashSkill }

enum ActionError {
  None, UnknownActor, UnitDead, NotPlayerPhase, NotEnemyPhase, NoActionPoints,
  OutOfRange, TargetEmpty, NoLineOfSight, CellBlocked, CellOccupied, NotEnoughMovePoints,
  SkillNotReady, SkillOnCooldown, CannotMoveRooted, ActorStunned, StageEnded,
  InvalidTarget, ClashNotAvailable, ObjectiveAlreadyEnded, AlreadyMoved, AlreadyActed, WrongPhase
}

struct ActionRequest {
  UnitId Actor; ActionKind Kind; GridCoord TargetCell; string SkillId; GridCoord MoveTo;
  static Move(actor, destination); static Attack(actor, target);
  static Skill(actor, skillId, target); static Guard(actor);
  static EndTurn(actor); static ClashSkill(actor, skillId);
}

struct ValidationResult { bool IsValid; ActionError Error; string Message; Ok()/Fail(...); }
struct ActionResult     { bool Success; ActionError Error; string Message; Ok()/Fail(...); }
```

### 6.1 `GameActionExecutor.Execute` — các nhánh

| Kind | Điều kiện chính | Tác dụng | Event |
|------|-----------------|----------|-------|
| `Move` | pha Player, !Rooted, !AlreadyMoved, AP ≥ 1, path Found | `TrySpend(1)`, `Units.MoveTo`, `HasMovedThisTurn = true` | `UnitMovedEvent(unit, from, to, cost)` + `UnitActedEvent` |
| `Attack` | pha Player, !AlreadyActed, AP ≥ 1, target địch kề (Manhattan Center ≤ 1) | `DamageEffect(ATK, NativeElement)` → `EffectQueue.Run`, `TrySpend(1)`, `HasActed` | `UnitActedEvent` (+ `DamageDealt/UnitDied`) |
| `Skill` | pha Player, sở hữu skill, cooldown 0, AP ≥ `ApCost`, target hợp lệ | `TrySpend(ApCost)`, `SetCooldown`, `Behaviour.BuildEffects` hoặc damage mặc định | `SkillUsedEvent` + `UnitActedEvent` |
| `Guard` | pha Player, !AlreadyActed, AP ≥ 1 | thêm `GuardStatus` (−80% damage) | `UnitActedEvent` |
| `ClashSkill` | pha Clash (hoặc Duel → nhánh riêng) | Clash pha 1: `state.Clash.Resolve` | `ClashResolvedEvent` |
| `EndTurn` | pha Player | `Turn.EndTurn()` | `TurnAdvancedEvent`… |

Trong **Duel**, `Execute` rẽ nhánh:

- `ActionRequest.ClashSkill` → `ExecuteDuelClash` → `ClashDuel.Choose(skillId)` → resolve + **finisher tự động** → `SkillUsedEvent` + `UnitActedEvent`.
- Không còn route `Attack` trong Duel: `ValidateDuel` chặn `Attack` với `WrongPhase`.

Sau mỗi nhánh (trừ EndTurn), executor gọi `FinishIfOver`: objective ≠ None && phase ≠ StageEnded → `Turn.FinishStage`.

### 6.2 `ActionValidator` — luật kiểm (theo thứ tự)

Chung:

1. `state` null/`IsOver` → `ObjectiveAlreadyEnded`.
2. actor không tồn tại → `UnknownActor`; unit chết → `UnitDead`.
3. **Duel?** → `ValidateDuel` (mục 6.3).
4. `EndTurn` chỉ khi pha Player (ngoài Duel); `ClashSkill` pha 1 chỉ khi pha Clash + có `state.Clash` + skill `IsClashSkill`.
5. Các kind khác: phải pha Player; unit `IsStaggered` → `ActorStunned`.
6. `Move`: `IsRooted` → `CannotMoveRooted`; `HasMovedThisTurn` → `AlreadyMoved`; AP; `InBounds` → `OutOfRange`; ô chặn (trừ bay hợp lệ) → `CellBlocked`; A* path: `OutOfRange` → `NotEnoughMovePoints`, còn lại → `CellBlocked`.
7. `Attack`: `HasActed` → `AlreadyActed`; AP; `At(cell)` null → `TargetEmpty`; cùng faction → `InvalidTarget`; `Manhattan(Center, Center) > 1` → `OutOfRange`.
8. `Skill`: `HasActed`; skill không tồn tại → `SkillNotReady`; không sở hữu → `SkillNotReady`; cooldown > 0 → `SkillOnCooldown`; AP; target:
   - `Targeting.Self` yêu cầu `TargetCell == Position`;
   - ngoài lưới → `OutOfRange`;
   - `RequiresLineOfSight` → không LOS → `NoLineOfSight`;
   - `Targeting.Unit` yêu cầu unit sống tại ô → `TargetEmpty`;
   - khoảng cách `RangeMin..RangeMax` → ngoài → `OutOfRange`.
9. `Guard`: `HasActed`; AP.

`GetValidTargets(request, state)` trả danh sách ô để UI tô sáng:

- Move → `ComputeReachable` bỏ ô hiện tại;
- Attack → 4 ô kề có địch;
- Skill → quét lưới, lọc qua `ValidateSkillTarget` (Self trả ô actor);
- Guard/EndTurn → ô actor;
- **Duel**: ClashSkill → ô actor; Attack/Guard/Move/EndTurn đều bị từ chối.

### 6.3 `ValidateDuel` (Duel mode)

| Trường hợp | Kết quả |
|-----------|---------|
| `state.Duel == null` hoặc `!IsActive` | `ClashNotAvailable` |
| Duel đã kết thúc | `StageEnded` |
| `ClashSkill` không phải `IsClashSkill` | `InvalidTarget` |
| Skill không nằm trong `DuelDefinition.PlayerSkillIds` | `SkillNotReady` |
| Actor không sở hữu skill | `SkillNotReady` |
| Mọi kind khác (Attack/Move/Guard/EndTurn…) | `WrongPhase` |

---

## 7. Combat — sát thương, skill, hiệu ứng, trạng thái

### 7.1 Hệ & ma trận

```csharp
struct ElementId { string Value; static None, Physical, Fire, Holy, Necrotic, Piercing, Temporal;
                   ==, !=, IsEmpty; (ElementId)"custom" }

class ElementMatrix {
  static ElementMatrix CreateDefault();
  void Set(ElementId attack, ElementId defend, float multiplier);
  float GetMultiplier(ElementId attack, ElementId defend);
  ElementMatrix Clone();
}
```

`ElementMatrix` mặc định:

| Attack \ Defend | Necrotic | Fire | Khác |
|-----------------|----------|------|------|
| Holy | 1.5 | 1.25 | 1.0 |
| Fire | 1.25 | 1.0 | 1.0 |
| Necrotic | 1.0 | 1.0 | 0.75 vs Holy |
| Temporal | luôn 1.0 | | |

### 7.2 Damage

```csharp
struct DamageRequest {
  UnitId Source, Target; int BasePower; ElementId Element;
  bool IsSkill, IgnoresArmor; GridCoord HitFrom;
  ctor(source, target, basePower, element, isSkill = false, ignoresArmor = false, hitFrom = default)
}

struct DamageResult {
  int FinalDamage; float Multiplier;
  bool WasStaggered, WasGuarded, WasFlanked, Killed; bool IsZero;
}

interface IDamageCalculator {
  DamageResult Compute(DamageRequest request, GameState state);
  DamageResult Apply(DamageRequest request, GameState state);
}
class DamageCalculator : IDamageCalculator { DamageCalculator(ElementMatrix matrix = null); }

static class CombatMath {
  int ComputePower(int basePower, int armor, float elementMultiplier, bool ignoresArmor, float statusMultiplier);
  float CombineMultipliers(float element, float status);
}
```

Công thức:

```
power = basePower × elementMultiplier            // hệ của đòn × hệ mục tiêu
nếu !ignoresArmor: power -= armor                 // giáp trừ SAU hệ
power = max(1, power)
power = power × statusMultiplier                  // Stagger 2, Flanked 1.5, Guard 0.2 (nhân dồn)
final = max(1, round(power))
```

`Apply`:
1. `Compute` → `FinalDamage`.
2. `target.SetHp(Hp - damage)` (clamp ≥ 0).
3. `target.Statuses.NotifyDamaged(ctx, result)` (Flanked tự xoá).
4. phát `DamageDealtEvent(source, target, damage, element, wasStaggered, wasGuarded, wasFlanked, killed, at)`.
5. nếu HP = 0 → `Units.Kill(target)` → `UnitDiedEvent` (đúng 1 lần).

### 7.3 Skill

```csharp
enum SkillTargeting { Self, Unit, Cell }

class SkillDefinition {
  string Id, DisplayName; SkillTargeting Targeting;
  int RangeMin = 1, RangeMax = 1, Power, ApCost = 1, CooldownTurns, AreaRadius;
  ElementId Element; bool RequiresLineOfSight, IsClashSkill;
  ISkillBehaviour Behaviour; MoveProfile MoveAfterUse;
}

interface ISkillBehaviour { void BuildEffects(EffectContext ctx); }

interface ISkillLibrary { IReadOnlyList<SkillDefinition> All; bool TryGet(id, out skill); SkillDefinition Get(id); }
class SkillLibrary : ISkillLibrary { Add(skill); ... }   // Get ném KeyNotFoundException
```

Cách viết skill mới (Content):

```csharp
public sealed class HolyPillarBehaviour : ISkillBehaviour {
    public void BuildEffects(EffectContext ctx) {
        // Cross 3 ô quanh ô đích: 6 damage Holy, +50% vs quỷ/hắc ám do ElementMatrix lo
        foreach (var cell in GridShapes.Cross(ctx.TargetCell, 1)) {
            if (!ctx.State.Grid.InBounds(cell)) continue;
            ctx.Queue.Enqueue(new SpawnTerrainEffect(cell, TerrainId.Poison, TerrainFlags.IsHazard, 1, 2, 0));
        }
        ctx.Queue.Enqueue(new DamageEffect(ctx.Target, 6, ElementId.Holy, isSkill: true));
    }
}
```

- `DamageEffect(UnitId.None, power, element, true)` **không phải AoE** — nó rơi về `ctx.Target`. Muốn AoE, lặp `ctx.Units.UnitsIn(cells)` và enqueue từng `DamageEffect` cho từng unit.
- Effect chạy theo `Priority` tăng dần: damage 10 → heal 12 → status 20/21 → push 30 → teleport 35 → terrain 40/41 → lock 45 → shake 90.

### 7.4 Effect

| Effect | Priority | Hành vi |
|--------|----------|---------|
| `DamageEffect(target, power, element, isSkill, ignoresArmor)` | 10 | `Damage.Apply`; `power ≤ 0` → dùng `ctx.Power` |
| `HealEffect(target, amount)` | 12 | hồi HP, clamp `MaxHp` (không hồi unit chết) |
| `ApplyStatusEffect(target, effect)` | 20 | `Statuses.Add(effect, ctx)` (refresh cùng Id) |
| `RemoveStatusEffect(target, statusId)` | 21 | `Statuses.Remove` |
| `PushEffect(target, direction, distance)` | 30 | đẩy từng ô, dừng khi bị chặn; phát `UnitMovedEvent(cost 0)` |
| `TeleportEffect(target, destination)` | 35 | `CanPlace(..., ignore: self)` rồi `MoveTo` |
| `SpawnTerrainEffect(cell, terrain, flags, movementCost, hazard, collapseInTurns)` | 40 | `SetTerrain` (+ `ArmCollapse` nếu > 0) |
| `ArmCollapseEffect(cell, turns)` | 41 | `ArmCollapse` |
| `LockArenaEffect(cells)` | 45 | `Grid.LockArea` |
| `CameraShakeEffect(strength, duration)` | 90 | phát `CameraShakeHintEvent` |

`EffectQueue.Run(ctx)`:

- sắp xếp **stable** theo `Priority`;
- nếu `ctx.Source` chết giữa chừng → **dừng**;
- phát event ngay sau mỗi effect (trong effect), không gom cuối;
- `Clear()` khi bắt đầu một đợt mới.

`EffectContext`:

```csharp
GameState State; EffectQueue Queue; IUnitRegistry Units; IEventBus Events; IRandomSource Random;
UnitId Source; GridCoord Origin; UnitId Target; GridCoord TargetCell;
SkillDefinition Skill; int Power; ElementId Element;
UnitState TargetUnit; GridCoord ResolvedCell;
UnitState UnitAt(GridCoord cell);
```

### 7.5 Status

```csharp
struct StatusId { string Value; static None; ==, !=, IsEmpty }
static class StatusIds { Stagger, Flanked, Root, Poison, Guard }

interface IStatusEffect {
  StatusId Id; int Duration; float DamageTakenMultiplier;
  bool PreventsAction, PreventsMovement;
  void OnApply(StatusContext ctx); void OnTurnStart(StatusContext ctx);
  void OnTurnEnd(StatusContext ctx); void OnRemoved(StatusContext ctx);
  void OnDamaged(StatusContext ctx, DamageResult result);
}
class StatusContext { UnitState Owner; IEventBus Events; IUnitRegistry Units; }

class StatusSet {
  bool Has(StatusId); bool Has<T>(); IStatusEffect Get(StatusId); int GetRemaining(StatusId);
  void Add(IStatusEffect effect, StatusContext ctx); bool Remove(StatusId); void Clear();
  void TickTurnStart(StatusContext); void TickTurnEnd(StatusContext);
  void NotifyDamaged(StatusContext, DamageResult);
  int Count; IEnumerable<IStatusEffect> Effects;
  float DamageTakenMultiplier; bool PreventsAction, PreventsMovement;
  bool IsStaggered, IsRooted, IsGuarding, IsFlanked;
}
```

| Status | Duration | Hệ số | Tác dụng |
|--------|----------|-------|----------|
| `StaggerStatus` | 1 | ×2 damage nhận | chặn action + move |
| `FlankedStatus` | 2 | ×1.5 | `OnDamaged` tự xoá sau đòn đầu |
| `RootStatus` | 1 | 1 | chặn move |
| `PoisonStatus(damagePerTurn, duration)` | N | 1 | `OnTurnEnd` trừ HP trực tiếp, phát `DamageDealtEvent` Necrotic; chết → `Units.Kill` |
| `GuardStatus` | 1 | ×0.2 | giảm 80% damage |

**Đếm duration:** status mới gắn có `CanTick = false`; `TickTurnStart` bật `CanTick = true`; `TickTurnEnd` chỉ giảm/`OnTurnEnd` khi `CanTick`. Nhờ vậy status gắn giữa lượt (Stagger sau Clash/Duel) không hết ngay cuối lượt đó mà sống sang lượt kế.

---

## 8. Clash pha 1 — trên bàn cờ

```csharp
struct ClashRule { string PlayerSkillId, BossSkillId; int Bonus; }
class ClashRuleTable {
  int DefaultBonus = 3;
  void Add(ClashRule rule);
  void AddElement(ElementId playerElement, ElementId bossElement, int bonus);
  int GetBonus(string playerSkillId, string bossSkillId, ElementId playerElement, ElementId bossElement);
}
class ClashDefinition {
  string BossSkillId; int BossPower; ElementId BossElement;
  ClashRuleTable Rules; Func<IStatusEffect> WinEffect, LoseEffect;
  IStatusEffect CreateWinEffect(); IStatusEffect CreateLoseEffect();
}
enum ClashResult { None, Win, Draw, Lose }
struct ClashPrediction { ClashResult Result; int PlayerPower, BossPower, Bonus; }
class ClashOutcome { ClashResult Result; int PlayerPower, BossPower; IStatusEffect AppliedEffect; bool AppliedToBoss; }
class ClashOption { string SkillId, DisplayName; int Power; ClashPrediction Prediction; }

interface IClashResolver {
  ClashPrediction Predict(UnitState player, UnitState boss, SkillDefinition playerSkill, ClashDefinition clash);
  ClashOutcome Resolve(UnitState player, UnitState boss, SkillDefinition playerSkill, ClashDefinition clash);
  IReadOnlyList<ClashOption> GetOptions(UnitState player, UnitState boss,
      IReadOnlyList<string> skillIds, ISkillLibrary skills, ClashDefinition clash);
}
class ClashResolver : IClashResolver { int EffectivePower(SkillDefinition, int bonus); }

static class ClashMath {
  int EffectivePower(SkillDefinition skill, int bonus);   // skill.Power + bonus
  ClashResult Compare(int playerPower, int bossPower);    // > Win, == Draw, < Lose
}
```

- Bonus lấy theo thứ tự: **cặp (playerSkillId, bossSkillId)** → **cặp element** → `DefaultBonus = 3`.
- `Predict` và `Resolve` dùng chung `EffectivePower`; không RNG → dự báo luôn khớp.
- `Resolve`: Win → `WinEffect` lên boss (`AppliedToBoss = true`); Lose → `LoseEffect` lên player; Draw → không gắn.
- `GetOptions` chỉ lấy skill `IsClashSkill` trong danh sách truyền vào.
- Luồng pha 1: chỉ khi `TurnPhase.Clash` (do `TurnManager.BeginClash` hoặc test gọi) mới nhận `ActionRequest.ClashSkill`.

---

## 9. Duel pha 2 ⭐

```csharp
class DuelDefinition {
  string Id, BackgroundId, MusicId, TransitionId;
  List<string> BossSkillIds = new();     // chu kỳ chiêu boss
  List<string> PlayerSkillIds = new();   // chiêu Clash người chơi được chọn
  int RoundLimit;                        // 0 = không giới hạn
  float DrawChipRatio = 0.10f;           // mỗi bên mất 10% power khi Draw
  int FinisherCount = 1;                 // số đòn kết liễu tự động sau Win
  ClashRuleTable Rules = new();
  Func<IStatusEffect> WinEffect = () => new StaggerStatus();
  Func<IStatusEffect> LoseEffect;        // null = không gắn
  IStatusEffect CreateWinEffect(); IStatusEffect CreateLoseEffect();
}

class DuelState {
  string DuelId; int Round, BossSkillIndex; string BossSkillId;
  int BossPower; ElementId BossElement;
  ClashResult LastResult; bool IsOver; UnitId Winner;
  IReadOnlyList<string> SeenBossSkills;
}

struct ClashRoundResult {
  ClashResult Result; int PlayerPower, BossPower, DamageToPlayer, DamageToBoss;
  IStatusEffect Applied;
}

interface IClashDuel {
  DuelState State; DuelDefinition Definition; bool IsActive;
  void Start(GameState state, DuelDefinition definition, UnitState player, UnitState boss);
  IReadOnlyList<ClashOption> GetOptions();
  ClashRoundResult Choose(string playerSkillId);   // resolve + tự finisher + tự sang vòng
  DuelSnapshot Capture(); void Restore(DuelSnapshot snapshot); void Clear();
}
class ClashDuel : IClashDuel { ClashDuel(GameState state); }
```

### 9.1 Cách Duel được kích hoạt

`GameSession`:

- `TryExecute` thành công → `TryEnterDuel()`: yêu cầu `Stage.Duel != null`, boss sống, `Phase != Duel`, `boss.HpRatio < Rules.Phase2Threshold` (mặc định 0.70).
- Có thể gọi thẳng `GameSession.EnterDuel()` (test/debug).

`EnterDuel()`:

1. `Telegraph.Clear()`.
2. `BossPhase.EnterPhase2(State)` (đánh dấu Phase 2 + `BossPhaseChangedEvent`).
3. `Duel.Start(state, stage.Duel, player, boss)`:
   - clear status 2 bên;
   - phát `ClashStartedEvent(boss, bossSkillId đầu)`;
   - `EnterRound(1)`.
4. `Turn.BeginDuel()` → `TurnPhase.Duel`.

### 9.2 Một vòng

```
EnterRound(n):
  Round = n
  BossSkillIndex = (n-1) % BossSkillIds.Count
  BossSkillId, BossPower, BossElement lấy từ SkillLibrary
  SeenBossSkills.Add(id); Knowledge.MarkSeen("boss-skill:" + id)
  phát DuelRoundStartedEvent(round, bossSkillId, bossPower)

Choose(playerSkillId):
  power = ClashMath.EffectivePower(skill, bonus từ Rules)
  so với BossPower:
    Win  → boss.Statuses.Add(WinEffect)
           → tự đánh max(1, FinisherCount) đòn: Damage.Apply(player → boss, ATK, NativeElement,
             isSkill: false, ignoresArmor: false)   // giáp vẫn áp dụng, Stagger ×2
    Draw → chip = round(power × DrawChipRatio) ; Damage.Apply cả 2 bên (ignoresArmor, element None)
    Lose → Damage.Apply(player, BossPower) ; nếu LoseEffect != null → gắn cho player
  phát ClashResolvedEvent
  CheckDeaths (player chết ưu tiên trước) → nếu chết: End(...)
  nếu chưa kết thúc → EnterRound(n+1)     // tự end turn, không có cửa sổ finisher thủ công
```

`Attack` trong Duel bị `ValidateDuel` chặn (`WrongPhase`) — không có API `TryFinisher` nữa.

### 9.3 Kết thúc

- Boss chết → `Duel.End(ClashResult.Win, winner: player)` → `DuelEndedEvent` → `ObjectiveEvaluator` → `Victory` → `FinishStage` → `StageEndedEvent`.
- Player chết → `DefeatPlayerDead`.
- `RoundLimit > 0` và vượt → `End(None, Draw)`; UI tự quyết định ý nghĩa hoà (hiện chưa dùng mặc định).

### 9.4 Snapshot

`DuelSnapshot` lưu: `DuelId, Round, BossSkillIndex, BossSkillId, BossPower, BossElement, LastResult, IsOver, Winner, SeenBossSkills`.
`GameSnapshot.Duel` có thể `null` (chưa vào Duel); `SnapshotService.Capture/Restore` truyền và khôi phục nó. `GameSnapshot.ContentEquals` so cả Duel.

---

## 10. AI & Telegraph

```csharp
class EnemyIntent {
  UnitId Unit; string SkillId; GridCoord TargetCell;
  IReadOnlyList<GridCoord> DangerCells; IReadOnlyList<IGameEffect> Effects; int Priority;
  bool IsEmpty; static Empty(UnitId);
}

interface IEnemyBrain { string Id; EnemyIntent Plan(UnitState self, GameState state); }

interface ITelegraphService {
  IReadOnlyCollection<EnemyIntent> All;
  IReadOnlyList<GridCoord> DangerCells;          // hợp nhất, sort Y rồi X
  bool TryGet(UnitId, out EnemyIntent);
  void Declare(EnemyIntent); void Clear();
}

interface IEnemyBrainLibrary { IReadOnlyList<IEnemyBrain> All; void Register(IEnemyBrain); bool TryGet(id, out brain); IEnemyBrain Get(id); }

interface IBossPhaseController {
  int Phase; bool ShouldEnterPhase2(int hp, int maxHp); void EnterPhase2(GameState state);
}
class BossPhaseController : IBossPhaseController {
  BossPhaseController(IGameRules rules, Func<GameState, IReadOnlyList<GridCoord>> arenaCells = null);
}
```

- `Plan` phải **thuần + tất định**: cùng state → cùng intent (đã test 4 case).
- `EnemyIntent` là **bất biến sau khi công bố**; pha Enemy chỉ resolve đúng intent.
- `GameSession.PlanEnemyIntents()` duyệt `Units.Alive(Enemy)`, lấy brain theo `Definition.BrainId`, gọi `Plan` và `Declare`. Brain không tìm thấy → bỏ qua (an toàn).
- `BossPhaseController.EnterPhase2` set `Phase = 2`, khoá `arenaCells` nếu có, phát `BossPhaseChangedEvent(bossId, 2)`; hiện Duel dùng nó như bước đánh dấu.

---

## 11. Run — state, session, snapshot, story

### 11.1 Rules & RNG

```csharp
interface IGameRules {
  int ApPerTurn, MovePoints, TurnLimit;
  float Phase2Threshold, GuardMultiplier, StaggerMultiplier, FlankMultiplier;
  int PoisonPerTurn;
}
class GameRules : IGameRules   // mặc định 2, 2, 25, 0.7, 0.2, 2, 1.5, 3

interface IRandomSource { uint State { get; set; } int NextInt(min, max); float NextFloat(); }
class XorShiftRandom : IRandomSource { XorShiftRandom(int seed); uint NextUInt(); }
```

- Seed 0 được thay bằng `0x9E3779B9` để tránh trạng thái xấu.
- Core **không** dùng `UnityEngine.Random`; mọi random gameplay đi qua `IRandomSource` và được snapshot.

### 11.2 Stage

```csharp
class StageDefinition {
  string Id, DisplayName;
  int Width = 8, Height = 8, TurnLimit = 25;
  UnitDefinition Player; GridCoord PlayerSpawn;
  List<UnitSpawnDefinition> Spawns = new();
  List<TerrainOverride> Terrains = new();
  string BossId;
  ClashDefinition Clash;   // pha 1
  DuelDefinition Duel;     // pha 2
}
class UnitSpawnDefinition { UnitDefinition Definition; GridCoord Anchor; }
struct TerrainOverride { GridCoord Coord; TerrainId Terrain; TerrainFlags Flags;
                         int MovementCost, HazardDamagePerTurn, CollapseInTurns; }
```

`GameSession.Build` áp `Terrains` bằng `SetTerrain`, và `ArmCollapse` nếu `CollapseInTurns > 0`; spawn `Player` + từng `Spawns`.

### 11.3 `GameState`

```csharp
GameState(IGridMap grid, IUnitRegistry units, IGameRules rules, IEventBus events, IRandomSource random)

IGridMap Grid; IUnitRegistry Units; IPathfinder Pathfinder; ITurnManager Turn;
IGameRules Rules; IEventBus Events; IRandomSource Random; ISkillLibrary Skills;
IDamageCalculator Damage; ITelegraphService Telegraph; IClashResolver Clash;
IClashDuel Duel; IBossPhaseController BossPhase; ISnapshotService Snapshots;
IRunKnowledge Knowledge; StageDefinition Stage; StageResult Result;
bool IsOver => Result != None;
```

Đây là "service locator" được truyền tường minh; Core dùng nó để tránh singleton. Validator/effect/brain nhận `GameState` qua tham số.

### 11.4 `GameSession` — façade duy nhất cho Unity

```csharp
class GameSession {
  GameSession(IGameRules rules = null, ISkillLibrary skills = null,
              IEnemyBrainLibrary brains = null, IClashResolver clash = null,
              IRandomSource random = null, ElementMatrix elements = null,
              IObjectiveEvaluator objective = null, IInitiativePolicy initiative = null,
              int snapshotCapacity = 8, int seed = 20260101);

  GameState State; IEventBus Events; ISkillLibrary Skills;
  IEnemyBrainLibrary Brains; IRunKnowledge Knowledge;

  void RegisterStage(StageDefinition stage);
  void StartStage(string stageId); void StartStage(StageDefinition stage);
  ActionResult TryExecute(ActionRequest request);
  ActionResult EndTurn();
  void BeginPlayerPhase();
  void EnterDuel();
  void Restart();   // chơi lại state.Stage, reset RNG về seed, RunKnowledge mới
  void Reset();     // alias Restart
}
```

`Build(stage)` thứ tự:

```
EventBus → GridMap(width, height, events) → UnitRegistry → GameState
→ Stage/Result/Skills/Pathfinder/Damage/Telegraph/Clash/Duel/BossPhase/Knowledge
→ TurnManager → SnapshotService(store capacity)
→ ApplyTerrains → SpawnUnits
→ ActionValidator(pathfinder) + GameActionExecutor(objective)
→ Turn.StartStage(stage) → PushCheckpoint(StageStart)
→ PlanEnemyIntents → BeginPlayerPhase
```

`EndTurn()` (gameplay):

```
nếu IsOver hoặc Phase != Player → ActionResult.Fail(WrongPhase)
BeginEnemyPhase → RunEnemyBrains → (nếu state over → return)
→ EndEnemyPhase → Turn.EndTurn() → PlanEnemyIntents → BeginPlayerPhase
```

`TryExecute(request)`:

```
if State/Turn null → Fail(WrongPhase)
result = _executor.Execute(request, State)
if result.Success → TryEnterDuel()
return result
```

### 11.5 Snapshot

```csharp
class GameSnapshot {
  int TurnIndex; TurnPhase Phase; StageResult Result; uint RandomState;
  CellState[] Cells; IReadOnlyList<GridCoord> LockedCells; UnitSnapshot[] Units;
  IReadOnlyList<EnemyIntent> Intents; DuelSnapshot Duel;
  bool ContentEquals(GameSnapshot other);
}
class UnitSnapshot { UnitId Id; UnitDefinition Definition; GridCoord Position; int Hp, ActionPoints;
                     bool HasMovedThisTurn, HasActedThisTurn;
                     IReadOnlyList<StatusSnapshot> Statuses; IReadOnlyDictionary<string,int> Cooldowns; }
struct StatusSnapshot { IStatusEffect Effect; int Remaining; bool CanTick; }
class DuelSnapshot { ...; bool ContentEquals(DuelSnapshot other); }

interface ISnapshotStore { int Capacity, Count; IReadOnlyList<GameSnapshot> All;
                           void Push(GameSnapshot); bool TryGetLatest(out GameSnapshot); void Clear(); }
class SnapshotStore : ISnapshotStore { SnapshotStore(int capacity = 8); }  // ring buffer

enum SnapshotTrigger { StageStart, TurnStart, ClashStart }

interface ISnapshotService {
  bool CanRewind; GameSnapshot Capture(); void Restore(GameSnapshot);
  void PushCheckpoint(SnapshotTrigger); void Rewind(); void ClearCheckpoints();
}
class SnapshotService : ISnapshotService { SnapshotService(GameState state, ISnapshotStore store = null); }
```

`Restore` thứ tự chính xác:

1. `Random.State = snapshot.RandomState`
2. `GridMap.RestoreCells(Cells, LockedCells)`
3. `UnitRegistry.ClearSilently()` + `RestoreAdd(def, pos, id, hp, ap)` từng unit
4. `SetHp`, `SetPosition`, `ActionPoints`, `HasMoved/Acted`, `RestoreCooldowns`, `Statuses.Restore`
5. `Telegraph.RestoreSilently(Intents)`
6. `Duel.Restore(Duel)` hoặc `Duel.Clear()` nếu null
7. `Result = snapshot.Result`
8. `TurnManager.RestoreState(TurnIndex, Phase)`

`PushCheckpoint(trigger)` lưu snapshot + `SnapshotPushedEvent`; `Rewind()` lấy checkpoint gần nhất, `Restore`, phát `RewindPerformedEvent(trigger, TurnIndex)`.

### 11.6 `RunKnowledge`

```csharp
interface IRunKnowledge {
  int Deaths; IReadOnlyDictionary<string,int> Counters; IReadOnlyCollection<string> SeenKeys;
  bool HasSeen(string key); void MarkSeen(string key);
  int GetCounter(string key); void Increment(string key, int amount = 1);
  void RegisterDeath();
}
class RunKnowledge : IRunKnowledge {
  RunKnowledge(IEventBus events = null);
  const string DeathsKey = "deaths"; const string BossSkillSeenPrefix = "boss-skill:";
}
```

Nằm **ngoài** snapshot → rewind không xoá. Dùng cho codex/UI/thoại ("đã thấy chiêu boss", "số lần chết").

### 11.7 Dialogue

```csharp
class DialogueNode {
  string Id, Speaker, Text, NextId; IReadOnlyList<DialogueChoice> Choices; StageResult Ending;
  DialogueNode AddChoice(string text, string nextId,
                         Func<IRunKnowledge,bool> condition = null, bool hideWhenFalse = false);
}
class DialogueChoice { string Text, NextId; Func<IRunKnowledge,bool> Condition;
                       bool HideWhenFalse; bool IsAvailable(IRunKnowledge); }
class DialogueGraph { string StartId; IReadOnlyCollection<DialogueNode> Nodes;
                      DialogueGraph(string startId); Add(node); TryGet; Get; List<string> Validate(); }
class DialogueRunner { DialogueNode Current; bool IsFinished; StageResult Ending;
                       DialogueRunner(DialogueGraph graph, IRunKnowledge knowledge);
                       void Start(); void Advance(); void Choose(int index); }
```

- `Advance()` ném `InvalidOperationException` nếu node hiện tại có choice.
- `Choose(index)` theo thứ tự lựa chọn **hiển thị** (đã lọc điều kiện/ẩn).
- `Ending` đọc khi node có `Ending != None` trước khi chuyển node.

### 11.8 Event bus

```csharp
delegate void RefEventHandler<T>(in T evt) where T : struct;

interface IEventBus {
  IDisposable Subscribe<T>(RefEventHandler<T> handler);
  void Unsubscribe<T>(RefEventHandler<T> handler);
  void Publish<T>(in T evt);
  void Clear();
}
class EventBus : IEventBus
```

- Không boxing: dùng `in T` + `Dictionary<Type, object>` nội bộ.
- Publish duyệt **bản sao** danh sách subscriber → subscribe/unsubscribe trong handler an toàn.
- Subscribe trả `IDisposable` (dispose = unsubscribe).

---

## 12. Danh sách 28 event

| # | Event | Trường | Ai phát |
|---|-------|--------|---------|
| 1 | `StageStartedEvent` | StageId, TurnLimit | `TurnManager.StartStage` |
| 2 | `TurnPhaseStartedEvent` | Phase, TurnIndex | `TurnManager.SetPhase` |
| 3 | `TurnAdvancedEvent` | TurnIndex | `TurnManager.EndTurn` |
| 4 | `UnitSpawnedEvent` | Unit, Position | `UnitRegistry.Add` |
| 5 | `UnitMovedEvent` | Unit, From, To, Cost | executor Move, `PushEffect` (cost 0) |
| 6 | `UnitActedEvent` | Unit, Kind, Target | executor sau mỗi action |
| 7 | `ApChangedEvent` | Unit, ActionPoints | `TurnManager.TrySpend` |
| 8 | `DamageDealtEvent` | Source, Target, Amount, Element, WasStaggered, WasGuarded, WasFlanked, Killed, At | `DamageCalculator.Apply`, `PoisonStatus` |
| 9 | `UnitDiedEvent` | Unit, At | `UnitRegistry.Kill` (đúng 1 lần) |
| 10 | `StatusAppliedEvent` | Unit, Status, Duration | `StatusSet.Add` |
| 11 | `StatusExpiredEvent` | Unit, Status | `StatusSet.TickTurnEnd` |
| 12 | `SkillUsedEvent` | Unit, SkillId, Target | executor Skill / Duel clash |
| 13 | `IntentDeclaredEvent` | Intent | `TelegraphService.Declare` |
| 14 | `TelegraphClearedEvent` | Count | `TelegraphService.Clear` |
| 15 | `CellChangedEvent` | Coord, Terrain, Flags, HazardDamagePerTurn | `GridMap.SetTerrain` |
| 16 | `CellCollapsedEvent` | Coord | `GridMap.TickCollapse` |
| 17 | `HazardTriggeredEvent` | Coord, Unit, Damage | `TurnManager.TickTerrain` |
| 18 | `ArenaLockedEvent` | CellCount | `GridMap.LockArea` |
| 19 | `BossPhaseChangedEvent` | Boss, Phase | `BossPhaseController.EnterPhase2` |
| 20 | `ClashStartedEvent` | Boss, BossSkillId | `ClashDuel.Start` |
| 21 | `ClashResolvedEvent` | Boss, PlayerSkillId, Result, PlayerPower, BossPower | `ClashResolver.Resolve`, `ClashDuel.Choose` |
| 22 | `StageEndedEvent` | Result, TurnIndex | `TurnManager.FinishStage` |
| 23 | `RewindPerformedEvent` | Trigger, TurnIndex | `SnapshotService.Rewind` |
| 24 | `SnapshotPushedEvent` | Trigger, TurnIndex | `SnapshotService.PushCheckpoint` |
| 25 | `RunKnowledgeUpdatedEvent` | Key, Value | `RunKnowledge` |
| 26 | `CameraShakeHintEvent` | Strength, Duration | `CameraShakeEffect` |
| 27 | `DuelRoundStartedEvent` | Round, BossSkillId, BossPower | `ClashDuel.EnterRound` |
| 28 | `DuelEndedEvent` | LastResult, Winner | `ClashDuel.End` |

**Quy ước UI:**

```csharp
IDisposable sub = session.Events.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) => {
    hud.SpawnDamageNumber(e.Target, e.Amount, e.Killed);
});
// ... khi destroy: sub.Dispose();
```

Trong Unity, nhớ `Dispose` khi `OnDestroy`/đổi scene để tránh handler treo.

---

## 13. Ba luồng mẫu

### 13.1 Đánh thường trên bàn cờ

```csharp
var session = new GameSession();
session.RegisterStage(new StageDefinition {
    Id = "stage-1", Width = 8, Height = 8,
    Player = wanderer, PlayerSpawn = new GridCoord(0, 0),
    Spawns = { new UnitSpawnDefinition(boss, new GridCoord(1, 0)) },
    TurnLimit = 25
});
session.StartStage("stage-1");

var result = session.TryExecute(ActionRequest.Attack(session.State.Units.Player.Id, new GridCoord(1, 0)));
// result.Success, boss Hp giảm, các event DamageDealt/UnitActed phát ra
```

### 13.2 Một vòng Duel

```csharp
// boss đang dưới 70% HP; TryExecute thành công sẽ tự vào Duel
session.TryExecute(ActionRequest.Skill(heroId, "swift-slash", bossCell));

if (session.State.Turn.Phase == TurnPhase.Duel) {
    var options = session.State.Duel.GetOptions();     // 3 ClashOption + prediction
    var option = options.First(o => o.Prediction.Result == ClashResult.Win);

    var round = session.TryExecute(ActionRequest.ClashSkill(heroId, option.SkillId));
    // Win → boss Stagger + finisher tự động, đã sang vòng kế; KHÔNG gửi Attack nữa
}
```

### 13.3 Snapshot & rewind

```csharp
session.State.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart); // thường TurnManager tự đẩy
// ... đánh nhau, chết
if (session.State.Snapshots.CanRewind) {
    session.State.Snapshots.Rewind();   // phát RewindPerformedEvent, khôi phục Duel/telegraph/RNG
}
```

---

## 14. Hằng số & công thức

| Hạng mục | Giá trị | Nơi |
|----------|---------|-----|
| AP/lượt | 2 (1 Move + 1 Action) | `GameRules.ApPerTurn` |
| Move | 2 ô, 4 hướng, không chéo | `MoveProfile` + `AStarPathfinder` |
| Melee | Manhattan ≤ 1 (theo `Center`) | `ActionValidator` |
| Ranged | 2–3, cần LOS | `SkillDefinition.RangeMin/Max` + `RequiresLineOfSight` |
| Turn limit | 25 mặc định (20–25/thiết kế) | `GameRules.TurnLimit` / `StageDefinition.TurnLimit` |
| Phase 2 | `HpRatio < 0.70` | `GameRules.Phase2Threshold` |
| Stagger | 1 lượt, ×2 | `StaggerStatus` + `GameRules.StaggerMultiplier` |
| Guard | ×0.2 | `GuardStatus` + `GameRules.GuardMultiplier` |
| Flanked | ×1.5, tự xoá sau đòn | `FlankedStatus` + `GameRules.FlankMultiplier` |
| Poison | 3 HP/lượt | `PoisonStatus` + `GameRules.PoisonPerTurn` |
| Damage | `max(1, round(max(1, base×hệ−giáp) × status))` | `CombatMath` |
| Clash bonus | cặp chiêu → cặp hệ → 3 | `ClashRuleTable` |
| Duel Draw | 10% power mỗi bên | `DuelDefinition.DrawChipRatio` |
| Duel finisher | tự động `FinisherCount` đòn ATK × status | `DuelDefinition.FinisherCount` + `ClashDuel.Choose` |
| Timeout snapshot | ring 8 | `SnapshotStore` |

---

## 15. Cách mở rộng (không sửa Core)

### 15.1 Skill mới

1. `MyBehaviour : ISkillBehaviour` (Content/Skills).
2. `BuildEffects(ctx)` — enqueue effect; damage dùng `DamageEffect`.
3. `SkillDataAsset` → khi load tạo `SkillDefinition` (id, targeting, range, power, element, AP, cooldown, `IsClashSkill`, behaviour).
4. Thêm id vào `UnitDefinition.SkillIds`.
5. Nếu dùng cho Duel: thêm id vào `DuelDefinition.PlayerSkillIds` (và `IsClashSkill = true`).

### 15.2 Quái/ brain mới

1. `MyBrain : IEnemyBrain` — `Plan` thuần, trả `EnemyIntent` (dùng `GridShapes` để tính danger).
2. Đăng ký vào `EnemyBrainLibrary` với `Id`.
3. `UnitDefinition.BrainId = "my-brain"`.
4. Thêm vào `StageDefinition.Spawns`.

### 15.3 Boss / màn mới

1. Brain boss + (tuỳ chọn) `IBossPhaseController` riêng.
2. `StageDefinition` mới (kích thước, spawn, terrains, boss id, `Clash`, `Duel`).
3. `ClashRuleTable` cho các cặp chiêu; `DuelDefinition` cho pha 2.
4. Dialogue nếu có.

### 15.4 Địa hình mới

1. `MyTerrain : ITerrainBehaviour` (Content/Terrain) — quyết định `BlocksMove`, `OnEnter`, `OnTurnStart`, `OnDamaged`.
2. Đăng ký `TerrainId → behaviour`.
3. `SetTerrain`/`TerrainOverride` dùng `(TerrainId)"my-terrain"`.

---

## 16. Mười một lỗi thường gặp (đã gặp thật khi làm Core)

| # | Lỗi | Cách tránh |
|---|-----|-----------|
| 1 | Gọi `Kill` sau khi đã `SetHp(0)` → mất `UnitDiedEvent` | `UnitRegistry.Kill` có `DeathPublished`; không tự set Hp về 0 trước khi gọi Kill |
| 2 | Gắn status giữa lượt rồi bị tick ngay cuối lượt | Thiết kế `CanTick` đã xử lý; khi thêm status mới, dùng `StatusSet.Add` chứ không tự thêm entry |
| 3 | Quên `includePlatforms: ["Editor"]` trong test asmdef → Unity chạy 0 test | Đặt đúng; chạy lại batchmode kiểm `results.xml` total > 0 |
| 4 | `Does.Contain(object)` khác nhau giữa NUnit Unity và NUnit NuGet | Dùng `Enumerable.Contains(path, cell)` hoặc so sánh trực tiếp |
| 5 | Telegraph công bố lúc pha Enemy → người chơi không kịp né | `PlanEnemyIntents` trước `BeginPlayerPhase`, resolve intent đã Declare |
| 6 | `DamageEffect(UnitId.None, ...)` tưởng là AoE | Nó đánh vào `ctx.Target`; AoE phải lặp `UnitsIn(cells)` |
| 7 | `UnitRegistry.Add/MoveTo` ném `InvalidOperationException` | Luôn `CanPlace` trước, hoặc coi chỗ đặt không hợp lệ là lỗi UI |
| 8 | `Player`/`Boss` null | `IUnitRegistry.Player/Boss` có thể null; kiểm tra trước khi dùng |
| 9 | Trong Duel lại gửi `Attack` | Finisher **tự động** sau Win; `ValidateDuel` chặn Attack bằng `WrongPhase` |
| 10 | Tưởng `EndTurn` qua `TryExecute` chạy AI | Nó chỉ `Turn.EndTurn`; `GameSession.EndTurn()` mới chạy pha Enemy |
| 11 | Sửa state từ UI (gán HP/position) | Không có setter public; mọi thay đổi qua `TryExecute`/effect |

---

## 17. Chạy test

```powershell
# 1) Ngoài Unity — nhanh nhất, dùng dotnet bundled của Unity 6000.6.7f1
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" `
    test "D:\test\Behind the hero\Tools\CoreTests\CoreTests.csproj"

# 2) Build only
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" `
    build "D:\test\Behind the hero\Tools\CoreTests\CoreTests.csproj" -v q --nologo

# 3) Unity EditMode (batchmode). Nếu project gốc đang mở trong Editor, dùng bản copy.
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit `
    -projectPath "D:\test\_core_check" -runTests -testPlatform EditMode `
    -assemblyNames BTH.Core.Tests `
    -testResults "D:\test\_core_check\results.xml" `
    -logFile "D:\test\_core_check\unity.log"

# Đọc kết quả (đợi file xuất hiện; Unity cha có thể exit 0 trước khi con xong)
Select-String -Path "D:\test\_core_check\results.xml" -Pattern "<test-run"
```

Kết quả hiện tại: **64/64 pass** (10 bộ), `dotnet` ~155 ms; Unity EditMode cũng 64/64.

---

## 18. Từ điển Unity cho người mới (đủ để đọc kế hoạch)

| Khái niệm | Nghĩa ngắn |
|-----------|-----------|
| `GameObject` | "Đối tượng" trong scene; chứa component |
| `Component` / `MonoBehaviour` | Hành vi gắn vào GameObject; chạy `Update` |
| `Prefab` | Mẫu GameObject lưu thành asset, đem ra dùng nhiều nơi |
| `Scene` | Màn chơi/không gian chứa GameObject |
| `ScriptableObject` | Asset chứa **dữ liệu** (không nằm trong scene) — nơi để Unit/Skill/Stage |
| `Transform` | Vị trí/xoay/scale của GameObject |
| `SpriteRenderer` | Component vẽ sprite 2D |
| `Canvas` / `RectTransform` | Hệ UI (uGUI) |
| `TMP` (TextMeshPro) | Text UI chất lượng cao |
| `Addressables` | Hệ load asset theo địa chỉ (chưa dùng) |
| `EditMode test` | Test chạy ngay trong Editor, không cần scene — đúng loại Core dùng |
| `PlayMode test` | Test chạy khi game đang play — dùng cho smoke test Unity |
| `asmdef` | File khai báo assembly; quyết định lớp nào được reference lớp nào |
| `UnityEngine.Random` | RNG của Unity — **cấm trong Core** |

---

## 19. Checklist khi bắt đầu Content/UI

**Content (P1):**

- [ ] 9 `UnitDefinition` (Wanderer, Eosphoros, Imp, Gargoyle, 5 boss) + portrait id
- [ ] ~22 `SkillDefinition` + behaviour cho chiêu đặc biệt; đánh dấu `IsClashSkill`
- [ ] 6 brain: MeleeRusher, RangedLurker, 5 boss (kế thừa base), (AllyBrain đã cắt)
- [ ] 5 `StageDefinition` (8×8, 9×9, 9×9, 10×10, 8×8) + terrain override
- [ ] 5 `ClashDefinition` (power 15/14/16/18/18) + `ClashRuleTable`
- [ ] 5 `DuelDefinition` (background/music/transition, boss 2 chiêu, player 3 chiêu, win effect)
- [ ] 1 test simulation: start ải 1 → đánh → Duel → thắng

**Unity/UI (P2–P3):**

- [ ] `GameBootstrap` tạo `GameSession`, load stage, bind presenter
- [ ] `BoardView`/`UnitView`/`BoardHighlighter` đọc event + `IGameStateView`
- [ ] `InputController` → `ActionRequest` (phím mũi tên/G/S/D/E/Space + chuột)
- [ ] `HudPresenter` (HP pips, AP, MP trang trí), command bar
- [ ] `DuelScreenPresenter` (background, 2 bên, 3 thẻ + badge xanh/vàng/đỏ, finisher tự động)
- [ ] Dialogue view + 2 ending; menu/settings/audio
- [ ] PlayMode smoke test: vào scene → đi 1 ô → đánh → EndTurn không lỗi

---

*Hết tài liệu. Khi thêm type/đổi chữ ký, cập nhật mục tương ứng + §2 file index + §12 nếu thêm event.*
