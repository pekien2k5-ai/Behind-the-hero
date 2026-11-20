# hlinh — CV4: Cốt truyện, hội thoại và âm thanh

Phạm Thị Huyền Linh · Nhóm 10 · Behind the Hero

Căn cứ: mục phân công CV4, §4.1–4.4 và §6.3 của báo cáo thiết kế gửi thầy. Vị trí lựa chọn A/B theo [quyết định của nhóm](QUYET_DINH.md#3-dữ-liệu--nội-dung-đã-chốt). Luồng hội thoại dùng các hợp đồng [DialogueNode, DialogueGraph và DialogueRunner](CORE_API_TONG_HOP.md#117-dialogue).

## Mạch truyện trong game

| Hồi | Vị trí | Người chơi đọc được |
|---|---|---|
| I — Oakhaven và lời nguyền | Đầu ải 1 | Thị trấn bị san phẳng, Kẻ Lang Thang thành người tị nạn và mắc lời nguyền. Eosphoros dùng nước thánh xoa dịu vết thương và trao kiếm. Đoạn thoại trao kiếm giữ nguyên lời trong §4.4. |
| II — Thảo phạt và đố kỵ | Đầu ải 2, đầu ải 3, cuối ải 4 | Eosphoros dẫn đầu dù đầy thương tích. Kẻ Lang Thang giữ ký ức qua các vòng lặp và dần vượt qua ân nhân. Sau trận Malkor, lời dẫn xác định ai đã kết liễu Quỷ Vương. |
| III — Khải hoàn và phản bội | Đầu và cuối ải 5 | Dân chúng tung hô Eosphoros. Vì sợ mất danh tiếng, anh dụ người đồng đội xuống mật thất. Cuộc đối thoại cao trào có đủ lời theo §4.4. Người chơi chọn kết thúc sau khi thắng Duel ở ải 5. |

**Kẻ Lang Thang** sống sót ở Oakhaven và trôi dạt về khu ổ chuột ven kinh đô. Cơ thể hắn hoại tử; mỗi lần chết, thời gian đưa hắn trở lại và ký ức vẫn còn. Hắn học cách sống sót qua từng thất bại và mang ơn người đã cứu mình. Khi bị Eosphoros đòi mạng, hắn phải chọn giữa hy vọng cứu phần lương tri của ân nhân và tự bảo vệ mình dù sẽ vĩnh viễn bị mang tiếng phản đồ.

**Eosphoros** là Dũng giả của Thần điện, được xem là biểu tượng công lý. Anh từng cứu một người vô danh và tin người đó xứng đáng có cơ hội làm anh hùng. Khi người mình cứu dần vượt qua mình, lòng tự tôn biến thành đố kỵ rồi thành nỗi sợ bị hạ bệ. Việc nhận công sau khi Malkor chết và xuống mật thất cùng người đồng đội cho thấy anh đặt danh tiếng lên trên điều mình từng tin.

- **A — Lời nói dối vĩnh hằng:** Kẻ Lang Thang buông gươm, Eosphoros đâm chết hắn, lời nguyền chấm dứt. Danh tiếng của Eosphoros được giữ bằng lời nói dối.
- **B — Tội đồ ẩn cư:** Kẻ Lang Thang hạ Eosphoros, bỏ lại vinh quang và sống những ngày cuối trong cô độc. Độc thoại cuối giữ nguyên văn §4.4.
- Mỗi nhánh đặt kết quả ở trang cuối. Người chơi đọc hết rồi mới thấy màn kết thúc tương ứng.

## Nhạc nền và hiệu ứng

Hai bài trong §6.3 được dùng lại từ bản nền l4ngx.

| Phân cảnh | BGM và cách phát |
|---|---|
| Mở đầu, bàn cờ và thoại sau trận | forest_theme.ogg, cao độ gốc, lặp. |
| Chuyển Duel | Chồng dần sang boss_theme.ogg trong 1,25 giây, cao độ 0,92 lần bản gốc. Tên clip lấy từ dữ liệu Duel. |
| Rewind hoặc chọn lại ải | Khôi phục BGM theo pha thực tế. Nhạc Duel tiếp tục nếu mốc vừa khôi phục vẫn là Duel. |
| Hai kết thúc ở ải 5 | Dùng forest_theme.ogg khi mở đoạn kết để lời thoại giữ không khí trầm như phần mở đầu. |

SFX theo sự kiện: chém thường dùng attack1, kỹ năng dùng attack2, phản công Duel dùng attack3. Đỡ đòn dùng transform_light, tua ngược dùng transform_dark. Chuyển Duel phát boss_roar và transform_dark. Hai clip player_hurt và enemy_hurt phát khi mục tiêu mất máu nhưng còn sống. Gục ngã dùng clip riêng của từng phía. Các âm lượng Master, BGM và SFX vẫn lấy từ bảng cài đặt.

## Kiểm thử

Bộ Core/Content chạy ngoài Unity đạt **86/86**. Các ca CV4 xác nhận thứ tự năm đoạn kể, hai nhánh kết thúc, nguyên văn độc thoại B và các file BGM/SFX cần dùng. BTH.Content và BTH.Game cũng biên dịch thành công với bộ tham chiếu của Unity. Trong Play Mode đã thấy các trang dẫn truyện mới ở ải 1.

| Ca thử trong Unity | Kết quả cần quan sát |
|---|---|
| Mở ải 1 và đọc tới hết intro | Thấy lời dẫn Oakhaven, rồi Eosphoros trao kiếm. Bàn cờ nhận lệnh sau khi đóng thoại. |
| Thắng ải 4 rồi qua ải 5 | Thấy lời dẫn người hạ Malkor rồi đến lễ khải hoàn và cuộc đối đầu trong mật thất. |
| Thắng ải 5, thử A rồi chơi lại để thử B | Hai lựa chọn mở ngay sau trận, đủ trang thoại và đúng màn kết. |
| Vào Duel, Rewind, vào Duel lại | Nhạc chuyển dần theo trạng thái trận, tiếng gầm và hiệu ứng chuyển cảnh phát đúng lúc. |
| Thử Master, BGM và SFX ở mức 0 và mức thường dùng | Mỗi thanh âm lượng tác động đúng kênh; BGM lặp đều và không lấn hiệu ứng chính. |
