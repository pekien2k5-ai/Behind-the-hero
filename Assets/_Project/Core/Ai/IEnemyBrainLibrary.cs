using System.Collections.Generic;

namespace BTH.Core.Ai
{
    /// <summary>Tra brain theo BrainId trong UnitDefinition (Content đăng ký lúc bootstrap).</summary>
    public interface IEnemyBrainLibrary
    {
        IReadOnlyList<IEnemyBrain> All { get; }

        void Register(IEnemyBrain brain);

        bool TryGet(string brainId, out IEnemyBrain brain);

        IEnemyBrain Get(string brainId);
    }
}
