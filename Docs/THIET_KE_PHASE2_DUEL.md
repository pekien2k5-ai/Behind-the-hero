# THIẾT KẾ PHASE 2 — "DUEL MODE" (đã chốt với chủ dự án)

> Chốt ngày 2026-11-15: Phase 2 **không còn là bàn cờ**. Player và boss đứng 2 bên màn hình, nền chỉ là background, người chơi chỉ đánh chiêu đối chiêu (Clash) tới khi một bên gục, có chuyển cảnh khi vào pha.
> Quyết định đầy đủ: `Docs/QUYET_DINH.md`. Kế hoạch tổng: `KE_HOACH_HOAN_THANH_GAME.md`.

---

## 1. Luồng tổng quát

```
Phase 1 (bàn cờ, hiện có)
  Telegraph → Player → Enemy → TurnEnd (lặp)
        │
        │  HP boss < 70% (sau khi một đòn resolve xong)
        ▼
  TRANSITION CUTSCENE  (~1,2 s, khoá input, không skip lần đầu)
    - boss_roar + transform_dark + rung màn hình + flash
    - fade đen → fade in background mới
    - đổi nhạc sang boss_theme (pitch thấp hơn)
        │
        ▼
  DUEL INTRO
    - player bên trái, boss bên phải (background full-screen)
    - HP bar 2 bên, tên boss + "PHASE 2"
    - không render lưới, không AP, không End Turn
        │
        ▼
  DUEL ROUND (lặp)
    1. Boss công bố chiêu (banner tên chiêu + hệ + uy lực)
    2. Người chơi chọn 1 trong 3 chiêu Clash (badge Win/Draw/Lose dự đoán)
    3. Resolve → hiệu ứng + damage + status
    4. Nếu thắng: boss Stagger (hoặc hiệu ứng riêng của ải) → player đánh kết liễu
    5. Kiểm tra chết → kết thúc, ngược lại sang round kế
        │
        ├── boss chết  → VICTORY (Ending chọn ở dialogue cuối ải 5)
        └── player chết → DEFEAT  → Rewind về ClashStart
```

**Bất biến:**
- Trong Duel **không** có Move / Guard / Wait / EndTurn / terrain / hazard / turn limit.
- TurnIndex **đóng băng** (không tăng), AP không dùng.
- HP cả 2 bên **mang theo** từ phase 1; **status clear** khi vào Duel.
- Snapshot/rewind vẫn hoạt động: checkpoint `ClashStart` được đẩy ngay lúc vào Duel.
- Phase 1 của màn boss là **solo 1v1, chỉ boss** (không Imp/Gargoyle).

---

## 2. Data model mới (Core)

### 2.1 `DuelDefinition` (Content dựng, Core chỉ thấy POCO)

```csharp
public sealed class DuelDefinition
{
    public string Id;
    public string BackgroundId;          // "bg_malkor_fire"
    public string MusicId;               // "boss_theme"
    public string TransitionId;          // "transition_malkor"

    public List<string> BossSkillIds;    // chu kỳ 2 chiêu: tuyệt kỹ → đòn thường → lặp
    public List<string> PlayerSkillIds;  // đúng 3 lựa chọn Clash

    public int RoundLimit = 0;           // 0 = không giới hạn (mặc định)
    public float DrawChipRatio = 0.10f;  // hoà: mỗi bên mất 10% uy lực của mình
    public int FinisherCount = 1;        // ải 4 = 3 nhát chém liên hoàn

    public Func<IStatusEffect> WinEffect;  // mặc định StaggerStatus lên boss
    public Func<IStatusEffect> LoseEffect; // mặc định null (chỉ damage)
}
```

### 2.2 `DuelState`

```csharp
public sealed class DuelState
{
    public string DuelId;
    public int Round;                 // 1-based
    public int BossSkillIndex;        // con trỏ trong BossSkillIds
    public string BossSkillId;
    public int BossPower;
    public ElementId BossElement;
    public bool BossStaggered;
    public bool AwaitingFinisher;
    public int FinishersRemaining;    // = FinisherCount khi vừa thắng
    public ClashResult LastResult;
    public bool IsOver;
    public UnitId Winner;             // None nếu chưa xong
    public List<string> SeenBossSkills;
}
```

### 2.3 `IClashDuel`

```csharp
public interface IClashDuel
{
    DuelState State { get; }
    bool IsActive { get; }

    void Start(GameState state, DuelDefinition definition, UnitState player, UnitState boss);
    IReadOnlyList<ClashOption> GetOptions();
    ClashRoundResult Choose(string playerSkillId);
    bool TryFinisher(out DamageResult damage);   // gọi nhiều lần tới khi hết FinishersRemaining
    DuelSnapshot Capture();
    void Restore(DuelSnapshot snapshot);
}
```

