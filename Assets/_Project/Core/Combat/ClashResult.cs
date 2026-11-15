namespace BTH.Core.Combat
{
    /// <summary>Kết quả so sánh lực trong Clash. Win khi player > boss, Draw khi bằng.</summary>
    public enum ClashResult
    {
        None = 0,
        Win = 1,
        Draw = 2,
        Lose = 3
    }
}
