using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Ai
{
    /// <summary>
    /// Chuyển boss sang phase 2 khi HP dưới ngưỡng (mặc định 70%),
    /// khoá sân và đổi moveset theo dữ liệu Content.
    /// </summary>
    public interface IBossPhaseController
    {
        int Phase { get; }

        bool ShouldEnterPhase2(int hp, int maxHp);

        void EnterPhase2(GameState state);
    }
}
