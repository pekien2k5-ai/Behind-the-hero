# BÁO CÁO & ĐẶC TẢ THIẾT KẾ CÔNG VIỆC 3 (CV3) — DỰ ÁN "BEHIND THE HERO"

- **Họ và tên thành viên:** Nguyễn Xuân Lăng
- **Mã sinh viên:** 2351060459
- **Nhóm:** Nhóm 10 – Lớp 65CNTT
- **Phạm vi phụ trách (CV3):**
  1. Thiết kế chuỗi Spritesheets chuyển động (`Idle`, `Walk`, `Attack`, `Death`) và hiệu ứng kỹ năng VFX.
  2. Xây dựng bộ Tileset địa hình ô cờ (sàn đá, tường ngục, bẫy chông, rương kho báu, vực thẳm).
  3. Thiết kế bố cục và cơ chế câu đố của 5 ải màn chơi (Level & Puzzle Design).
  4. Thiết kế giao diện người dùng (UI/HUD, khung chân dung đối thoại, thanh máu/mana).

---

## 1. Thiết kế chuỗi Spritesheets chuyển động & Hiệu ứng kỹ năng VFX

### 1.1 Quy chuẩn kỹ thuật Spritesheet nhân vật & Boss
Toàn bộ 9 đơn vị trong game được chuẩn hóa về định dạng **Spritesheet 512×512 px (4 hàng × 4 cột, mỗi frame 128×128 px, nền trong suốt RGBA, FilterMode.Point)** lưu tại `Assets/_Project/Art/Spritesheets/` và `Assets/Resources/Art/Spritesheets/`:

| Hàng (Row) | Trạng thái (`UnitAnimState`) | Số Frame | Tốc độ (FPS) | Chế độ lặp | Mô tả diễn hoạt |
|---|---|---|---|---|---|
| **Hàng 0** | `Idle` (Đứng nghỉ) | 4 frame | 5.5 FPS | Loop | Nhịp thở chiến thuật, tà áo choàng và vũ khí ánh năng lượng theo hệ. |
| **Hàng 1** | `Walk` (Di chuyển) | 4 frame | 11.0 FPS | Loop khi lướt | Bước chân tiến qua các ô lưới A* (4 hướng), kèm bụi đá dưới chân. |
| **Hàng 2** | `Attack` (Tấn công / Kỹ năng) | 4 frame | 12.0 FPS | One-shot | Vung vũ khí / niệm phép kết hợp phát hiệu ứng VFX tại ô mục tiêu. |
| **Hàng 3** | `Death` (Gục ngã / Tan biến) | 4 frame | 7.5 FPS | One-shot (giữ frame 4) | Giật lùi trúng đòn (đỏ) → Khuỵu gối ám màu hệ → Ngã ngang → Tan thành tro bụi. |

### 1.2 Danh mục 9 Spritesheet Đơn vị (`UnitCatalog`)

| ID Đơn vị | Tên hiển thị | Vai trò | Kích thước ô | File Spritesheet | Điểm nhấn thị giác |
|---|---|---|---|---|---|
| `wanderer` | Kẻ Lang Thang | Nhân vật chính | 1×1 | `wanderer.png` | Áo choàng rách nâu-xám, đoản kiếm ánh tím thời gian (`#6B3BA7`). |
| `eosphoros` | Dũng Giả Eosphoros | Đồng minh (Truyện) | 1×1 | `eosphoros.png` | Giáp hoàng kim (`#E5C158`), khiên sư tử và thánh kiếm. |
| `imp` | Tiểu Quỷ (Imp) | Quái cận chiến | 1×1 | `imp.png` | Cánh dơi tím thẫm, mắt đỏ, đinh ba lửa. |
| `gargoyle` | Quái Thạch (Gargoyle) | Quái bay tầm xa | 1×1 | `gargoyle.png` | Tượng đá xám có cánh, bay vượt qua vực thẳm và bẫy địa hình. |
| `boss-ashen` | Đao Phủ Tro Tàn | Boss Ải 1 | 1×1 | `boss-ashen.png` | Giáp sắt rỉ sét, mũ sừng bò, đại phủ bổ nứt sàn đá. |
| `boss-morbida` | Vu Nữ Hoại Tử Morbida | Boss Ải 2 | 1×1 | `boss-morbida.png` | Áo choàng mục rữa, trượng sọ độc lục, triệu hồi bàn tay xác sống. |
| `boss-abyss` | Kỵ Sĩ Hư Không | Boss Ải 3 | 1×1 | `boss-abyss.png` | Giáp Hắc Diện Thạch, Khiên Mắt Quỷ phản đòn và thương hư không. |
| `boss-malkor` | Quỷ Vương Malkor | Boss Ải 4 | **2×2** | `boss-malkor.png` | Khổng lồ chiếm 4 ô bàn cờ, cự kiếm rực lửa hỏa ngục. |
| `boss-fallen-eosphoros` | Eosphoros Sa Ngã | Boss Cuối (Ải 5) | 1×1 | `boss-fallen-eosphoros.png` | Giáp vàng nhuốm bóng tối, phóng chùm tia thánh thuật và lướt chém. |