### 2.4 Kết quả một round

```csharp
public readonly struct ClashRoundResult
{
    public ClashResult Result;      // Win / Draw / Lose
    public int PlayerPower;         // skill.Power + counter bonus
    public int BossPower;
    public int DamageToPlayer;      // Lose: bossPower (xuyên giáp); Draw: 10% boss power
    public int DamageToBoss;        // Draw: 10% player power
    public IStatusEffect Applied;   // WinEffect lên boss / LoseEffect lên player
}
```

Quy tắc resolve (dùng chung `ClashRuleTable.GetBonus` với phase Clash):

| Kết quả | Điều kiện | Hệ quả |
|---|---|---|
| **Win** | PlayerPower > BossPower | boss **không mất HP ngay**; gắn `WinEffect` (mặc định Stagger); player được `FinisherCount` đòn kết liễu |
| **Draw** | bằng nhau | mỗi bên mất `round(power * 10%)` HP (xuyên giáp), không status |
| **Lose** | PlayerPower < BossPower | player mất `BossPower` HP (xuyên giáp); `LoseEffect` nếu có |

Finisher: `DamageCalculator.Apply(DamageRequest(player → boss, BasePower = player.Attack, element = player.NativeElement, ignoresArmor: false))` — ăn multiplier từ status của boss (Stagger ×2, Flanked ×1.5). Hết `FinishersRemaining` → round++.

Kết thúc: boss chết → Winner = player; player chết → Winner = boss; `IsOver = true`; publish `DuelEndedEvent`.

---

## 3. Sửa Core — danh sách file

| File | Thay đổi |
|---|---|
| `Core/Turn/TurnPhase.cs` | thêm `Duel` (sau `Clash`) |
| `Core/Run/GameState.cs` | thêm `public IClashDuel Duel { get; internal set; }` |
| `Core/Combat/DuelDefinition.cs` | mới (§2.1) |
| `Core/Combat/DuelState.cs` | mới (§2.2) |
| `Core/Combat/ClashRoundResult.cs` | mới (§2.4) |
| `Core/Combat/ClashMath.cs` | `EffectivePower(skill, bonus)`, `Compare(a,b)` dùng chung |
| `Core/Combat/IClashDuel.cs` + `ClashDuel.cs` | mới (§2.3) |
| `Core/Run/DuelSnapshot.cs` | mới (Round, BossSkillIndex, BossSkillId, BossPower, BossElement, Staggered, AwaitingFinisher, FinishersRemaining, LastResult, IsOver, Winner, SeenBossSkills) |
| `Core/Run/GameSession.cs` | `TryEnterBossPhase()` → `EnterDuel()` khi HP < 70%; `TryExecute` route ClashSkill/Attack; `EndTurn` bỏ qua khi Duel; clear telegraph |
| `Core/Actions/ActionValidator.cs` | Duel: chỉ `ClashSkill` và `Attack` khi `AwaitingFinisher`; còn lại lỗi `WrongPhase` |
| `Core/Actions/ActionError.cs` | thêm `WrongPhase` |
| `Core/Actions/GameActionExecutor.cs` | `ClashSkill` → `Duel.Choose`; `Attack` trong Duel → `Duel.TryFinisher`; bỏ nhánh Clash cũ trong grid |
| `Core/Run/ObjectiveEvaluator.cs` | không áp `TurnIndex > TurnLimit` khi `Phase == Duel` |
| `Core/Run/GameSnapshot.cs` + `SnapshotService` | thêm `DuelSnapshot`, Capture/Restore/ContentEquals |
| `Core/Run/Events/` | `DuelRoundStartedEvent(round, bossSkillId, bossPower)`, `DuelEndedEvent(result, winner)` |
| `Core/Turn/TurnManager.cs` | `BeginDuel()`: Phase=Duel + checkpoint ClashStart; `EndTurn` no-op khi Duel |

**Test mới `Assets/Tests/DuelFlowTests.cs` (8 ca):**
1. Boss HP < 70% → phase `Duel`, checkpoint `ClashStart`, HP giữ nguyên, status bị clear.
2. Thắng Clash → boss Stagger + `AwaitingFinisher`; Finisher trừ đúng ×2.
3. Hoà → cả 2 mất 10% uy lực.
4. Thua → player mất đúng `BossPower`.
5. Finisher giết boss → `Victory` + `StageEndedEvent`.
6. Lose giết player → `DefeatPlayerDead`.
7. Validator: Duel từ chối Move/EndTurn/Guard; cho ClashSkill; Attack chỉ khi `AwaitingFinisher`.
8. Snapshot giữa Duel → Restore trùng khớp; rewind từ thua về round 1, `RunKnowledge` giữ chiêu boss đã thấy.

