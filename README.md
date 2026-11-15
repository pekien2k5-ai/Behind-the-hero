# Behind the Hero

Game chiến thuật theo lượt (turn-based tactics) 1v1, làm bằng Unity 2D — đồ án Nhóm 10, lớp 65CNTT.

Người chơi điều khiển **Wanderer** qua 5 ải, mỗi ải đối đầu một boss với bộ chiêu riêng. Combat dùng lưới ô vuông, 2 AP mỗi lượt (1 di chuyển + 1 hành động), hệ thống **Clash** (chọn chiêu để đấu với chiêu boss) và cơ chế **rewind** khi thua.

## Trạng thái

| Phần | Trạng thái |
|---|---|
| **Core** — luật chơi (C# thuần, không dùng UnityEngine) | ✅ 163 file, **56 unit test xanh** (gồm Duel mode) |
| Content — chiêu, quái, màn chơi, dữ liệu | ⏳ chưa làm |
| Unity — hiển thị, input, animation, âm thanh | ⏳ chưa làm |
| UI — HUD, menu, Clash/Duel UI | ⏳ chưa làm |

## Cấu trúc

```
Assets/
  _Project/
    Core/          # BTH.Core — luật chơi, C# thuần, không tham chiếu UnityEngine
      Grid/        #   bàn cờ, địa hình, tầm nhìn, hình dạng vùng
      Units/       #   quân, faction, footprint, registry, A*
      Turn/        #   pha lượt, AP, thứ tự hành động
      Actions/     #   request, validator, executor
      Combat/      #   damage, effect, status, element, skill, clash
      Ai/          #   enemy intent, telegraph, boss phase
      Run/         #   rules, stage, snapshot, event bus, session, dialogue
  Tests/           # BTH.Core.Tests — 56 test NUnit cho Core + Duel
Tools/
  CoreTests/       # project dotnet để chạy test nhanh ngoài Unity
Docs/
  KIEN_TRUC_UNITY_BEHIND_THE_HERO.md   # kiến trúc 4 lớp
  CORE_API_TONG_HOP.md                 # bảng tra class/hàm cho đồng đội
```

Chi tiết kiến trúc: [Docs/KIEN_TRUC_UNITY_BEHIND_THE_HERO.md](Docs/KIEN_TRUC_UNITY_BEHIND_THE_HERO.md).
Tra nhanh API: [Docs/CORE_API_TONG_HOP.md](Docs/CORE_API_TONG_HOP.md).

## Chạy test

**Cách 1 — ngoài Unity (nhanh, ~120 ms):**

```powershell
dotnet test Tools/CoreTests/CoreTests.csproj
```

Không có .NET SDK? Dùng bản Unity đang cài:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe" test Tools/CoreTests/CoreTests.csproj
```

**Cách 2 — trong Unity:** mở project bằng Unity **6000.6.7f1**, vào `Window > General > Test Runner > EditMode > Run All`.

## Nguyên tắc

- `BTH.Core` **không** được `using UnityEngine` (`noEngineReferences: true` trong asmdef).
- Mọi thay đổi state đi qua `ActionRequest → IActionValidator → IGameActionExecutor`.
- Tất định: RNG có seed, sort luôn có tie-break, snapshot khôi phục được từng trường.
- Thêm chiêu / quái / màn chơi **không sửa Core** — chỉ thêm dữ liệu và behaviour ở lớp Content.
- UI nghe event để vẽ, không tự tính luật.
