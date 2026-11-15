namespace BTH.Core.Units
{
    /// <summary>Kết quả tìm đường; OutOfRange = đích hợp lệ nhưng xa hơn tầm đi.</summary>
    public enum PathStatus
    {
        Found = 0,
        NoPath = 1,
        OutOfRange = 2,
        InvalidStart = 3,
        InvalidTarget = 4
    }
}