### 1.3 Hệ thống 12 Dải Hiệu ứng Kỹ năng VFX (`Assets/_Project/Art/VFX/`)
Mỗi hiệu ứng VFX được cắt thành dải **512×128 px (4 frame × 128×128 px)** nền alpha trong suốt, kích hoạt đồng bộ qua `VfxPopupManager` khi `EventBus` phát `SkillUsedEvent`, `CellCollapsedEvent` hoặc `RewindPerformedEvent`:

1. **`vfx_time_rewind.png`**: Đồng hồ cổ ngữ tím phát sáng, kim quay ngược rồi vỡ tung thành mảnh thời gian (dùng cho `Rewind`, `Temporal Step`, `Dodge`).
2. **`vfx_swift_slash.png`**: Vệt chém bán nguyệt tím-bạc tốc độ cao (dùng cho `Swift Slash` và đòn đánh thường).
3. **`vfx_necrotic_burst.png`**: Đám mây hoại tử đen-tím xoáy tụ rồi nổ thành vòng cổng không gian (`Necrotic Burst`, `Necrotic Ward`).
4. **`vfx_divine_barrier.png`**: Khiên thánh giá vàng kim bung tia sáng bảo hộ (`Blade Guard`, `Protect`, `Divine Barrier`).
5. **`vfx_holy_pillar.png`**: Cột sáng thiên phạt giáng thẳng từ bầu trời xuống vùng chữ thập (`Holy Pillar`).
6. **`vfx_holy_slash.png`**: Lưỡi kiếm ánh sáng hoàng kim quét vòng cung (`Holy Light Slash`, `Holy Strike`).
7. **`vfx_earth_crack.png`**: Mặt đất nổ tung, đá văng kèm vết nứt dung nham (`Earth Crack`, `Ashen Fury`).
8. **`vfx_toxic_miasma.png`**: Bọt khí độc xanh lục phát nổ kèm bàn tay xác sống trồi lên giữ chân (`Plague Storm`, `Grave Hands`).
9. **`vfx_void_spear.png`**: Quả cầu hắc ám phóng tia thương tím xuyên hàng và con mắt hư không (`Void Spear Beam`).
10. **`vfx_hellfire_wave.png`**: Sóng lửa đỏ cam cuộn trào theo nhát chém đại kiếm (`Hellfire Greatsword`, `Fireball`).
11. **`vfx_brimstone_meteor.png`**: Thiên thạch dung nham lao chéo từ trên cao và phát nổ thành miệng hố lửa (`Brimstone Meteor`).
12. **`vfx_dark_pillar.png`**: Cột lửa địa ngục tím thẫm giải phóng các linh hồn đầu lâu.

---

## 2. Xây dựng bộ Tileset địa hình ô cờ (`TerrainCatalog`)

Bộ Tileset gồm **25 tile 128×128 px** cùng bảng ghép `dungeon_tileset_atlas.png` tại `Assets/_Project/Art/Tilesets/`, ánh xạ trực tiếp vào 7 loại `TerrainId` trong `BTH.Core.Grid` và `BTH.Content.Terrain.TerrainCatalog`:

