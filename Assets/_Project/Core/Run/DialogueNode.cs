using System;
using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Một dòng thoại. Điều kiện và kết thúc màn để Unity chỉ việc hiển thị.</summary>
    public sealed class DialogueNode
    {
        private readonly List<DialogueChoice> _choices = new List<DialogueChoice>();

        public DialogueNode(string id, string speaker, string text, string nextId = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            NextId = nextId;
        }

        public string Id { get; }

        public string Speaker { get; }

        public string Text { get; }

        public string NextId { get; set; }

        public IReadOnlyList<DialogueChoice> Choices => _choices;

        /// <summary>Kết thúc màn gắn với node này (EndingA/EndingB); None nếu chỉ là thoại.</summary>
        public StageResult Ending { get; set; }

        public DialogueNode AddChoice(string text, string nextId, Func<IRunKnowledge, bool> condition = null,
            bool hideWhenFalse = false)
        {
            _choices.Add(new DialogueChoice(text, nextId, condition, hideWhenFalse));
            return this;
        }
    }
}
