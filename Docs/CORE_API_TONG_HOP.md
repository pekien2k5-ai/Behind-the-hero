# TÓM TẮT CORE — Behind the Hero

> Tài liệu cho đồng đội: mỗi class làm gì, hàm nào dùng nhiều, khi nào gọi.
> Cập nhật: 2026-11-15 · **154 file C# · 0 dòng UnityEngine · 47/47 test xanh** (dotnet + Unity EditMode).
> Đường dẫn gốc: `Behind the hero/Assets/_Project/Core/` — mỗi mục dưới đây ghi đường dẫn tương đối từ đó.

---

## 0. Ba điều phải nhớ trước khi đọc

1. **Core không dùng Unity.** Không `MonoBehaviour`, không `GameObject`, không `Random`, không `Time`. Core là luật chơi + state; Unity chỉ vẽ và nhận input.
2. **Không sửa state trực tiếp.** Mọi thay đổi đi qua `GameSession.TryExecute(ActionRequest)` (hoặc `EndTurn()`); Core kiểm tra luật rồi mới đổi. UI đọc state để vẽ, không tự trừ máu.
3. **UI nghe event, không tự tính.** Mỗi thay đổi phát một event struct; view subscribe để cập nhật. Không ai được nghe event rồi quyết định luật.

```
Content (ScriptableObject)  →  Core (luật, state, event)  →  Unity (view/input)  →  UI
        cấu hình                   154 file, 47 test             trình bày          hiển thị
```

| Lớp | Được dùng gì | Không được dùng |
|---|---|---|
| `BTH.Core` | C# thuần | UnityEngine |
| `BTH.Content` | Core + Unity (SO) | sửa state trực tiếp |
| `BTH.Unity` | Core + Content + InputSystem | tính luật |
| `BTH.UI` | Core + Unity + TMP | tính luật |

---

## 1. Bảng tra nhanh: muốn làm X → đọc Y

| Việc cần làm | Class / file chính |
|---|---|
| Toạ độ, khoảng cách, ô kề | `Grid/GridCoord`, `Grid/GridShapes` ⭐ |
| Ô đi được không, LOS, địa hình | `Grid/IGridMap`, `Grid/GridMap` ⭐ |
| Tìm đường / vùng đi được | `Units/AStarPathfinder`, `Units/ReachableSet` ⭐ |
| Máu, AP, trạng thái của 1 quân | `Units/UnitState` ⭐ |
| Thêm/bớt/di chuyển quân, tra quân ở ô | `Units/IUnitRegistry`, `Units/UnitRegistry` ⭐ |
| Tính sát thương | `Combat/CombatMath`, `Combat/DamageCalculator` ⭐ |
| Gắn/bỏ status (choáng, độc, guard…) | `Combat/StatusSet`, `Combat/*Status` ⭐ |
| Tạo hiệu ứng chiêu (damage, đẩy, đổi địa hình) | `Combat/IGameEffect`, `Combat/*Effect` ⭐ |
| Định nghĩa chiêu mới | `Combat/SkillDefinition`, `Combat/ISkillBehaviour` ⭐ |
| Kiểm tra 1 lệnh có hợp lệ | `Actions/ActionValidator` ⭐ |
| Thi hành 1 lệnh | `Actions/GameActionExecutor` ⭐ |
| Lượt, AP, thứ tự đi | `Turn/ITurnManager`, `Turn/TurnManager` ⭐ |
| Thắng/thua | `Run/ObjectiveEvaluator` ⭐ |
| Chạy cả ván (không Unity) | `Run/GameSession` ⭐ |
| Tua lại / checkpoint | `Run/SnapshotService`, `Run/SnapshotStore` ⭐ |
| Số cân bằng (AP, guard, poison…) | `Run/IGameRules`, `Run/GameRules` ⭐ |
| Đối thoại, ending | `Run/DialogueGraph`, `Run/DialogueRunner` |
| UI cập nhật khi có gì xảy ra | `Run/EventBus` + 26 event ⭐ |

---

## 2. Bản đồ thư mục

| Thư mục | File | Nội dung |
|---|---|---|
| `Grid/` | 7 | toạ độ, ô, lưới, hình học |
| `Units/` | 18 | quân, registry, tìm đường, footprint |
| `Turn/` | 6 | lượt, AP, thứ tự initiative |
| `Actions/` | 15 | request, validate, execute |
| `Combat/` | 45 | damage, status, effect, skill, clash |
| `Ai/` | 8 | intent, telegraph, brain library, boss phase |
| `Run/` | 54 | rules, state, stage, session, snapshot, dialogue, **26 event** |

Quy ước: **1 file = 1 type**. Vì vậy 26 event = 26 file nhỏ (~20 dòng mỗi file). Đừng hoảng vì số file.

---

## 3. Grid — bàn cờ

### `GridCoord` ⭐ — `Grid/GridCoord.cs`
Toạ độ ô nguyên, bất biến, là struct.

```csharp
var c = new GridCoord(2, 3);
int d = GridCoord.Manhattan(a, b);   // khoảng cách 4 hướng (static, 2 đối số!)
int e = GridCoord.Chebyshev(a, b);   // khoảng cách 8 hướng
var zero = GridCoord.None;           // (-1,-1)
bool ok = c.IsValid;                 // X >= 0 && Y >= 0
var n = c + GridCoord.North;         // cộng/trừ vector
```

Có sẵn `North/South/East/West` (đơn vị 1 ô), `==`, `!=`, `ToString()` dạng `(x,y)`.

