namespace BTH.Core.Run
{
    /// <summary>
    /// Run knowledge sống ngoài snapshot: rewind không xoá những gì người chơi đã học.
    /// Value là số nguyên (số lần chết, cờ 0/1, số chiêu đã thấy...).
    /// </summary>
    public readonly struct RunKnowledgeUpdatedEvent
    {
        public readonly string Key;
        public readonly int Value;

        public RunKnowledgeUpdatedEvent(string key, int value)
        {
            Key = key;
            Value = value;
        }
    }
}