| Mã `TerrainId` | Tên địa hình | File Tile đại diện | Cờ (`TerrainFlags`) | Chi phí đi (`MOV`) | Sát thương / Lượt | Cơ chế chiến thuật & giải đố |
|---|---|---|---|---|---|---|
| `floor` | Sàn Đá Cổ | `floor_oakhaven.png`, `floor_moss.png`, `floor_grate.png`, `floor_brick.png` | `None` | 1 | 0 | Ô di chuyển tiêu chuẩn, thay đổi họa tiết theo chủ đề từng ải. |
| `wall` | Tường Ngục & Trụ Đá | `wall_stone.png`, `wall_cracked.png`, `pillar_intact.png` | `BlocksMove`, `BlocksLineOfSight`, `IsCover` | Chặn | 0 | Chặn cả đi bộ lẫn bay; dùng làm **vật che tầm nhìn (LOS)** để tránh đòn bắn xuyên của Boss. |
| `pit` | Vực Thẳm / Hố Chông | `pit_spikes.png`, `pit_stairs.png` | `BlocksMove` | Chặn bộ | 0 | Chặn đơn vị đi bộ nhưng không chặn LOS; có thể vượt qua bằng `Temporal Step` hoặc đơn vị bay (`Gargoyle`). |
| `cracked` | Sàn Đá Nứt | `floor_cracked_stone.png` | `None` (Đếm ngược `TurnsUntilCollapse`) | 1 | 0 | Đi qua bình thường nhưng đếm ngược $N$ lượt (hoặc khi trúng chấn động Boss) sẽ **sập thành Vực Thẳm (`Pit`)**. |
| `poison` | Bẫy Chông / Đầm Độc | `trap_spikes.png`, `poison_swamp.png` | `IsHazard` | 2 | **3 HP** (Xuyên giáp) | Làm chậm bước chân (tốn 2 MOV) và rút 3 HP cuối mỗi lượt nếu kết thúc lượt trên ô. |
| `lava` | Bẫy Dung Nham Hỏa Ngục | `lava_pool.png` | `IsHazard` | 2 | **4 HP** (Xuyên giáp) | Vùng lửa địa ngục ở Ải 4; ép người chơi phải giữ vị trí trên các dải đá an toàn. |
| `chest` | Rương Kho Báu / Di Vật | `chest_gold.png`, `chest_iron.png` | `IsInteractive` | 1 | 0 | Điểm mốc chiến thuật chứa mảnh ký ức và di vật trong thánh điện. |

---

## 3. Thiết kế bố cục và cơ chế câu đố của 5 ải màn chơi (Level & Puzzle Design)

Mỗi màn chơi được chia làm **2 Pha tách biệt**:
- **Phase 1 (Giải đố vị trí trên bàn cờ ô lưới):** Đầu mỗi lượt, Boss công bố ý định tấn công (**Telegraph** — tô đỏ các ô `DangerCells`). Người chơi có **2 AP** (1 Di chuyển + 1 Hành động) để vừa tận dụng địa hình né vùng đỏ, vừa tiếp cận kéo máu Boss xuống **dưới 70% HP**.
- **Phase 2 (Duel Mode — Đấu chiêu đối kháng 1v1):** Khi máu Boss `< 70%`, bàn cờ biến mất, chuyển cảnh sang đấu trường ngang. Mỗi vòng Boss tung tuyệt kỹ theo chu kỳ, người chơi giải đố khắc chế bằng cách chọn **1 trong 3 thẻ chiêu Clash** (có huy hiệu dự báo Thắng/Hòa/Thua) để gây hiệu ứng đặc biệt và tự động tung chuỗi đòn kết liễu.

### Bảng tổng hợp 5 Ải màn chơi (`StageCatalog` & `DuelCatalog`)