> ⚠️ `Manhattan` là **static 2 tham số**, không phải `a.Manhattan(b)`. Đây là lỗi compile hay gặp nhất khi mới dùng.

### `TerrainId` ⭐ — `Grid/TerrainId.cs`
Chuỗi có sẵn hằng, **không phải enum** (để Content thêm địa hình mới mà không sửa Core).

`TerrainId.Floor / Wall / Pit / Cracked / Poison / Lava / Chest`, so sánh không phân biệt hoa thường, ép từ string: `(TerrainId)"wall"`.

### `TerrainFlags` — `Grid/TerrainFlags.cs`
`[Flags]`: `None`, `BlocksMove`, `BlocksLineOfSight`, `IsHazard`, `IsCover`, `IsInteractive`.

### `CellState` — `Grid/CellState.cs`
Một ô: `Coord`, `Terrain`, `Flags`, `MovementCost`, `HazardDamagePerTurn`, `TurnsUntilCollapse`, `Occupant`, `IsLocked`.
Tiện ích: `IsOccupied`, `BlocksMove`, `BlocksLineOfSight`, `IsHazard`, `IsInteractive`.
Setter là `internal` — **chỉ Core sửa**; người ngoài đọc.

### `IGridMap` / `GridMap` ⭐ — `Grid/IGridMap.cs`, `Grid/GridMap.cs`
Bàn cờ thật, lưu mảng phẳng `y * Width + x`.

```csharp
new GridMap(width, height, events = null);
map.Get(coord);                 // ném lỗi nếu ngoài lưới → ưu tiên TryGet
map.TryGet(coord, out cell);
map.InBounds(coord);
map.IsBlocked(coord);           // tường/vật cản (không xét quân)
map.IsWalkable(coord, profile); // xét cả quân, khoá sân, bay
map.HasLineOfSight(from, to);   // Bresenham, chỉ chặn ở ô giữa
map.MovementCost(coord);
map.SetTerrain(coord, terrain, flags, movementCost = 1, hazardDamagePerTurn = 0);
map.ArmCollapse(coord, turns);  // hẹn sập sau N lượt
map.TickCollapse();             // gọi trong EndTurn
map.LockArea(cells);            // khoá sân (pha 2 boss)
map.UnlockArea();
map.SetOccupant(coord, unitId); // chỉ UnitRegistry gọi
event CellChanged;              // Action<GridCoord>
```

Quy tắc bay: `CanFly` vượt được ô `BlocksMove` **nếu ô đó không `BlocksLineOfSight`** (bay qua vực/bẫy được, xuyên tường thì không).

### `GridShapes` — `Grid/GridShapes.cs`
Hình học dùng chung, đều trả `List<GridCoord>`:

```csharp
GridShapes.Directions4                    // mảng 4 hướng
GridShapes.Neighbours4(center)            // 4 ô kề (1 đối số!)
GridShapes.Line(from, to)                 // Bresenham, gồm 2 đầu
GridShapes.Cross(center, radius)          // hình thoi Manhattan
GridShapes.Square(center, radius)         // hình vuông Chebyshev
GridShapes.Cone(origin, dirX, dirY, len)  // nón 45°
GridShapes.Row(origin, dirX, dirY, len)   // hàng thẳng, bỏ ô gốc
GridShapes.Footprint(anchor, sizeX, sizeY)// các ô của quân nhiều ô
GridShapes.Contains(cells, coord)
```

---

## 4. Units — quân cờ

### `UnitId` ⭐ — `Units/UnitId.cs`
Số định danh (`int`) bọc struct. `None = 0`, `IsValid = Value > 0`. Registry tự cấp, tăng dần từ 1.

### `UnitFaction` / `UnitArchetype`
`UnitFaction`: `Player, Ally, Enemy, Neutral`.
`UnitArchetype`: `Hero, Ally, Minion, Elite, StageBoss, FinalBoss` (2 loại cuối → `IsBoss = true`).

### `MoveProfile` — `Units/MoveProfile.cs`
Struct readonly, mô tả khả năng di chuyển:
`MovePoints`, `CanFly`, `IgnoresHazard`, `CanPassUnits`; factory `MoveProfile.Walk(n)` / `MoveProfile.Fly(n)`.

### `UnitDefinition` — `Units/UnitDefinition.cs`
"Dữ liệu tĩnh" của một quân (POCO, settable):

`Id, DisplayName, Faction, Archetype, MaxHp, Attack, Armor, Initiative, SizeX, SizeY, Move, NativeElement, SkillIds, BrainId, PortraitId`, `IsBoss`.

Content sẽ dựng object này từ ScriptableObject; Core chỉ đọc.

### `UnitState` ⭐ — `Units/UnitState.cs`
"Trạng thái sống" của một quân trong ván. **Constructor là `internal`** — không tự `new`; hãy dùng `UnitRegistry.Add(definition, anchor)`.

Đọc thường dùng:

```csharp
unit.Id; unit.Definition; unit.Faction; unit.Archetype; unit.IsBoss;
unit.Hp; unit.MaxHp; unit.HpRatio; unit.IsAlive;
unit.Position;                 // ô neo (góc min của footprint)
unit.Center;                   // tâm — dùng để đo tầm đánh
unit.Footprint;                // các ô đang chiếm
unit.ActionPoints;             // AP còn lại
unit.HasMovedThisTurn; unit.HasActedThisTurn;
unit.Statuses;                 // StatusSet
unit.IsStaggered / IsRooted / IsGuarding / IsFlanked;
unit.MovePoints; unit.Move;
```

