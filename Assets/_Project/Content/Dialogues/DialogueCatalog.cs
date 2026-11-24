using System.Collections.Generic;
using BTH.Content.Units;
using BTH.Core.Run;

namespace BTH.Content.Dialogues
{
    /// <summary>
    /// Kịch bản Cốt truyện 3 Hồi, Tiểu sử Nhân vật và Đồ thị Hội thoại / 2 Kết thúc
    /// (Khung CV4 - Phạm Thị Huyền Linh, đối chiếu nguyên văn GDD §4.1 - §4.4).
    /// </summary>
    public static class DialogueCatalog
    {
        public static IReadOnlyList<(string Title, string Content)> GetThreeActsLore() => new List<(string, string)>
        {
            ("Hồi I: Thảm họa Oakhaven & Lời nguyền Khởi sinh",
             "Năm thứ 12 triều đại Thiên hoàng thứ XIV, thị trấn Oakhaven bị san phẳng bởi quân đoàn Quỷ Vương. Kẻ lang thang sống sót nhưng mang lời nguyền hoại tử thể xác: mỗi khi ngã xuống, thời gian lập tức quay ngược lại. Giữa lúc tuyệt vọng, Dũng giả Eosphoros đã chìa tay cứu vớt và trao cho hắn thanh đoản kiếm."),
            ("Hồi II: Chặng đường Thảo phạt & Mầm mống Đố kỵ",
             "Qua hàng trăm lần bị quái vật xé xác và tua ngược thời gian, Kẻ lang thang giải mã mọi quỹ đạo đòn đánh, bộc phát kỹ năng thực chiến làm lu mờ cả ánh hào quang của Thần điện. Cảm giác bất lực khi bị một kẻ vô danh vượt mặt đã gieo mầm đố kỵ ăn mòn tâm trí Eosphoros."),
            ("Hồi III: Khúc khải hoàn đẫm máu & Bi kịch dưới Mật thất",
             "Sau khi Quỷ Vương Malkor gục ngã, Eosphoros độc chiếm toàn bộ vinh quang rồi lừa người đồng đội xuống mật thất dưới đáy Thánh điện để diệt khẩu nhằm bảo vệ bức tượng đài hoàn mỹ. Người chơi đối diện với ngã rẽ định mệnh: Kết thúc A (Lời nói dối vĩnh hằng) hoặc Kết thúc B (Tội đồ ẩn cư).")
        };