| Ải | Tên màn & Kích thước | Boss鎮守 (HP / ATK) | Bố cục địa hình & Cơ chế câu đố Phase 1 (Bàn cờ) | Cơ chế giải đố Phase 2 (Duel Mode) |
|---|---|---|---|---|
| **Ải 1** | **Tàn Tích Oakhaven**<br>Lưới `8×8`<br>Giới hạn: 22 lượt | **Đao Phủ Tro Tàn**<br>HP: 20 \| ATK: 5<br>Size: `1×1` | • **Bố cục:** 4 trụ đá ở 4 góc (`(1,1), (6,1), (1,5), (6,5)`), cầu đá nứt ở trung tâm (`(3,4), (4,4)` sập sau 3 lượt), 2 vực thẳm ở biên và bẫy chông.<br>• **Câu đố:** Đao Phủ dùng `Earth Crack` đập nứt sàn theo hình chữ thập (`Cross`). Người chơi không được đứng trên ô `Cracked` quá hạn và phải lách sang ô chéo ngoài chữ thập trước khi áp sát dùng `Swift Slash`. | • **Chiêu Boss:** `Tro Tàn Cuồng Nộ` (Power 15, Physical) ↔ `Bổ Rìu` (Power 10).<br>• **Lời giải Clash:** Chọn `Blade Guard` (15+3=18) hoặc `Swift Slash` (14+3=17) để **Thắng**, gây **Stagger (×2 sát thương)** + 1 đòn kết liễu tự động. |
| **Ải 2** | **Hầm Mộ Độc Khí**<br>Lưới `9×9`<br>Giới hạn: 24 lượt | **Vu Nữ Morbida**<br>HP: 18 \| ATK: 4<br>Size: `1×1` | • **Bố cục:** 5 hồ độc (`Poison` -3 HP/lượt, tốn 2 MOV) chia cắt bản đồ thành các dải đá khô hẹp, kèm 4 bức tường cổ chắn LOS.<br>• **Câu đố:** Morbida niệm `Grave Hands` tạo vùng độc mới và trói chân (`Root`). Người chơi phải đứng sau tường chắn tầm nhìn (LOS) hoặc di chuyển qua các bậc đá khô để không bị kẹt trong đầm độc khi bị `Root`. | • **Chiêu Boss:** `Bão Khí Độc Diệt Thế` (Power 14, Necrotic) ↔ `Độc Vũ` (Power 9).<br>• **Lời giải Clash:** Dùng `Necrotic Ward` (13+3=16) hoặc `Necrotic Burst` (13+3=16) để **phản ngược độc khí (3 HP/lượt × 2 vòng)** lên chính Vu Nữ. |
| **Ải 3** | **Hành Lang Hư Không**<br>Lưới `9×9`<br>Giới hạn: 24 lượt | **Kỵ Sĩ Hư Không**<br>HP: 25 \| ATK: 6<br>Giáp: 1 \| Size: `1×1` | • **Bố cục:** Cây cầu đá bắc qua 8 ô Vực Thẳm (`Pit`) hai bên sườn, với 4 trụ đá chắn giữa cầu.<br>• **Câu đố:** Kỵ Sĩ có giáp dày và phóng `Tia Thương Hư Không` (`Void Spear`) xuyên thẳng hàng 4 ô xuyên giáp. Người chơi phải nấp sau 4 trụ đá để cắt đường đâm, sau đó dùng `Temporal Step` dịch chuyển qua vực thẳm áp sát sườn Boss. | • **Chiêu Boss:** `Tia Thương Hư Không` (Power 16, Piercing) ↔ `Thương Đâm` (Power 11).<br>• **Lời giải Clash:** Dùng `Temporal Step` (16+3=19) hoặc `Swift Slash` (14+3=17) để lách qua Khiên Mắt Quỷ, gắn trạng thái **Flanked (×1.5 sát thương)**. |
| **Ải 4** | **Đấu Trường Hỏa Ngục**<br>Lưới `10×10`<br>Giới hạn: 25 lượt | **Quỷ Vương Malkor**<br>HP: 45 \| ATK: 8<br>Giáp: 1 \| **Size: `2×2`** | • **Bố cục:** 8 hồ Dung Nham (`Lava` -4 HP/lượt) ở 4 góc đấu trường, 2 vực thẳm biên và sàn đá nứt quanh khu vực trung tâm.<br>• **Câu đố Footprint 2×2:** Malkor chiếm 4 ô (`(5,4)..(6,5)`), mọi ô kề cạnh footprint 2×2 đều có thể đánh trúng Boss nhưng cũng nằm trong tầm quét `Hellfire` và thiên thạch `Brimstone` (làm sập sàn sau 1 lượt). Người chơi phải liên tục đảo vị trí quanh 8 ô tiếp giáp của footprint 2×2. | • **Chiêu Boss:** `Cự Kiếm Hỏa Ngục` (Power 18, Fire) ↔ `Hỏa Cầu` (Power 12).<br>• **Lời giải Clash:** Dùng `Temporal Step` (16+3=19) hoặc `Blade Guard` (15+4=19) để làm choáng Malkor và kích hoạt **chuỗi 3 đòn kết liễu liên hoàn (`FinisherCount = 3`)**. |
| **Ải 5** | **Mật Thất Thánh Điện**<br>Lưới `8×8`<br>Giới hạn: 25 lượt | **Eosphoros Sa Ngã**<br>HP: 30 \| ATK: 6<br>Giáp: 1 \| Size: `1×1` | • **Bố cục:** Kiến trúc thánh đường đối xứng hoàn hảo với 4 trụ thánh quang, 2 vực thẳm ký ức và 2 rương di vật ở phía bắc.<br>• **Câu đố:** Eosphoros kết hợp cả `Holy Pillar` (chữ thập ở lượt chẵn) và `Thánh Quang Trảm` (hàng thẳng ở lượt lẻ). Người chơi phải đổi nhịp đứng chéo ở lượt chẵn và đứng lệch hàng ở lượt lẻ, trước khi bước vào trận Duel định mệnh và chọn 1 trong **2 Kết Cục (Ending A / Ending B)**. | • **Chiêu Boss:** `Thánh Quang Trảm` (Power 18, Holy) ↔ `Thánh Kích` (Power 12).<br>• **Lời giải Clash:** Dùng `Blade Guard` (15+4=19) hoặc `Swift Slash` (14+5=19) để phá kiếm ánh sáng, mở ra lựa chọn thoại kết thúc game. |

