using System;

namespace BTH.Core.Run
{
    /// <summary>
    /// Chạy thoại thuần logic: Unity chỉ đọc Current/Choices và gọi Advance/Choose.
    /// Không tự đổi state chiến đấu; Ending chỉ là dữ liệu cho màn hình kết thúc.
    /// </summary>
    public sealed class DialogueRunner
    {
        private readonly DialogueGraph _graph;
        private readonly IRunKnowledge _knowledge;

        public DialogueRunner(DialogueGraph graph, IRunKnowledge knowledge = null)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _knowledge = knowledge;
        }

        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current == null;

        public StageResult Ending { get; private set; }

        public void Start()
        {
            if (!_graph.TryGet(_graph.StartId, out var node))
            {
                throw new InvalidOperationException("StartId không hợp lệ: " + _graph.StartId);
            }

            Ending = StageResult.None;
            Current = node;
        }

        /// <summary>Đi tới NextId. Node có lựa chọn thì phải gọi Choose; hết đường thì kết thúc.</summary>
        public void Advance()
        {
            if (Current == null)
            {
                return;
            }

            if (Current.Choices.Count > 0)
            {
                throw new InvalidOperationException("Node có lựa chọn, phải gọi Choose().");
            }

            MoveTo(Current.NextId);
        }

        public void Choose(int index)
        {
            if (Current == null)
            {
                return;
            }

            var visible = 0;
            for (int i = 0; i < Current.Choices.Count; i++)
            {
                var choice = Current.Choices[i];
                if (!choice.IsAvailable(_knowledge))
                {
                    if (choice.HideWhenFalse)
                    {
                        continue;
                    }

                    throw new InvalidOperationException("Lựa chọn chưa mở khoá.");
                }

                if (visible == index)
                {
                    MoveTo(choice.NextId);
                    return;
                }

                visible++;
            }

            throw new ArgumentOutOfRangeException(nameof(index), "Không có lựa chọn tại vị trí này.");
        }

        private void MoveTo(string nextId)
        {
            if (Current.Ending != StageResult.None)
            {
                Ending = Current.Ending;
            }

            if (string.IsNullOrEmpty(nextId) || !_graph.TryGet(nextId, out var next))
            {
                Current = null;
                return;
            }

            Current = next;
        }
    }
}