Setter (`Hp`, `Position`, `ActionPoints`, cờ lượt, cooldown) là `internal`: chỉ Executor/TurnManager/Snapshot được sửa.

### `IUnitRegistry` / `UnitRegistry` ⭐ — `Units/IUnitRegistry.cs`, `Units/UnitRegistry.cs`

```csharp
new UnitRegistry(grid, events = null);
reg.All; reg.Player; reg.Boss;          // Player/Boss có thể null nếu màn không có
reg.Alive(UnitFaction.Enemy);
reg.Get(id); reg.TryGet(id, out unit);
reg.At(coord); reg.IsOccupied(coord);
reg.CanPlace(def, anchor);              // kiểm tra đặt quân
reg.CanPlace(def, anchor, ignore);      // bỏ qua chính mình (push/teleport)
reg.Add(def, anchor);                   // → UnitState + UnitSpawnedEvent; ném lỗi nếu bị chặn
reg.MoveTo(id, anchor);                 // đổi vị trí, KHÔNG phát event; ném lỗi nếu bị chặn
reg.Remove(id);                         // despawn, không event
reg.Kill(id);                           // HP→0 + UnitDiedEvent (đúng 1 lần)
reg.UnitsIn(cells); reg.AdjacentTo(coord); // quân có ô chạm cạnh coord
```

`Add` tự cấp id, cập nhật occupancy + `grid.SetOccupant`. `Kill` chống phát event trùng nhờ cờ `DeathPublished`.

### Đường đi — `Units/AStarPathfinder` ⭐

```csharp
var pathfinder = new AStarPathfinder(grid);
PathResult r = pathfinder.FindPath(new PathRequest(start, goal, profile, maxCost: 0, ignoreUnit));
ReachableSet reach = pathfinder.ComputeReachable(start, profile, maxCost, selfId);
```

- `PathResult`: `Status` (`Found/NoPath/OutOfRange/InvalidStart/InvalidTarget`), `Path` (**gồm cả ô xuất phát**), `Cost`, `IsFound`, `Goal`.
- `PathStatus.OutOfRange` chỉ xuất hiện khi `maxCost > 0`.
- `ReachableSet`: `Origin`, `Cells` (sort Y rồi X), `Costs`, `Count`, `Contains`, `CostTo` (trả -1 nếu không tới được).
- `MovementRangeCalculator.Compute(...)` là tiện ích bọc ngoài.

Tất định: tie-break `f → h → index`, không phụ thuộc thứ tự Dictionary.

---

## 5. Turn — lượt

### `TurnPhase`
`None, StageIntro, Telegraph, Player, Enemy, TurnEnd, Clash, StageEnded`.

### `ActionPoint` — `Turn/ActionPoint.cs`
Struct nhỏ: `Current`, `Max`, `IsEmpty`, `CanSpend(cost)`, `Spend(cost)` → trả `ActionPoint` mới (bất biến).

### `InitiativePolicy` — `Turn/InitiativePolicy.cs`
`Order(units)` → sort Initiative giảm dần, hoà thì `UnitId` tăng dần, bỏ quân chết.

### `ITurnManager` / `TurnManager` ⭐ — `Turn/ITurnManager.cs`, `Turn/TurnManager.cs`

```csharp
new TurnManager(state, initiative = null, objective = null);
turn.Phase; turn.TurnIndex; turn.TurnLimit; turn.ActiveUnit; turn.EnemyOrder;
turn.StartStage(stage); turn.BeginTelegraph();
turn.BeginPlayerPhase(); turn.EndPlayerPhase();
turn.BeginEnemyPhase(); turn.EndEnemyPhase();
turn.EndTurn(); turn.BeginClash(); turn.FinishStage(result);
turn.CanSpend(unitId, cost); turn.TrySpend(unitId, cost);   // TrySpend phát ApChangedEvent
turn.ResetActionPoints(); turn.NextEnemy();
```

`EndTurn()` chạy đúng thứ tự (đừng tự gọi tay các bước):
1. phase `TurnEnd`
2. tick status (`TickTurnEnd` → cooldown → `TickTurnStart`)
3. tick hazard dưới footprint (bỏ qua `IgnoresHazard`) → `TickCollapse`
4. `TurnIndex++` + `TurnAdvancedEvent`
5. `ObjectiveEvaluator.Evaluate`
6. nếu chưa xong: `ResetActionPoints` + checkpoint `TurnStart` + `BeginTelegraph`

`TurnLimit` lấy từ `Stage.TurnLimit` nếu có, ngược lại `Rules.TurnLimit`.

---

## 6. Actions — ra lệnh

### `ActionKind` / `ActionError`
`ActionKind`: `Move, Attack, Skill, Guard, Wait, EndTurn, Reset, ClashSkill` (hiện Executor xử lý tất cả trừ `Wait`/`Reset` — đừng dùng 2 loại này).

`ActionError`: `None, UnknownActor, UnitDead, NotPlayerPhase, NotEnemyPhase, NoActionPoints, OutOfRange, TargetEmpty, NoLineOfSight, CellBlocked, CellOccupied, NotEnoughMovePoints, SkillNotReady, SkillOnCooldown, CannotMoveRooted, ActorStunned, StageEnded, InvalidTarget, ClashNotAvailable, ObjectiveAlreadyEnded, AlreadyMoved, AlreadyActed`.

### `ActionRequest` ⭐ — `Actions/ActionRequest.cs`
Struct readonly; dùng các factory thay vì tự new:

