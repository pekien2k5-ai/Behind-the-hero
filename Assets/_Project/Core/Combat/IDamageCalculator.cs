using BTH.Core.Run;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Tính và áp sát thương. Compute là hàm thuần (không đổi state);
    /// Apply mới trừ HP và phát DamageDealtEvent.
    /// </summary>
    public interface IDamageCalculator
    {
        DamageResult Compute(DamageRequest request, GameState state);

        DamageResult Apply(DamageRequest request, GameState state);
    }
}
