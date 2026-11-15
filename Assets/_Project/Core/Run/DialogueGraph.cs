using System;
using System.Collections.Generic;

namespace BTH.Core.Run
{
    /// <summary>Đồ thị thoại: node theo id, node bắt đầu, kiểm tra tính hợp lệ khi dựng.</summary>
    public sealed class DialogueGraph
    {
        private readonly Dictionary<string, DialogueNode> _nodes =
            new Dictionary<string, DialogueNode>(StringComparer.Ordinal);

        public DialogueGraph(string startId = null)
        {
            StartId = startId;
        }

        public string StartId { get; set; }

        public IReadOnlyCollection<DialogueNode> Nodes => _nodes.Values;

        public DialogueGraph Add(DialogueNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _nodes[node.Id] = node;
            if (string.IsNullOrEmpty(StartId))
            {
                StartId = node.Id;
            }

            return this;
        }

        public bool TryGet(string id, out DialogueNode node)
        {
            if (string.IsNullOrEmpty(id))
            {
                node = null;
                return false;
            }

            return _nodes.TryGetValue(id, out node);
        }

        public DialogueNode Get(string id)
        {
            if (!_nodes.TryGetValue(id, out var node))
            {
                throw new KeyNotFoundException("Không có node thoại: " + id);
            }

            return node;
        }

        /// <summary>Kiểm tra mọi next/choice trỏ tới node tồn tại. Trả về danh sách lỗi.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(StartId))
            {
                errors.Add("Graph chưa có StartId.");
            }
            else if (!_nodes.ContainsKey(StartId))
            {
                errors.Add("StartId trỏ tới node không tồn tại: " + StartId);
            }

            foreach (var node in _nodes.Values)
            {
                if (!string.IsNullOrEmpty(node.NextId) && !_nodes.ContainsKey(node.NextId))
                {
                    errors.Add(node.Id + ".NextId -> " + node.NextId + " không tồn tại.");
                }

                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var choice = node.Choices[i];
                    if (!string.IsNullOrEmpty(choice.NextId) && !_nodes.ContainsKey(choice.NextId))
                    {
                        errors.Add(node.Id + ".Choices[" + i + "] -> " + choice.NextId + " không tồn tại.");
                    }
                }
            }

            return errors;
        }
    }
}