---

## 4. Content — 5 màn Duel

| Ải | Boss | Background | Chiêu boss (uy lực/hệ) | 3 lựa chọn player | Hiệu ứng thắng |
|---|---|---|---|---|---|
| 1 | Đao Phủ Tro Tàn | `bg_oakhaven_ruins` | Tro Tàn Cuồng Nộ (15, Physical) + Bổ Rìu (10, Physical) | Blade Guard, Swift Slash, Necrotic Burst | Stagger boss |
| 2 | Vu Nữ Morbida | `bg_catacomb_toxic` | Bão Khí Độc (14, Necrotic) + Độc Vũ (9, Necrotic) | Necrotic Ward, Necrotic Burst, Protect | Poison 3 HP/lượt ×2 lên boss |
| 3 | Kỵ Sĩ Hư Không | `bg_void_arena` | Tia Thương Hư Không (16, Piercing) + Thương Đâm (11, Piercing) | Temporal Step, Swift Slash, Protect | Flanked boss (×1.5) |
| 4 | Quỷ Vương Malkor | `bg_fire_arena` | Cự Kiếm Hỏa Ngục (18, Fire) + Hỏa Cầu (12, Fire) | Temporal Step, Blade Guard, Swift Slash | Stagger boss + 3 đòn kết liễu |
| 5 | Eosphoros Sa Ngã | `bg_mausoleum` | Thánh Quang Trảm (18, Holy) + Thánh Kích (12, Holy) | Blade Guard, Swift Slash, Necrotic Burst | Stagger boss |

- `ClashRuleTable` mỗi ải: bonus cặp chiêu (theo docx), fallback bonus hệ, default +3; ải 4 Temporal Step/Dodge +5; ải 5 Blade Guard +4.
- Ải 2: `WinEffect = () => new PoisonStatus(3, 2)`, `FinisherCount = 1`, không Stagger.
- Ải 3: `WinEffect = () => new FlankedStatus(1)`, `FinisherCount = 1`.
- Ải 4: `WinEffect = () => new StaggerStatus()`, `FinisherCount = 3`.
- Ải 1/5: Stagger, `FinisherCount = 1`.
- Tên chiêu: id `blade-guard` dùng chung cho Protect/Blade Guard; docx dùng lẫn hai tên, hiển thị "Blade Guard".

File gợi ý: `Content/Duels/DuelCatalog.cs` + 5 `DuelDefinition`; `Content/Skills/Clash/` cho chiêu clash.

---

## 5. UI / Art

**DuelScreen (uGUI):**
- Background full-screen; player trái (idle/attack), boss phải (Malkor to hơn).
- 2 thanh HP + tên; badge "PHASE 2"; round counter; nút Settings nhỏ.
- Banner chiêu boss (tên + hệ + uy lực) trước khi chọn.
- 3 card chiêu: icon, tên, power, badge xanh/vàng/đỏ.
- Nút "Kết liễu" (hiện `FinishersRemaining`); ải 4 hiện 3 chấm.
- Flash khi clash, shake khi Lose, slow-motion khi boss chết.

**Transition:** 1 prefab, tham số màu/nhạc; 1,2 s; rung + flash + `boss_roar` + `transform_dark` → fade đen → fade in background.

**Art cần:** 5 background 16:9, 1 pose phase-2 mỗi boss (dùng lại frame attack), VFX lửa/độc/hư không (đã có 3 sheet).

---

## 6. Ảnh hưởng kế hoạch

| Pha | Tăng |
|---|---|
| P1 Content | +1 ngày (DuelCatalog + 5 definition + clash skills) |
| P2 Unity | +0,5 ngày (DuelScreen khung + background) |
| P3 UI | +1,5 ngày (DuelScreen đầy đủ + transition) |
| P4 Flow | +1 ngày (EnterDuel, validator/executor, snapshot, test) |
| P5 Art | +1 ngày (5 background + transition VFX) |
| **Tổng** | **+4–5 ngày công** → bản đầy đủ ~5 tuần part-time, ~13 ngày full-time |

---

## 7. Trạng thái quyết định

Tất cả luật Duel đã chốt trong `Docs/QUYET_DINH.md` (Draw 10%, HP mang theo, status clear, Win = Stagger/Finisher riêng từng ải, 2 chiêu boss lặp vòng, phase 1 solo boss, cắt ally khỏi gameplay, 2 ending là lựa chọn dialogue).

Còn mở (không chặn code): deadline, số người, Windows/WebGL, 5 ải hay demo trước.