        public static DialogueGraph GetIntro(string stageId)
        {
            switch (stageId)
            {
                case "stage-1":
                    return new DialogueGraph("s1_1")
                        .Add(new DialogueNode("s1_1", UnitCatalog.EosphorosId,
                            "Hãy đứng lên. Thế gian này không ruồng bỏ ngươi chừng nào ngọn lửa trong mắt ngươi còn cháy. Cầm lấy thanh kiếm này. Không phải chỉ những kẻ được thần linh lựa chọn, mới xứng đáng được mang danh anh hùng!", "s1_2"))
                        .Add(new DialogueNode("s1_2", UnitCatalog.WandererId,
                            "Tàn tích Oakhaven... Đao Phủ Tro Tàn đang chặn đường. Ta phải tận dụng các trụ đá, tránh ô gạch nứt vỡ và mở rương thánh dược!", "s1_3"))
                        .Add(new DialogueNode("s1_3", UnitCatalog.AshenExecutionerId,
                            "Kẻ mang dấu ấn hoại tử! Đại rìu của ta sẽ bổ nứt từng ô đá dưới chân ngươi!"));

                case "stage-2":
                    return new DialogueGraph("s2_1")
                        .Add(new DialogueNode("s2_1", UnitCatalog.PlagueWitchId,
                            "Khí độc trong Rừng Bệnh Dịch này được chưng cất từ những kẻ đã bị Dũng Giả bỏ rơi...", "s2_2"))
                        .Add(new DialogueNode("s2_2", UnitCatalog.WandererId,
                            "Ta phải di chuyển trên các phiến đá khô, né bàn tay xác sống Grave Hands và dùng Necrotic Burst phản ngược bão độc!"));

                case "stage-3":
                    return new DialogueGraph("s3_1")
                        .Add(new DialogueNode("s3_1", UnitCatalog.AbyssKnightId,
                            "Khiên Mắt Quỷ phản chấn mọi đòn trực diện. Ngươi không thể vượt qua Cổng Thành Vực Sâu này!", "s3_2"))
                        .Add(new DialogueNode("s3_2", UnitCatalog.WandererId,
                            "Những trụ đá obsidian sẽ chắn tia thương xuyên thấu. Bước Nhảy Thời Gian sẽ đưa ta lách vào sườn hở của hắn!"));

                case "stage-4":
                    return new DialogueGraph("s4_1")
                        .Add(new DialogueNode("s4_1", UnitCatalog.DemonKingMalkorId,
                            "Lại là chiếc bóng đi sau Eosphoros! Cự Kiếm Hỏa Ngục và Thiên Thạch Dung Nham của ta sẽ thiêu rụi toàn bộ vương tọa này!", "s4_2"))
                        .Add(new DialogueNode("s4_2", UnitCatalog.WandererId,
                            "Quỷ Vương Malkor chiếm tới 2×2 ô bàn cờ... Kéo máu hắn xuống dưới 70%, ta sẽ dập tắt lửa địa ngục bằng 3 nhát chém hoàn mỹ liên hoàn!"));

                case "stage-5":
                    // Nguyên văn đoạn thoại cao trào dưới Mật thất Thánh điện (GDD §4.4)
                    return new DialogueGraph("s5_1")
                        .Add(new DialogueNode("s5_1", UnitCatalog.WandererId,
                            "Tại sao lại là nơi này, Eosphoros? Bên trên mặt đất kia... dân chúng đang tung hô tên ngài.", "s5_2"))
                        .Add(new DialogueNode("s5_2", UnitCatalog.FallenEosphorosId,
                            "Chính vì họ đang tung hô ta! Thế gian này chỉ cần một vị thần hoàn mỹ, một đấng cứu thế không tì vết. Nhưng ngươi... một tên ăn mày mang lời nguyền dơ bẩn, ngươi lại dám chiến đấu hoàn mỹ hơn cả ta!", "s5_3"))
                        .Add(new DialogueNode("s5_3", UnitCatalog.WandererId,
                            "Ta chưa từng muốn vinh quang của ngài. Ta chỉ muốn sống…", "s5_4"))
                        .Add(new DialogueNode("s5_4", UnitCatalog.FallenEosphorosId,
                            "Vậy thì ngươi đã sai lầm khi chọn sống quá xuất sắc! Hãy nằm lại dưới nấm mồ này, để bức tượng đài của ta được vĩnh hằng!"));

                default:
                    return null;
            }
        }

        public static DialogueGraph GetOutro(string stageId)
        {
            if (stageId == "stage-5")
            {
                var choiceNode = new DialogueNode("s5_end_1", UnitCatalog.FallenEosphorosId,
                    "Thanh Thánh Kiếm đã nứt vỡ... Giữa hầm sâu không một ai chứng kiến, ngươi sẽ chọn con đường nào, Kẻ Lang Thang?");
                choiceNode.AddChoice("Kết thúc A — 'Lời nói dối vĩnh hằng' (Buông gươm chấp nhận tan biến)", "ending_a");
                choiceNode.AddChoice("Kết thúc B — 'Tội đồ ẩn cư' (Vung kiếm quyết chiến chấm dứt vòng lặp)", "ending_b");

                var nodeA = new DialogueNode("ending_a", UnitCatalog.WandererId,
                    "Ta buông kiếm quỳ xuống với hy vọng thức tỉnh lương tri của ân nhân năm xưa... Lời nguyền thời gian tan biến, còn Eosphoros bước lên bậc thang đầy nắng, đón nhận sự tôn thờ của thế gian bằng một lời nói dối hòa bình hoàn hảo.")
                {
                    Ending = StageResult.EndingA
                };

                var nodeB = new DialogueNode("ending_b", UnitCatalog.WandererId,
                    "Lịch sử được viết bởi những kẻ chiến thắng, nhưng sự thật thì ngủ vùi trong bóng tối. Ta đã giết ân nhân của mình... Nhưng ít nhất, ngọn lửa của một lời nói dối đã tắt. Vòng lặp vĩnh viễn đóng lại.")
                {
                    Ending = StageResult.EndingB
                };

                return new DialogueGraph("s5_end_1")
                    .Add(choiceNode)
                    .Add(nodeA)
                    .Add(nodeB);
            }

            return new DialogueGraph("outro_1")
                .Add(new DialogueNode("outro_1", UnitCatalog.WandererId,
                    "Chỉ huy quỷ dữ trấn ải đã gục ngã sau màn Đấu Kỹ Clash. Con đường tiến sâu hơn vào bóng tối đã mở."));
        }
    }
}
