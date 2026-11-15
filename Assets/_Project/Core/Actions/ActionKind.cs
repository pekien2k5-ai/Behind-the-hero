namespace BTH.Core.Actions
{
    /// <summary>Loại hành động người chơi/AI gửi vào GameSession.</summary>
    public enum ActionKind
    {
        Move = 0,
        Attack = 1,
        Skill = 2,
        Guard = 3,
        Wait = 4,
        EndTurn = 5,
        Reset = 6,
        ClashSkill = 7
    }
}
