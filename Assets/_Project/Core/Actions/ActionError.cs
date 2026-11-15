namespace BTH.Core.Actions
{
    /// <summary>Lý do một ActionRequest bị từ chối. UI dùng để hiện thông báo, không tự suy luật.</summary>
    public enum ActionError
    {
        None = 0,
        UnknownActor = 1,
        UnitDead = 2,
        NotPlayerPhase = 3,
        NotEnemyPhase = 4,
        NoActionPoints = 5,
        OutOfRange = 6,
        TargetEmpty = 7,
        NoLineOfSight = 8,
        CellBlocked = 9,
        CellOccupied = 10,
        NotEnoughMovePoints = 11,
        SkillNotReady = 12,
        SkillOnCooldown = 13,
        CannotMoveRooted = 14,
        ActorStunned = 15,
        StageEnded = 16,
        InvalidTarget = 17,
        ClashNotAvailable = 18,
        ObjectiveAlreadyEnded = 19,
        AlreadyMoved = 20,
        AlreadyActed = 21,

        /// <summary>Đang ở pha khác (ví dụ trong Duel mà gửi lệnh di chuyển).</summary>
        WrongPhase = 22
    }
}
