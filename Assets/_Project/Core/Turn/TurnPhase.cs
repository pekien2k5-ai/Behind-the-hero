namespace BTH.Core.Turn
{
    /// <summary>
    /// Bốn nhịp của một lượt: Telegraph (báo trước) → Player → Enemy → TurnEnd.
    /// Clash là pha chèn khi boss chuyển phase.
    /// </summary>
    public enum TurnPhase
    {
        None = 0,
        StageIntro = 1,
        Telegraph = 2,
        Player = 3,
        Enemy = 4,
        TurnEnd = 5,
        Clash = 6,
        StageEnded = 7,

        /// <summary>Phase 2: đấu chiêu 1v1, không bàn cờ/AP/EndTurn (Docs/QUYET_DINH.md).</summary>
        Duel = 8
    }
}