```csharp
ActionRequest.Move(actor, destination);
ActionRequest.Attack(actor, targetCell);
ActionRequest.Skill(actor, skillId, targetCell);
ActionRequest.Guard(actor);
ActionRequest.EndTurn(actor);
ActionRequest.ClashSkill(actor, skillId);
```

### `ValidationResult` / `ActionResult`
`IsValid/Success` + `Error` + `Message`. Tạo bằng `Ok()` / `Fail(error, message)`.

### `IActionValidator` / `ActionValidator` ⭐ — `Actions/ActionValidator.cs`

```csharp
var validator = new ActionValidator(state.Pathfinder);   // pathfinder = null cũng được
ValidationResult v = validator.Validate(request, state);  // THUẦN: không sửa state
IReadOnlyList<GridCoord> cells = validator.GetValidTargets(request, state); // cho UI highlight
```

Thứ tự kiểm: màn đã kết thúc → actor tồn tại → còn sống → phase → choáng → luật riêng từng lệnh.

### `IGameAction` + 5 action class
`MoveAction`, `AttackAction`, `SkillAction`, `GuardAction`, `EndTurnAction` — biểu diễn command (`Id/Kind/Actor/Target`). `GameSession` dùng thẳng `ActionRequest`, nên các class này chủ yếu để log/debug/queue về sau.

### `GameActionExecutor` ⭐ — `Actions/GameActionExecutor.cs`

```csharp
var executor = new GameActionExecutor(validator, objectiveEval = null);
ActionResult r = executor.Execute(request, state);  // sửa state do lệnh; vòng lượt do TurnManager
```

Thứ tự trong mỗi lệnh: trừ AP → đổi state → dựng/chạy effect → phát event → `FinishIfOver` (giết boss thì kết thúc màn ngay).

---

## 7. Combat — chiến đấu

### `ElementId` / `ElementMatrix`
`ElementId`: `Physical, Fire, Holy, Necrotic, Piercing, Temporal, None` (chuỗi).
`ElementMatrix.CreateDefault()`: Holy→Necrotic ×1.5, Holy→Fire ×1.25, Fire→Necrotic ×1.25, Necrotic→Holy ×0.75; Temporal trung tính. Có `Set`, `GetMultiplier`, `Clone`.

### `DamageRequest` / `DamageResult`
`DamageRequest` (readonly fields): `Source, Target, BasePower, Element, IsSkill, IgnoresArmor, HitFrom`.
`DamageResult`: `FinalDamage, Multiplier, WasStaggered, WasGuarded, WasFlanked, Killed, IsZero`.

### `CombatMath` ⭐ — `Combat/CombatMath.cs`
Công thức duy nhất, mọi nơi dùng lại:

```csharp
int power = CombatMath.ComputePower(basePower, armor, elementMult,
                                   ignoresArmor, statusMult);
// base × element − armor (nếu không xuyên giáp) → min 1
// × statusMult → Math.Round → min 1
```

Hệ quả: giáp nặng vẫn chịu tối thiểu 1; Guard ×0.2; Stagger ×2; Flanked ×1.5.

### `IDamageCalculator` / `DamageCalculator` — `Combat/DamageCalculator.cs`

```csharp
new DamageCalculator(matrix = null);   // null → ElementMatrix.CreateDefault()
DamageResult c = calc.Compute(request, state);  // chỉ tính
DamageResult a = calc.Apply(request, state);    // trừ HP, NotifyDamaged, DamageDealtEvent, Kill nếu chết
```

### `StatusSet` ⭐ + 5 status — `Combat/StatusSet.cs`

```csharp
unit.Statuses.Has(StatusIds.Poison);
unit.Statuses.GetRemaining(StatusIds.Poison);
unit.Statuses.Add(new PoisonStatus(3, 2), new StatusContext(unit, events, units));
unit.Statuses.Remove(StatusIds.Guard);
unit.Statuses.DamageTakenMultiplier;  // tích các hệ số
```

| Status | Hệ số nhận damage | Khoá hành động | Khoá di chuyển | Ghi chú |
|---|---|---|---|---|
| `StaggerStatus` | ×2 | ✔ | ✔ | mặc định 1 lượt |
| `GuardStatus` | ×0.2 | ✘ | ✘ | hết sau 1 lượt |
| `FlankedStatus` | ×1.5 | ✘ | ✘ | tự xoá khi bị đánh |
| `RootStatus` | ×1 | ✘ | ✔ | 1 lượt |
| `PoisonStatus` | ×1 | ✘ | ✘ | trừ HP cuối lượt, phát `DamageDealtEvent` Necrotic |

Quan trọng: status gắn **giữa lượt** có `CanTick = false` → cuối lượt đó không tick; sang lượt kế mới bắt đầu đếm. Nhờ vậy Stagger sau Clash còn tác dụng sang lượt người chơi.

### `IGameEffect` + các effect ⭐ — `Combat/*Effect.cs`
Mỗi hiệu ứng là một object nhỏ, có `Priority` và `Resolve(ctx)`. `EffectQueue` sort ổn định theo priority, dừng nếu nguồn chết.

