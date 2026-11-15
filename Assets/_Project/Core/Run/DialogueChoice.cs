using System;

namespace BTH.Core.Run
{
    /// <summary>Một lựa chọn trong node thoại; điều kiện đọc từ bộ nhớ run (giữ qua rewind).</summary>
    public sealed class DialogueChoice
    {
        public DialogueChoice(string text, string nextId, Func<IRunKnowledge, bool> condition = null,
            bool hideWhenFalse = false)
        {
            Text = text ?? string.Empty;
            NextId = nextId;
            Condition = condition;
            HideWhenFalse = hideWhenFalse;
        }

        public string Text { get; }

        public string NextId { get; }

        public Func<IRunKnowledge, bool> Condition { get; }

        public bool HideWhenFalse { get; }

        public bool IsAvailable(IRunKnowledge knowledge)
        {
            return Condition == null || Condition(knowledge);
        }
    }
}
