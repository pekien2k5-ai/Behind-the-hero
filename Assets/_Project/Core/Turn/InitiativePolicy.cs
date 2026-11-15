using System.Collections.Generic;
using BTH.Core.Units;

namespace BTH.Core.Turn
{
    /// <summary>Thứ tự mặc định: Initiative cao đi trước, hoà thì UnitId nhỏ trước.</summary>
    public sealed class InitiativePolicy : IInitiativePolicy
    {
        public List<UnitId> Order(IReadOnlyList<UnitState> units)
        {
            var result = new List<UnitId>();
            if (units == null)
            {
                return result;
            }

            var copy = new List<UnitState>(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && units[i].IsAlive)
                {
                    copy.Add(units[i]);
                }
            }

            copy.Sort((a, b) =>
            {
                int byInitiative = b.Definition.Initiative.CompareTo(a.Definition.Initiative);
                return byInitiative != 0 ? byInitiative : a.Id.Value.CompareTo(b.Id.Value);
            });

            for (int i = 0; i < copy.Count; i++)
            {
                result.Add(copy[i].Id);
            }

            return result;
        }
    }
}