| Effect | Priority | Tác dụng |
|---|---|---|
| `DamageEffect(target, power, element, isSkill, ignoresArmor)` | 10 | gây damage (None → dùng `ctx.Target`) |
| `HealEffect(target, amount)` | 12 | hồi máu |
| `ApplyStatusEffect(target, effect)` | 20 | gắn status |
| `RemoveStatusEffect(target, statusId)` | 21 | bỏ status |
| `PushEffect(target, direction, distance)` | 30 | đẩy, dừng khi gặp vật cản |
| `TeleportEffect(target, destination)` | 35 | dịch chuyển |
| `SpawnTerrainEffect(cell, terrain, flags, moveCost, hazard, collapseIn)` | 40 | đổi địa hình |
| `ArmCollapseEffect(cell, turns)` | 41 | hẹn sập ô |
| `LockArenaEffect(cells)` | 45 | khoá sân |
| `CameraShakeEffect(strength, duration)` | 90 | gợi ý rung camera |

`EffectContext` cung cấp: `State, Queue, Units, Events, Random, Source, Origin, Target, TargetCell, Skill, Power, Element, TargetUnit, ResolvedCell, UnitAt(cell)`.

### `SkillDefinition` / `ISkillBehaviour` / `SkillLibrary` ⭐

```csharp
var skill = new SkillDefinition {
    Id = "holy-pillar", DisplayName = "Holy Pillar",
    Targeting = SkillTargeting.Cell, RangeMin = 1, RangeMax = 3,
    Power = 6, Element = ElementId.Holy, ApCost = 1, CooldownTurns = 2,
    RequiresLineOfSight = true, IsClashSkill = false,
    Behaviour = myBehaviour   // null → damage đơn mục tiêu
};
skills.Add(skill);
```

`ISkillBehaviour.BuildEffects(ctx)` — nơi duy nhất mô tả hình dạng chiêu:

```csharp
class HolyPillarBehaviour : ISkillBehaviour {
    public void BuildEffects(EffectContext ctx) {
        var cells = GridShapes.Cross(ctx.TargetCell, 1);      // 3 ô chữ thập
        foreach (var unit in ctx.Units.UnitsIn(cells))        // quân trong vùng
            ctx.Queue.Enqueue(new DamageEffect(unit.Id, ctx.Power,
                                              ElementId.Holy, isSkill: true));
    }
}
```

`ctx.Queue` cùng `ctx.Power/ctx.Element` giúp tái sử dụng. Lưu ý: `DamageEffect(UnitId.None, ...)` chỉ đánh vào **ctx.Target** (một mục tiêu), không phải toàn vùng — muốn AoE phải lặp `ctx.Units.UnitsIn(cells)`.

### Clash — `Combat/Clash*.cs`
- `ClashRuleTable`: `Add(rule)` theo cặp chiêu, `AddElement(a,b,bonus)` theo cặp hệ, mặc định **+3**.
- `ClashDefinition`: `BossSkillId, BossPower, BossElement, Rules, WinEffect, LoseEffect` (`Func<IStatusEffect>`).
- `ClashResolver.Predict(...)` → `ClashPrediction(Result, PlayerPower, BossPower, Bonus)`; `Resolve(...)` → `ClashOutcome` + gắn status; `GetOptions(...)` → danh sách `ClashOption` cho UI.
- Công thức: `EffectivePower = skill.Power + bonus`; `>` Win, `==` Draw, `<` Lose. Không RNG.

### Duel — `Combat/Duel*.cs`, `Combat/ClashDuel.cs` ⭐
Phase 2 đấu chiêu 1v1, **không còn bàn cờ/AP/End Turn**.

- `DuelDefinition`: `Id, BackgroundId, MusicId, TransitionId, BossSkillIds, PlayerSkillIds, RoundLimit, DrawChipRatio (0.10), FinisherCount, Rules, WinEffect, LoseEffect`.
- `DuelState`: `Round, BossSkillIndex, BossSkillId, BossPower, BossElement, AwaitingFinisher, FinishersRemaining, LastResult, IsOver, Winner, SeenBossSkills`.
- `ClashMath.EffectivePower(skill, bonus)`; `Compare(playerPower, bossPower)`.
- `IClashDuel` / `ClashDuel`:
  - `Start(state, definition, player, boss)` — clear status 2 bên, publish `ClashStartedEvent`, vào vòng 1.
  - `GetOptions()` → 3 `ClashOption` kèm `ClashPrediction`.
  - `Choose(skillId)` → `ClashRoundResult`:
    - Win: gắn `WinEffect` lên boss + `AwaitingFinisher = true` (1 đòn kết liễu).
    - Draw: mỗi bên mất `round(power × 0.10)`.
    - Lose: player ăn đúng `BossPower` + `LoseEffect`.
  - `TryFinisher(out DamageResult)` — đánh thường của player lên boss (Stagger ×2), hết finisher thì sang vòng mới.
- Kích hoạt: `GameSession` tự gọi `TryEnterDuel` sau mỗi lệnh thành công khi `boss.HpRatio < rules.Phase2Threshold`; `EnterDuel()` public để test/UI ép thủ công.
- `TurnPhase.Duel`: `EndTurn` trả `ActionError.WrongPhase`; validator chỉ nhận `ClashSkill` (khi chưa `AwaitingFinisher`) và `Attack` (khi `AwaitingFinisher`).
- `DuelSnapshot` nằm trong `GameSnapshot.Duel` nên rewind giữa Duel hoạt động; `DuelRoundStartedEvent(round, bossSkillId, bossPower)`, `DuelEndedEvent(lastResult, winner)`.

---

## 8. Ai — trí tuệ địch

### `EnemyIntent` — `Ai/EnemyIntent.cs`
Bất biến sau khi công bố: `Unit, SkillId, TargetCell, DangerCells, Effects, Priority`, `IsEmpty`, `Empty(unit)`.

