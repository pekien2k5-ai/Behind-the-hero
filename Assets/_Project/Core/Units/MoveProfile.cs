namespace BTH.Core.Units
{
    /// <summary>
    /// Cách một unit di chuyển. Là kiểu giá trị để copy vào path request mà không cấp phát.
    /// </summary>
    public readonly struct MoveProfile
    {
        public readonly int MovePoints;
        public readonly bool CanFly;
        public readonly bool IgnoresHazard;
        public readonly bool CanPassUnits;

        public MoveProfile(int movePoints, bool canFly = false, bool ignoresHazard = false, bool canPassUnits = false)
        {
            MovePoints = movePoints < 0 ? 0 : movePoints;
            CanFly = canFly;
            IgnoresHazard = ignoresHazard;
            CanPassUnits = canPassUnits;
        }

        public static MoveProfile Walk(int movePoints)
        {
            return new MoveProfile(movePoints);
        }

        public static MoveProfile Fly(int movePoints)
        {
            return new MoveProfile(movePoints, true, true);
        }

        public override string ToString()
        {
            return "Move(" + MovePoints + (CanFly ? ",Fly" : string.Empty) + ")";
        }
    }
}