---

## 4. Thiết kế Giao diện người dùng (UI / HUD / Đối thoại / Duel Screen)

Hệ thống giao diện được thiết kế theo đúng bản mẫu `mockup_gameplay_grid.jpg` và bảng màu Dark Fantasy đã chốt (`#E5C158` Vàng Kim, `#2C4270` Xanh Lam Đậm, `#6B3BA7` Tím Thời Gian, `#8B1E1E` Đỏ Máu, `#3A3E45` Xám Đá):

### 4.1 Cột HUD Chiến Thuật Bên Trái (`DrawLeftHudPanel`)
- **Khung Chân Dung & Thông số Nhân vật (`HOODED WANDERER`):**
  - Hiển thị ảnh chân dung `portrait_wanderer.png` trong khung viền cổ điển.
  - **Thanh Máu (HP Bar & Diamond Pips):** Hiển thị trực quan lượng máu hiện tại `12 / 12 ♦` trên nền đỏ thẫm `#8B1E1E`.
  - **Thanh Năng Lượng Lời Nguyền (MP Bar):** Hiển thị `MP: 3 / 3 ❖ (Temporal)` màu lam ngọc `#2C4270`.
  - **Điểm Hành Động (AP Counter):** Hiển thị `AP: 2 / 2 ◆ ◆`, tự động trừ khi người chơi thực hiện Move hoặc Action.
- **Khung Trạng Thái & Lượt (`STATUS & TURN`):**
  - Hiển thị lượt hiện tại trên giới hạn lượt của ải (`Turn: 1 / 22`), pha hiện tại (`Player / Enemy / Duel`), danh sách hiệu ứng (`Guard`, `Stagger`, `Poison`, `Root`, `Flanked`) và bộ đếm `RunKnowledge` (Số lần chết / Số tuyệt kỹ Boss đã ghi nhớ qua các lần tua ngược).
- **Bảng Lệnh Chiến Thuật (`ACTIONS`):**
  - Các nút lệnh kèm phím tắt rõ ràng, tự động sáng màu tím `#6B3BA7` khi đang chọn:
    - `Move (Di chuyển 2 ô) [G]`
    - `Slash (Đánh thường 4 DMG) [S]`
    - `Trảm Tốc (5 DMG) [1]`
    - `Bùng Nổ Hoại Tử (AoE Chữ thập) [2]`
    - `Bước Nhảy Thời Gian (Dịch chuyển) [3]`
    - `Guard (Giảm 80% sát thương) [D]`
    - `Rewind (Tua ngược thời gian) [R]`
    - `End Turn (Kết thúc lượt) [Space]`