### `IEnemyBrain` ⭐ — `Ai/IEnemyBrain.cs`

```csharp
public interface IEnemyBrain {
    string Id { get; }
    EnemyIntent Plan(UnitState self, GameState state);  // phải THUẦN + tất định
}
```

Đăng ký: `brains.Register(new MeleeRusherBrain())`, rồi gán `UnitDefinition.BrainId = "melee"`.

### `TelegraphService` — `Ai/TelegraphService.cs`
`Declare(intent)`, `Clear()`, `All`, `DangerCells` (hợp nhất, sort Y rồi X), `TryGet(unit, out intent)`.
`Declare` phát `IntentDeclaredEvent`; `Clear` phát `TelegraphClearedEvent`.

### `BossPhaseController` — `Ai/BossPhaseController.cs`
`ShouldEnterPhase2(hp, maxHp)` theo ngưỡng `Rules.Phase2Threshold` (0.7); `EnterPhase2(state)` → phase 2, khoá sân (nếu có cells), phát `BossPhaseChangedEvent`. Chỉ 1 lần.

---

## 9. Run — vòng chơi

### `GameRules` ⭐ — `Run/GameRules.cs`

| Thuộc tính | Mặc định |
|---|---|
| `ApPerTurn` | 2 |
| `MovePoints` | 2 |
| `TurnLimit` | 25 |
| `Phase2Threshold` | 0.7 |
| `GuardMultiplier` | 0.2 |
| `StaggerMultiplier` | 2.0 |
| `FlankMultiplier` | 1.5 |
| `PoisonPerTurn` | 3 |

Đây là chỗ duy nhất chứa số cân bằng. Đừng hard-code trong luật.

### `XorShiftRandom` — `Run/XorShiftRandom.cs`
`IRandomSource`: `State` (get/set, dùng cho snapshot), `NextInt(min, maxExclusive)`, `NextFloat()`. Seed 0 tự đổi thành hằng khác 0.

### `GameState` ⭐ — `Run/GameState.cs`
"Túi đồ" của một ván: `Grid, Units, Pathfinder, Turn, Rules, Events, Random, Skills, Damage, Telegraph, Clash, BossPhase, Snapshots, Knowledge, Stage, Result, IsOver`.

### `StageDefinition` ⭐ / `UnitSpawnDefinition` / `TerrainOverride`

```csharp
var stage = new StageDefinition {
    Id = "ai-1", DisplayName = "Oakhaven",
    Width = 8, Height = 8,
    Player = heroDefinition, PlayerSpawn = new GridCoord(0, 0),
    TurnLimit = 25, BossId = bossDefinition.Id,
    Clash = clashDefinition
};
stage.Spawns.Add(new UnitSpawnDefinition(bossDefinition, new GridCoord(6, 6)));
stage.Terrains.Add(new TerrainOverride(new GridCoord(2, 2), TerrainId.Wall,
                                       TerrainFlags.BlocksMove | TerrainFlags.BlocksLineOfSight));
```

`Spawns`/`Terrains` là list chỉ-đọc-tham-chiếu: **Add vào list**, không gán mới.
`TerrainOverride` là struct readonly → dùng constructor positional (như trên), không object initializer.

### `ObjectiveEvaluator` ⭐ — `Run/ObjectiveEvaluator.cs`
`Evaluate(state, stage)` theo thứ tự: đã có kết quả → trả luôn; player chết → `DefeatPlayerDead`; boss chết → `Victory`; quá `TurnLimit` → `DefeatTurnLimit`; ngược lại `None`.

### `GameSession` ⭐ — `Run/GameSession.cs`
Composition root: lắp ráp toàn bộ service, chạy nhịp lượt.

```csharp
var session = new GameSession();           // seed mặc định 20260101
session.RegisterStage(stage);
session.StartStage("ai-1");

session.State;        // GameState
session.Events;       // IEventBus (null trước StartStage)
session.Skills; session.Brains; session.Knowledge;

ActionResult r = session.TryExecute(ActionRequest.Attack(heroId, targetCell));
ActionResult r2 = session.EndTurn();     // chạy đủ Enemy phase rồi sang lượt mới
session.Restart();                        // chơi lại màn hiện tại: RNG về seed, RunKnowledge mới
session.Reset();                          // alias của Restart
```

`TryExecute` chỉ chạy executor + kiểm tra phase 2 boss; **không chạy AI**. `EndTurn()` mới chạy `BeginEnemyPhase → RunEnemyBrains → EndEnemyPhase → Turn.EndTurn → plan telegraph → BeginPlayerPhase`.

Thứ tự dựng trong `StartStage`: EventBus → GridMap → UnitRegistry → GameState → Pathfinder/Damage/Telegraph/Clash/BossPhase/Knowledge/Turn/Snapshots → apply terrain → spawn unit → `StartStage` (turn) → checkpoint StageStart → plan telegraph → player phase.

### Snapshot — `Run/GameSnapshot.cs`, `Run/SnapshotService.cs`, `Run/SnapshotStore.cs`

```csharp
state.Snapshots.CanRewind;
GameSnapshot snap = state.Snapshots.Capture();
state.Snapshots.Restore(snap);
state.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart);
state.Snapshots.Rewind();          // về checkpoint gần nhất + RewindPerformedEvent
```

`GameSnapshot` chứa: turn, phase, result, RNG state, cells, locked cells, units (HP/AP/flags/status/cooldown), intents. `ContentEquals` so từng trường (dùng cho test).
`SnapshotStore` giữ tối đa 8 ảnh gần nhất (ring buffer), không ghi đĩa.
Restore đúng thứ tự: RNG → lưới → registry → từng unit → telegraph → result → turn.

