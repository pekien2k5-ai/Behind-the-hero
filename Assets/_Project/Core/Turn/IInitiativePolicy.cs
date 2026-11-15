using System.Collections.Generic;
using BTH.Core.Units;

namespace BTH.Core.Turn
{
    /// <summary>Thứ tự hành động của phe địch. Sort ổn định để AI tất định.</summary>
    public interface IInitiativePolicy
    {
        /// <summary>Initiative giảm dần, hoà thì UnitId tăng dần.</summary>
        List<UnitId> Order(IReadOnlyList<UnitState> units);
    }
}