- **Chú giải Tileset thời gian thực:** Khi rê chuột lên bất kỳ ô cờ nào, HUD tự động hiển thị tên địa hình, số lượt sập còn lại và gợi ý chiến thuật của ô đó.

### 4.2 Thanh Điều Khiển Phía Trên & Thanh Máu Boss (`DrawTopBar`)
- **Thanh chọn nhanh 5 Ải (`Ải 1` → `Ải 5`):** Giúp kiểm thử và trình diễn ngay bất kỳ màn chơi nào.
- **Chân dung & Thanh Máu Boss:** Hiển thị Portrait riêng của từng Boss, thanh máu lớn kèm **vạch vàng đánh dấu mốc 70% HP** (ngưỡng kích hoạt Phase 2 Duel Mode).
- **Banner Gợi Ý Câu Đố & Telegraph:** Giải thích cơ chế giải đố của ải hiện tại và thông báo mọi sự kiện chiến đấu theo thời gian thực.
- **Cụm nút `↺ REWIND`, `⟳ RESET` và `[TAB] Trưng Bày Art CV3`:** Đặt ở góc phải trên đúng bố cục bản mẫu.

### 4.3 Khung Chân Dung Đối Thoại (`DrawDialogueOverlay`)
- Xuất hiện ở đầu mỗi màn chơi và cuối màn 5.
- Gồm khung chân dung lớn (`136×136 px`) viền vàng kim `#E5C158` của người đang nói, tên nhân vật, hộp văn bản cốt truyện và các nút lựa chọn nhánh kết thúc (**Ending A: Buông gươm giải thoát vòng lặp** vs **Ending B: Kết liễu Eosphoros**).

### 4.4 Giao diện Phase 2 Duel Mode (`DrawDuelScreen`)
- Toàn màn hình trên 5 bức tranh nền 16:9 riêng biệt (`bg_oakhaven_ruins`, `bg_catacomb_toxic`, `bg_void_arena`, `bg_fire_arena`, `bg_mausoleum`).
- Hiển thị nhân vật chính bên trái và Boss bên phải (chạy hoạt ảnh `Idle`/`Attack`/`Death` + hiệu ứng VFX khi va chạm chiêu).
- Phía dưới là **3 Thẻ Chiêu Clash** hiển thị công thức `Uy lực gốc + Khắc chế = Tổng uy lực` kèm huy hiệu màu:
  - **Xanh lá:** `DỰ BÁO: THẮNG (+N ĐÒN KẾT LIỄU)`
  - **Vàng:** `DỰ BÁO: HÒA (MỖI BÊN -10%)`
  - **Đỏ:** `DỰ BÁO: THUA (ĂN ĐÒN BOSS)`

---

## 5. Khung Tích Hợp Cho Toàn Bộ 4 Thành Viên Nhóm 10 (CV1, CV2, CV3, CV4)

Đối chiếu với tài liệu thiết kế `65CNTT_Nhom10_Tai_lieu_thiet_ke.docx`, mã nguồn đã được tổ chức thành 4 khung module rõ ràng tương ứng với bảng phân công công việc của cả 4 thành viên (có thể xem trực tiếp trong game bằng phím **`[TAB] Trưng Bày 4 CV`**):