### `RunKnowledge` — `Run/RunKnowledge.cs`
Bộ nhớ **sống ngoài snapshot** (rewind không xoá): `MarkSeen`, `HasSeen`, `Increment`, `GetCounter`, `Deaths`, `RegisterDeath`. Hằng: `DeathsKey = "deaths"`, `BossSkillSeenPrefix = "boss-skill:"`.

### Dialogue — `Run/Dialogue*.cs`
`DialogueNode` (Id, Speaker, Text, NextId, Choices, Ending), `DialogueChoice` (Text, NextId, Condition, HideWhenFalse), `DialogueGraph` (Add, TryGet, Get, Validate), `DialogueRunner` (Start, Advance, Choose, Current, IsFinished, Ending). Lựa chọn có điều kiện dựa trên `IRunKnowledge`.

### `EventBus` ⭐ — `Run/EventBus.cs`

```csharp
using var sub = session.Events.Subscribe<DamageDealtEvent>(
    (in DamageDealtEvent e) => Debug.Log(e.Amount));
session.Events.Publish(new CameraShakeHintEvent(0.3f, 0.2f));
```

`delegate void RefEventHandler<T>(in T) where T : struct` → không boxing. Publish duyệt bản sao subscriber, nên tự huỷ trong handler an toàn.
**Quy tắc:** state đổi xong mới publish; không sửa state trong handler.

#### 26 event

| Event | Phát khi | Trường |
|---|---|---|
| `StageStartedEvent` | màn bắt đầu | StageId, TurnLimit |
| `TurnPhaseStartedEvent` | vào phase | Phase, TurnIndex |
| `TurnAdvancedEvent` | sang lượt | TurnIndex |
| `UnitSpawnedEvent` | đặt quân | Unit, Position |
| `UnitMovedEvent` | di chuyển/đẩy | Unit, From, To, Cost |
| `UnitActedEvent` | hành động xong | Unit, Kind, Target |
| `ApChangedEvent` | AP đổi | Unit, ActionPoints |
| `DamageDealtEvent` | gây damage | Source, Target, Amount, Element, WasStaggered, WasGuarded, WasFlanked, Killed, At |
| `UnitDiedEvent` | chết | Unit, At |
| `StatusAppliedEvent` | gắn status | Unit, Status, Duration |
| `StatusExpiredEvent` | hết status | Unit, Status |
| `SkillUsedEvent` | dùng chiêu | Unit, SkillId, Target |
| `IntentDeclaredEvent` | công bố ý đồ | Intent |
| `TelegraphClearedEvent` | xoá telegraph | Count |
| `CellChangedEvent` | ô đổi | Coord, Terrain, Flags, HazardDamagePerTurn |
| `CellCollapsedEvent` | ô sập | Coord |
| `HazardTriggeredEvent` | dính hazard | Coord, Unit, Damage |
| `ArenaLockedEvent` | khoá sân | CellCount |
| `BossPhaseChangedEvent` | boss sang phase | Boss, Phase |
| `ClashStartedEvent` | bắt đầu đọ chiêu | Boss, BossSkillId |
| `ClashResolvedEvent` | xong đọ chiêu | Boss, PlayerSkillId, Result, PlayerPower, BossPower |
| `StageEndedEvent` | màn kết thúc | Result, TurnIndex |
| `SnapshotPushedEvent` | tạo checkpoint | Trigger, TurnIndex |
| `RewindPerformedEvent` | tua lại | Trigger, TurnIndex |
| `RunKnowledgeUpdatedEvent` | ký ức run đổi | Key, Value |
| `CameraShakeHintEvent` | gợi ý rung camera | Strength, Duration |

---

## 10. Ba luồng mẫu

### A. Một đòn đánh

```
ActionRequest.Attack(heroId, cell)
  → ActionValidator.Validate          (thuần, trả lỗi nếu sai)
  → GameActionExecutor.Execute
      → TurnManager.TrySpend(1)      → ApChangedEvent
      → queue.Enqueue(DamageEffect)
      → queue.Run(ctx)
          → DamageCalculator.Apply   → DamageDealtEvent
          → nếu HP = 0 → UnitRegistry.Kill → UnitDiedEvent
      → UnitActedEvent
      → FinishIfOver → ObjectiveEvaluator → StageEndedEvent (nếu xong)
```

### B. Một lượt đầy đủ

```
session.EndTurn()
  → Turn.BeginEnemyPhase
  → RunEnemyBrains (resolve intent đã công bố)
  → Turn.EndEnemyPhase
  → Turn.EndTurn: tick status → hazard → collapse → TurnIndex++
                  → objective → checkpoint TurnStart → telegraph
  → PlanEnemyIntents (công bố intent cho lượt kế)
  → Turn.BeginPlayerPhase (AP = 2)
```

### C. Rewind

```
state.Snapshots.PushCheckpoint(SnapshotTrigger.TurnStart)   // tự động trong EndTurn
...
state.Snapshots.Rewind()
  → Restore(checkpoint cuối)  → RewindPerformedEvent
```

---

## 11. Bảng hằng số & công thức

