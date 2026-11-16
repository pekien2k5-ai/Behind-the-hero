# QUYẾT ĐỊNH THIẾT KẾ — chốt ngày 2026-11-15

> Ghi lại các quyết định đã chốt với chủ dự án (K). Mục nào còn mở sẽ ghi rõ.
> Liên quan: `THIET_KE_PHASE2_DUEL.md`, `KE_HOACH_HOAN_THANH_GAME.md`.

## 1. Phase 2 = Duel mode (đã chốt)

- Phase 2 **không còn bàn cờ**: bỏ lưới/AP/di chuyển/End Turn; player trái, boss phải, nền chỉ là background.
- Kích hoạt khi HP boss **< 70%**; có **chuyển cảnh** (~1,2 s) trước khi vào Duel.
- Trong Duel chỉ có: chọn 1 trong 3 chiêu Clash mỗi vòng; thắng thì **tự đánh kết liễu và tự sang vòng**, không cần bấm Attack (chốt m02424 thay cho nút Kết liễu thủ công cũ).

## 2. Luật Duel (đã chốt)

| # | Luật | Chốt |
|---|---|---|
| 1 | Đòn phản công khi thắng | 1 đòn = ATK người chơi, nhân theo status của boss (Stagger ×2, Flanked ×1.5) |
| 2 | Stagger | Kéo dài 1 round, kết thúc ngay sau đòn kết liễu tự động |
| 3 | Hiệu ứng thắng riêng từng boss | Ải 1: Stagger. Ải 2: Poison 3 HP/lượt ×2 (không Stagger). Ải 3: Flanked. Ải 4: Stagger + 3 đòn kết liễu. Ải 5: Stagger |
| 4 | Hoà | **Mỗi bên mất 10% uy lực của mình** (K chốt: "nhận 10% sát thương thôi") |
| 5 | Thua | Player mất đúng `BossPower` HP, xuyên giáp; **không** gắn thêm status |
| 6 | HP khi vào Duel | Mang theo, không hồi |
| 7 | Status khi vào Duel | Clear cả 2 bên |
| 8 | Turn limit / AP / End Turn | Không áp dụng trong Duel |
| 9 | Chu kỳ chiêu boss | 2 chiêu lặp vòng: tuyệt kỹ → đòn thường → tuyệt kỹ… (đòn thường power thấp hơn) |

## 3. Dữ liệu & nội dung (đã chốt)

| # | Mục | Chốt |
|---|---|---|
| 10 | 3 lựa chọn mỗi ải | Ải 1: Blade Guard, Swift Slash, Necrotic Burst. Ải 2: Necrotic Ward, Necrotic Burst, Protect. Ải 3: Temporal Step, Swift Slash, Protect. Ải 4: Temporal Step, Blade Guard, Swift Slash. Ải 5: Blade Guard, Swift Slash, Necrotic Burst |
| 11 | Bonus khắc chế | Default +3; Ải 4 Dodge/Temporal +5; Ải 5 Protect +4 |
| 12 | Protect vs Blade Guard | Là cùng một chiêu; id `blade-guard`, hiển thị "Blade Guard". Guard thường trên bàn cờ vẫn là `ActionKind.Guard` |
| 13 | Phase 1 màn boss | Solo 1v1, **chỉ boss**, không Imp/Gargoyle |
| 14 | Transition | 1 prefab dùng chung, 1,2 s, không cho skip lần đầu; 5 background riêng |
| 20 | 2 ending | Lựa chọn dialogue ở cuối ải 5 (A: buông gươm; B: quyết chiến), không phụ thuộc chỉ số |

## 4. Ngoài Duel

- Eosphoros đồng minh: **cắt khỏi gameplay** (docx quy định solo 1v1); chỉ xuất hiện trong cutscene/dialogue ải 5.
- Rewind: về `StageStart` / `TurnStart` / `ClashStart` (đầu Duel); `RunKnowledge` giữ qua rewind.
- Không save giữa run (ADR-05).
- MP trên HUD là trang trí.

## 5. Còn mở (chưa chốt)

1. Deadline nộp.
2. Số người tham gia; ai làm art/background Duel.
3. Chỉ Windows hay cần WebGL.
4. Làm đủ 5 ải + 2 ending, hay demo 1–3 ải trước.