| STT | Thành viên | Mã SV | Khung Code & File Triển Khai |
|---|---|---|---|
| **CV1** | **Ngô Quý Kiên** (Nhóm trưởng) | `2351060457` | • `Assets/_Project/Content/GameMatrixCatalog.cs`: Quản lý High Concept, `CreateBalancedRules()` (AP=2, MOV=2, TurnLimit=25, Phase2=70%, Stagger ×2, Guard ×0.2, Flanked ×1.5), Bảng Ma trận cân bằng 9 Đơn vị (`GetUnitMatrix()`), Ma trận Hệ thuộc tính (`ElementMatrix`).<br>• `Assets/_Project/Core/Run/GameSession.cs`: Core Loop, Die & Rewind, Ràng buộc lượt đi. |
| **CV2** | **Nguyễn Tuấn Kiệt** | `2351060458` | • `Assets/_Project/Content/TacticalNavigator.cs`: Khung tìm đường A* và vẽ đường dẫn thông minh (`PreviewPlayerPath`, `GetPlayerReachableSet`), Tua ngược trạng thái (`TryRewind`), Truy vấn vùng nguy hiểm AI (`GetTelegraphDangerCells`).<br>• `Assets/_Project/Core/Grid/GridMap.cs`, `AStarPathfinder.cs`, `TurnManager.cs`, `SnapshotService.cs`, `Assets/_Project/Content/Brains/BrainCatalog.cs`. |
| **CV3** | **Nguyễn Xuân Lăng** | `2351060459` | • `Assets/_Project/Content/Cv3DesignCatalog.cs`: Đặc tả 9 Spritesheets 4 trạng thái (`Idle, Walk, Attack, Death`), 12 dải VFX kỹ năng, 8 loại địa hình Tileset tương tác, Bảng màu Dark Fantasy (`#E5C158, #2C4270, #6B3BA7, #8B1E1E, #3A3E45`).<br>• `Assets/_Project/Content/Terrain/TerrainCatalog.cs`: 8 địa hình ô cờ + tương tác bước vào Rương kho báu (`+4 HP` tức thời) và Ô gạch nứt (`ArmCollapse(1)`).<br>• `Assets/_Project/Content/Stages/StageCatalog.cs` & `DuelCatalog.cs`: Bố cục & cơ chế câu đố 5 ải màn chơi + Phase 2 Duel.<br>• `Assets/_Project/Game/ArtCatalog.cs`, `UnitSpriteAnimator.cs`, `VfxPopupManager.cs`, `GameBootstrap.cs`, `Assets/_Project/UI/HudSpecPresenter.cs`: Toàn bộ hiển thị Spritesheet, Tileset, VFX, UI/HUD, Khung chân dung đối thoại, Thanh máu/mana và Bảng cài đặt (`GameOptionsModel`). |
| **CV4** | **Phạm Thị Huyền Linh** | `2351060460` | • `Assets/_Project/Content/Dialogues/DialogueCatalog.cs`: Cốt truyện chi tiết 3 Hồi (`GetThreeActsLore()`), kịch bản hội thoại nguyên văn theo GDD §4.4 cho cả 5 ải và 2 nhánh kết thúc (**Ending A: Lời nói dối vĩnh hằng** & **Ending B: Tội đồ ẩn cư**).<br>• `Assets/_Project/Game/AudioCatalog.cs`: Khung âm thanh `IAudioService`, 2 bản BGM (`forest_theme`, `boss_theme`) và 14 hiệu ứng SFX (`attack1..3`, `boss_roar`, `player_death`, `transform_dark/light`...). |

---

## 6. Kiểm Thử & Debug Trên VS Code (81/81 Unit Test Xanh)

Bộ kiểm thử tự động trong `Assets/Tests/ContentTests.cs` đã được cấu hình sẵn trong `.vscode/launch.json` và `.vscode/tasks.json` để chạy và debug trực tiếp trên VS Code:
- **`Cv3_AllArtAndAudioAssets_PhysicallyExistInUnityProject`**: Kiểm tra vật lý từng file PNG của 9 Spritesheets (`Idle/Walk/Attack/Death`), 12 dải VFX, bộ Tileset 8 loại địa hình + Atlas, 7 Portraits đối thoại và 5 Background Duel trong `Assets/Resources/Art/`.
- **`Cv3_TilesetBehaviors_ChestHealsPlayer_AndCrackedTileArmsCollapse`**: Kiểm tra cơ chế bước vào ô Rương Kho Báu hồi ngay `+4 HP` và bước vào ô Gạch Nứt kích hoạt đếm ngược sập sàn đúng GDD §3.2.2.
- **`Cv3_DesignCatalog_SpritesheetsVfxTilesetsAndPalette_AreComplete`**: Kiểm tra đủ 4 trạng thái chuyển động, 12 VFX và 5 mã màu chuẩn GDD §6.1.
- **`Stage1_CanBePlayedFromBoardToDuelVictory_WithoutUnity`**: Giả lập chơi trọn vẹn từ bàn cờ Phase 1 → kéo máu Boss `< 70%` → vào Duel Phase 2 → chọn thẻ chiêu Thắng Clash → Kết liễu tự động → `Victory`.

