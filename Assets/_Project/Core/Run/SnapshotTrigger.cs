namespace BTH.Core.Run
{
    /// <summary>Thời điểm chụp checkpoint để rewind.</summary>
    public enum SnapshotTrigger
    {
        StageStart = 0,
        TurnStart = 1,
        ClashStart = 2
    }
}