| Đại lượng | Giá trị |
|---|---|
| AP mỗi lượt | 2 (1 move + 1 action) |
| Move mặc định | 2 ô, 4 hướng, không chéo |
| Tầm đánh thường | 1 (Manhattan từ Center) |
| Damage tối thiểu | 1 |
| Guard | ×0.2 damage nhận |
| Stagger | ×2 damage nhận, khoá 1 lượt |
| Flanked | ×1.5 damage nhận |
| Poison mặc định | 3 HP/lượt |
| Boss phase 2 | HP ≤ 70% |
| Clash mặc định | +3 power |
| Checkpoint | ring 8, StageStart/TurnStart/ClashStart |

---

## 12. Cách mở rộng (không sửa Core)

**Thêm chiêu mới**
1. Viết class `ISkillBehaviour` (dùng `ctx.Queue.Enqueue(...)`, `GridShapes.*`).
2. Tạo `SkillDefinition` và `skills.Add(skill)`.
3. Thêm id vào `UnitDefinition.SkillIds` / `ClashDefinition`.

**Thêm AI mới**
1. Viết class `IEnemyBrain` (Plan thuần, trả `EnemyIntent` với danger cells).
2. `brains.Register(brain)`.
3. Gán `UnitDefinition.BrainId`.

**Thêm màn mới**
1. Dựng `StageDefinition` (kích thước, spawn, terrain, turn limit, boss, clash).
2. `session.RegisterStage(stage)` → `session.StartStage(stage.Id)`.

**Thêm status mới**
1. Viết class `IStatusEffect` (Id, Duration, hệ số, hook).
2. Thêm `StatusId` vào `StatusIds`.
3. Gắn qua `ApplyStatusEffect` hoặc `unit.Statuses.Add(...)`.

**Thêm event mới**
1. Tạo `readonly struct` trong `Run/`.
2. `Publish(new XEvent(...))` ở nơi state đã ổn định.
3. View `Subscribe<XEvent>(...)`. Không cần đăng ký trước.

**Thêm test**
1. Dùng `Assets/Tests/TestWorld.cs` (`Unit`, `Boss`, `State`, `Stage`).
2. Thêm file `*Tests.cs` với `Assert.That`.
3. Chạy lệnh ở mục 14.

---

## 13. Mười lỗi thường gặp

1. **`new UnitState(...)`** — constructor là `internal`. Dùng `registry.Add(def, anchor)`.
2. **`GridCoord.Manhattan(a)`** — sai; phải `GridCoord.Manhattan(a, b)`.
3. **`GridShapes.Neighbours4(c, 1)`** — sai; chỉ nhận `center`.
4. **Gán `stage.Spawns = ...`** — không được; list chỉ đọc tham chiếu → `stage.Spawns.Add(...)`.
5. **Object initializer cho struct readonly** (`TerrainOverride`, `MoveProfile`, `ActionRequest`, `DamageRequest`) — dùng constructor.
6. **`grid.Get` ngoài lưới** → ném `ArgumentOutOfRangeException`; dùng `TryGet`.
7. **Tưởng status tick ngay lượt gắn** — không; `CanTick = false` nên sống hết lượt kế.
8. **Dùng `TryExecute(EndTurn)` rồi tưởng AI đã chạy** — không; muốn đủ nhịp dùng `session.EndTurn()`.
9. **Subscribe trước `StartStage`** — `session.Events` còn null.
10. **Sửa state trong event handler** — làm lệch thứ tự publish; handler chỉ nên đọc.

11. **`DamageEffect(UnitId.None, ...)` không phải AoE** — nó rơi về `ctx.Target` (một unit). Muốn đánh vùng, lặp `ctx.Units.UnitsIn(cells)`.

Thêm: `ClashDefinition.WinEffect` là `Func<IStatusEffect>` — mỗi lần gọi tạo instance mới; đừng cache instance vào nhiều nơi.

---

## 14. Chạy test

**Ngoài Unity (nhanh, ~120 ms):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" `
  test "D:\test\Behind the hero\Tools\CoreTests\CoreTests.csproj"
```

**Trong Unity (EditMode, batchmode):** phải đóng Editor đang mở project (hoặc dùng bản copy), sau đó:

```powershell
Unity.exe -batchmode -projectPath "<copy>" -runTests -testPlatform EditMode `
          -assemblyNames BTH.Core.Tests -testResults "<copy>\results.xml" -logFile "<copy>\unity.log"
```

Test asmdef phải có `"includePlatforms": ["Editor"]`, nếu không Unity xếp vào PlayMode và chạy 0 test.

**8 bộ test hiện có (47 test):** GridTests (8), PathfindingTests (6), TurnFlowTests (5), DamageTests (6), ClashTests (7), AiDeterminismTests (4), SnapshotTests (4), StageObjectiveTests (7).

---

## 15. Ghi chú cho người chưa quen Unity

| Từ Unity | Nghĩa | Trong Core |
|---|---|---|
| GameObject | vật thể trong scene | không có |
| Prefab | khuôn mẫu vật thể | không có |
| MonoBehaviour | class gắn vào vật thể, chạy mỗi frame | không có |
| Scene | file lưu màn chơi | thay bằng `StageDefinition` (dữ liệu luật) |
| ScriptableObject | asset dữ liệu trong project | Content dùng, rồi chuyển thành POCO của Core |
| `Debug.Log` | in log | tương đương `Console.WriteLine` khi chạy ngoài Unity |

Muốn hình dung: **Core là trọng tài + quyển luật**, Unity là **người quay phim và khán giả**. Trọng tài chỉ cần biết toạ độ, máu, lượt — không cần nhìn thấy sprite.

---

*Tài liệu này mô tả đúng public API tại thời điểm 2026-11-15. Nếu sửa Core, cập nhật lại mục tương ứng.*
