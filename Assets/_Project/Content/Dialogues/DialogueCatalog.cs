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
        public const string Narrator = "Người dẫn chuyện";

        private static readonly string[] Oakhaven =
        {
            "Năm thứ 12 triều đại Thiên hoàng thứ XIV, bầu trời vương quốc nhuốm một màu đỏ quạch của máu và tro tàn. Thị trấn Oakhaven trù phú – cửa ngõ kinh thành – bị san phẳng chỉ trong một đêm bởi quân đoàn Quỷ vương.",
            "Hơn 5.000 dân thường tan thành tro bụi, số ít sống sót trở thành lưu dân tị nạn khốn cùng. Trong dòng người ấy có hắn.",
            "May mắn sống sót nhưng hắn lại gánh chịu một lời nguyền quái ác ăn sâu vào tủy: thể xác liên tục bị hoại tử, nhưng mỗi khi ngã xuống, thời gian lập tức quay ngược lại, kéo hắn sống dậy để bắt đầu một vòng lặp mới.",
            "Trôi dạt về khu ổ chuột ven kinh đô lộng lẫy, hắn sống lay lắt như một bóng ma.",
            "Giữa lúc tuyệt vọng sắp nuốt chửng khát vọng sống, Eosphoros – vị Dũng giả rực rỡ nhất của Thần điện, biểu tượng của ánh sáng và công lý – đã chìa tay cứu vớt hắn.",
            "Bằng nước thánh thanh khiết, Eosphoros xoa dịu vết thương của lời nguyền, trao cho hắn thanh kiếm ngắn.",
        };

        private static readonly string[] Campaign =
        {
            "Trên chặng đường thảo phạt quân đoàn quỷ dữ, Eosphoros luôn tiên phong làm tấm khiên vững chãi càn quét các Chỉ huy quỷ dữ. Cơ thể Eosphoros chằng chịt vết thương chiến trận, khiến nhịp độ chiến đấu đôi lúc chững lại.",
            "Thế nhưng, Eosphoros không hề hay biết rằng kẻ đồng hành ốm yếu bên cạnh đang dùng chính cái chết làm vũ khí tối thượng.",
        };

        private static readonly string[] Rivalry =
        {
            "Qua hàng trăm lần bị quái vật xé xác, vòng lặp thời gian tua ngược liên tục, mang theo toàn bộ ký ức và kinh nghiệm vào kiếp sống mới.",
            "Hắn giải mã được mọi quỹ đạo đòn đánh, thấu thị điểm mù và tung ra những nhát kiếm hoàn mỹ tuyệt đối. Kẻ ăn mày tàn tật ngày nào bộc phát kỹ năng thực chiến áp đảo, làm lu mờ cả ánh hào quang của Thần điện.",
            "Từng ánh mắt thán phục của hắn dành cho Eosphoros vô tình trở thành những mũi dao găm vào lòng tự tôn của người anh hùng.",
            "Cảm giác bất lực của một thiên tài khổ luyện cả đời nay lại bị một tên ăn trộm vượt mặt đã gieo mầm đố kỵ ăn mòn tâm trí vị thánh sống.",
        };

        private static readonly string[] MalkorVictory =
        {
            "Trận huyết chiến tại vương tọa Quỷ vương khép lại trong sự ngỡ ngàng tột độ: Eosphoros kiệt sức quỳ một chân trên đất, còn Kẻ lang thang tiến lên, dùng kiếm kỹ tích lũy qua vô số kiếp luân hồi để tự tay kết liễu Quỷ vương.",
        };

        private static readonly string[] Betrayal =
        {
            "Ngày khải hoàn, kinh đô rợp cờ hoa tung hô Eosphoros. Anh ta hiên ngang nhận lấy vương miện, tượng đài và sự tôn sùng mù quáng của muôn dân.",
            "Nhưng đằng sau lớp vỏ bọc hoàn mỹ ấy là một linh hồn đã thối rữa vì nỗi sợ bị hạ bệ.",
            "Khuất bóng mặt trời, Eosphoros lừa người đồng đội xuống tầng hầm ẩm mốc sâu bên dưới thánh điện để diệt khẩu nhằm chôn vùi sự thật vĩnh viễn.",
        };

        private static readonly string[] EndingA =
        {
            "Kẻ lang thang buông kiếm, quỳ xuống với hy vọng thức tỉnh lương tri của vị ân nhân năm xưa. Đáp lại chỉ là nhát kiếm xuyên tim lạnh lùng của Eosphoros.",
            "Lời nguyền thời gian tan biến, Kẻ lang thang nhắm mắt xuôi tay trong vũng máu tối tăm, biến mất không một dòng lịch sử.",
            "Còn Eosphoros bước lên bậc thang đầy nắng, vĩnh viễn mỉm cười đón nhận sự tôn thờ của thế gian bằng một lời nói dối hòa bình hoàn hảo.",
        };

        private static readonly string[] EndingB =
        {
            "Kẻ lang thang vung kiếm tử chiến sinh tử dưới hầm sâu. Tiếng reo hò xưng tụng vị thánh sống trên mặt đất hòa lẫn với tiếng kim loại va chạm chát chúa trong bóng tối.",
            "Sau trận huyết chiến nghẹt thở, Kẻ lang thang hạ sát Eosphoros. Thế gian mãi mãi coi hắn là tên phản đồ đê tiện giết hại đấng cứu thế. Nhưng hắn không màng thanh minh.",
            "Tra gươm vào vỏ, hắn bỏ lại xác ân nhân trong bóng tối, rũ bỏ mọi vinh quang để bước vào kiếp sống ẩn cư, lặng lẽ đếm từng ngày chờ căn bệnh hoại tử tước đi sinh mạng tàn tạ của mình.",
        };

        private static readonly string[] EndingBMonologue =
        {
            "Lịch sử được viết bởi những kẻ chiến thắng, nhưng sự thật thì ngủ vùi trong bóng tối. Ta đã giết ân nhân của mình. Thế gian sẽ nguyền rủa ta như một kẻ phản đồ đê tiện nhất.",
            "Nhưng ít nhất... ngọn lửa của một lời nói dối đã tắt. Vòng lặp vĩnh viễn đóng lại. Giờ đây, chỉ còn ta và cơn đau này... đếm ngược đến hơi thở cuối cùng.",
        };

        public static IReadOnlyList<(string Title, string Content)> GetThreeActsLore() =>
            new (string, string)[]
            {
                ("Hồi I: Thảm họa Oakhaven & Lời nguyền Khởi sinh", string.Join(" ", Oakhaven)),
                ("Hồi II: Chặng đường Thảo phạt & Mầm mống Đố kỵ",
                    string.Join(" ", Campaign) + " " + string.Join(" ", Rivalry) + " " + string.Join(" ", MalkorVictory)),
                ("Hồi III: Khúc khải hoàn đẫm máu & Bi kịch dưới Mật thất",
                    string.Join(" ", Betrayal) + " Kết thúc A: " + string.Join(" ", EndingA)
                    + " Kết thúc B: " + string.Join(" ", EndingB))
            };

        private static DialogueGraph NarratedIntro(string prefix, IReadOnlyList<string> pages, string nextId)
        {
            var graph = new DialogueGraph(prefix + "_1");
            AddPages(graph, prefix, Narrator, pages, nextId);
            return graph;
        }

        private static void AddPages(DialogueGraph graph, string prefix, string speaker,
            IReadOnlyList<string> pages, string nextId = null, StageResult ending = StageResult.None)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                bool isLast = i == pages.Count - 1;
                var node = new DialogueNode($"{prefix}_{i + 1}", speaker, pages[i],
                    isLast ? nextId : $"{prefix}_{i + 2}");
                if (isLast) node.Ending = ending;
                graph.Add(node);
            }
        }

        public static DialogueGraph GetIntro(string stageId)
        {
            switch (stageId)
            {
                case "stage-1":
                    return NarratedIntro("s1_story", Oakhaven, "s1_1")
                        .Add(new DialogueNode("s1_1", UnitCatalog.EosphorosId,
                            "Hãy đứng lên. Thế gian này không ruồng bỏ ngươi chừng nào ngọn lửa trong mắt ngươi còn cháy. Cầm lấy thanh kiếm này. Không phải chỉ những kẻ được thần linh lựa chọn, mới xứng đáng được mang danh anh hùng!", "s1_1b"))
                        .Add(new DialogueNode("s1_1b", UnitCatalog.WandererId,
                            "Ngài... thật sự tin một kẻ mang lời nguyền hoại tử như ta? Nếu thanh kiếm này có thể đổi lại một cơ hội sống, ta sẽ bước theo ngài đến tận cùng chiến trường.", "s1_1c"))
                        .Add(new DialogueNode("s1_1c", UnitCatalog.EosphorosId,
                            "Đừng nghi ngờ bản thân nữa. Phía trước là tàn tích Oakhaven — hãy bước cùng ta và chứng minh lưỡi kiếm trong tay ngươi!", "s1_2"))
                        .Add(new DialogueNode("s1_2", UnitCatalog.WandererId,
                            "Tàn tích Oakhaven... Đao Phủ Tro Tàn đang chặn đường. Ta phải tận dụng các trụ đá, tránh ô gạch nứt vỡ và mở rương thánh dược!", "s1_3"))
                        .Add(new DialogueNode("s1_3", UnitCatalog.AshenExecutionerId,
                            "Kẻ mang dấu ấn hoại tử! Đại rìu của ta sẽ bổ nứt từng ô đá dưới chân ngươi!"));

                case "stage-2":
                    return NarratedIntro("s2_story", Campaign, "s2_1")
                        .Add(new DialogueNode("s2_1", UnitCatalog.PlagueWitchId,
                            "Khí độc trong Rừng Bệnh Dịch này được chưng cất từ những kẻ đã bị Dũng Giả bỏ rơi...", "s2_2"))
                        .Add(new DialogueNode("s2_2", UnitCatalog.WandererId,
                            "Ta phải di chuyển trên các phiến đá khô, né bàn tay xác sống Grave Hands và dùng Necrotic Burst phản ngược bão độc!"));

                case "stage-3":
                    return NarratedIntro("s3_story", Rivalry, "s3_1")
                        .Add(new DialogueNode("s3_1", UnitCatalog.AbyssKnightId,
                            "Khiên Mắt Quỷ phản chấn mọi đòn trực diện. Ngươi không thể vượt qua Cổng Thành Vực Sâu này!", "s3_2"))
                        .Add(new DialogueNode("s3_2", UnitCatalog.WandererId,
                            "Những trụ đá obsidian sẽ chắn tia thương xuyên thấu. Bước Nhảy Thời Gian sẽ đưa ta lách vào sườn hở của hắn!"));

                case "stage-4":
                    return new DialogueGraph("s4_story_1")
                        .Add(new DialogueNode("s4_story_1", Narrator,
                            "Vương tọa Hỏa Ngục rực cháy trước mắt. Qua bao lần chết đi sống lại, kiếm pháp của Kẻ Lang Thang đã chạm ngưỡng hoàn mỹ — còn bước chân Eosphoros phía sau lại ngày một trĩu nặng.", "s4_1"))
                        .Add(new DialogueNode("s4_1", UnitCatalog.DemonKingMalkorId,
                            "Lại là chiếc bóng đi sau Eosphoros! Cự Kiếm Hỏa Ngục và Thiên Thạch Dung Nham của ta sẽ thiêu rụi toàn bộ vương tọa này!", "s4_2"))
                        .Add(new DialogueNode("s4_2", UnitCatalog.WandererId,
                            "Quỷ Vương Malkor chiếm tới 2×2 ô bàn cờ... Kéo máu hắn xuống dưới 70%, ta sẽ dập tắt lửa địa ngục bằng 3 nhát chém hoàn mỹ liên hoàn!"));

                case "stage-5":
                    // Nguyên văn đoạn thoại cao trào dưới Mật thất Thánh điện (GDD §4.4)
                    return NarratedIntro("s5_story", Betrayal, "s5_1")
                        .Add(new DialogueNode("s5_1", UnitCatalog.WandererId,
                            "Tại sao lại là nơi này, Eosphoros? Bên trên mặt đất kia... dân chúng đang tung hô tên ngài.", "s5_2"))
                        .Add(new DialogueNode("s5_2", UnitCatalog.FallenEosphorosId,
                            "Chính vì họ đang tung hô ta! Thế gian này chỉ cần một vị thần hoàn mỹ, một đấng cứu thế không tì vết. Nhưng ngươi... một tên ăn mày mang lời nguyền dơ bẩn, ngươi lại dám chiến đấu hoàn mỹ hơn cả ta!", "s5_2b"))
                        .Add(new DialogueNode("s5_2b", UnitCatalog.FallenEosphorosId,
                            "Nhát kiếm của ngươi làm ô uế ánh hào quang của ta! Ngươi nghĩ ta sẽ để sự thật đó bước ra ánh sáng sao?", "s5_3"))
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
            if (stageId == "stage-1")
            {
                return new DialogueGraph("s1_end_1")
                    .Add(new DialogueNode("s1_end_1", UnitCatalog.EosphorosId,
                        "Nhát kiếm kết liễu gọn ghẽ lắm! Không ngờ một kẻ từng gục ngã giữa tro tàn Oakhaven lại có thể đọc vị rìu của Đao Phủ nhanh đến vậy.", "s1_end_2"))
                    .Add(new DialogueNode("s1_end_2", UnitCatalog.WandererId,
                        "Ta chỉ cố sống sót qua từng đòn đánh... Tiến về Hầm Mộ Độc Khí thôi, thưa Dũng Giả."));
            }

            if (stageId == "stage-2")
            {
                return new DialogueGraph("s2_end_1")
                    .Add(new DialogueNode("s2_end_1", UnitCatalog.EosphorosId,
                        "Lạ thật... Morbida còn chưa kịp niệm xong độc chú, ngươi đã bước sẵn vào điểm mù sau trụ đá. Như thể ngươi đã thấy trước tương lai vậy.", "s2_end_2"))
                    .Add(new DialogueNode("s2_end_2", UnitCatalog.WandererId,
                        "Mọi tuyệt kỹ của lũ quỷ đều có quy luật. Chỉ cần để ý kỹ, ngài cũng sẽ thấy.", "s2_end_3"))
                    .Add(new DialogueNode("s2_end_3", UnitCatalog.EosphorosId,
                        "(Ánh mắt Dũng Giả khẽ trầm xuống) Phải... phải rồi. Hãy đi tiếp đến Hành Lang Hư Không."));
            }

            if (stageId == "stage-3")
            {
                return new DialogueGraph("s3_end_1")
                    .Add(new DialogueNode("s3_end_1", UnitCatalog.EosphorosId,
                        "Khiên Mắt Quỷ làm ta suýt gãy kiếm, vậy mà ngươi... lại một mình xuyên phá qua kẽ hở của Kỵ Sĩ Hư Không không một vết xước.", "s3_end_2"))
                    .Add(new DialogueNode("s3_end_2", UnitCatalog.WandererId,
                        "Vết thương trên vai ngài đang rỉ máu rồi, Eosphoros. Để ta đi trước mở đường vào Vương Tọa Hỏa Ngục.", "s3_end_3"))
                    .Add(new DialogueNode("s3_end_3", UnitCatalog.EosphorosId,
                        "Không cần lòng thương hại của một kẻ vô danh! (Gã siết chặt chuôi thánh kiếm, nhìn theo bóng lưng Kẻ Lang Thang)"));
            }

            if (stageId == "stage-4")
            {
                var graph = new DialogueGraph("s4_story_end_1");
                AddPages(graph, "s4_story_end", Narrator, MalkorVictory);
                return graph;
            }

            if (stageId == "stage-5")
            {
                var choiceNode = new DialogueNode("s5_end_1", UnitCatalog.FallenEosphorosId,
                    "Thanh Thánh Kiếm đã nứt vỡ... Giữa hầm sâu không một ai chứng kiến, ngươi sẽ chọn con đường nào, Kẻ Lang Thang?");
                choiceNode.AddChoice("Kết thúc A — Lời nói dối vĩnh hằng (Buông gươm)", "ending_a_1");
                choiceNode.AddChoice("Kết thúc B — Tội đồ ẩn cư (Vung kiếm quyết chiến)", "ending_b_1");

                var graph = new DialogueGraph(choiceNode.Id).Add(choiceNode);
                AddPages(graph, "ending_a", Narrator, EndingA, ending: StageResult.EndingA);
                AddPages(graph, "ending_b", Narrator, EndingB, "ending_b_monologue_1");
                AddPages(graph, "ending_b_monologue", UnitCatalog.WandererId, EndingBMonologue,
                    ending: StageResult.EndingB);
                return graph;
            }

            return null;
        }
    }
}
