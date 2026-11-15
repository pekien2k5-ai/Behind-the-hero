namespace BTH.Core.Run
{
    /// <summary>
    /// Chụp/tua state. Restore(Capture()) phải trùng từng trường (SnapshotTests bắt buộc);
    /// checkpoint mặc định ở StageStart, TurnStart và ClashStart.
    /// </summary>
    public interface ISnapshotService
    {
        bool CanRewind { get; }

        GameSnapshot Capture();

        void Restore(GameSnapshot snapshot);

        void PushCheckpoint(SnapshotTrigger trigger);

        /// <summary>Tua về checkpoint gần nhất đã đẩy; phát RewindPerformedEvent.</summary>
        void Rewind();

        void ClearCheckpoints();
    }
}
