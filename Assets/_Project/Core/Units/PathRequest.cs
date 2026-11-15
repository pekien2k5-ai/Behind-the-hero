using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>Yêu cầu tìm đường. MaxCost 0 = không giới hạn.</summary>
    public readonly struct PathRequest
    {
        public readonly GridCoord Start;
        public readonly GridCoord Goal;
        public readonly MoveProfile Profile;
        public readonly int MaxCost;
        public readonly UnitId IgnoreUnit;

        public PathRequest(GridCoord start, GridCoord goal, MoveProfile profile, int maxCost = 0,
            UnitId ignoreUnit = default)
        {
            Start = start;
            Goal = goal;
            Profile = profile;
            MaxCost = maxCost;
            IgnoreUnit = ignoreUnit;
        }
    }
}
