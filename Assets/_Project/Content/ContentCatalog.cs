using BTH.Content.Brains;
using BTH.Content.Skills;
using BTH.Content.Stages;
using BTH.Content.Terrain;
using BTH.Core.Combat;
using BTH.Core.Run;

namespace BTH.Content
{
    public static class ContentCatalog
    {
        public static GameSession CreateSession(int seed = 20260101)
        {
            var rules = new GameRules();
            var skills = SkillCatalog.CreateLibrary();
            var brains = BrainCatalog.CreateLibrary();
            var elements = ElementMatrix.CreateDefault();
            var session = new GameSession(
                rules: rules,
                skills: skills,
                brains: brains,
                clash: new ClashResolver(),
                elements: elements,
                snapshotCapacity: 12,
                seed: seed,
                onUnitEnteredCell: TerrainCatalog.HandleUnitEnteredCell);

            var stages = StageCatalog.All();
            for (int i = 0; i < stages.Count; i++)
            {
                session.RegisterStage(stages[i]);
            }

            return session;
        }
    }
}
