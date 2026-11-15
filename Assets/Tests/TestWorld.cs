using System;
using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Turn;
using BTH.Core.Units;

namespace BTH.Core.Tests
{
    /// <summary>Dựng state/test fixture không cần Unity, không cần prefab.</summary>
    internal static class TestWorld
    {
        public static UnitDefinition Unit(string id, UnitFaction faction, int hp = 10, int attack = 4,
            int armor = 0, int move = 2, int initiative = 10, int sizeX = 1, int sizeY = 1,
            ElementId? element = null, string brainId = "", params string[] skillIds)
        {
            return new UnitDefinition
            {
                Id = id,
                DisplayName = id,
                Faction = faction,
                Archetype = faction == UnitFaction.Enemy ? UnitArchetype.Minion : UnitArchetype.Hero,
                MaxHp = hp,
                Attack = attack,
                Armor = armor,
                Initiative = initiative,
                SizeX = sizeX,
                SizeY = sizeY,
                Move = MoveProfile.Walk(move),
                NativeElement = element ?? ElementId.None,
                BrainId = brainId ?? string.Empty,
                SkillIds = skillIds
            };
        }

        public static UnitDefinition Boss(string id, int hp = 20, int attack = 5, int initiative = 5,
            int sizeX = 1, int sizeY = 1)
        {
            var definition = Unit(id, UnitFaction.Enemy, hp, attack, 0, 1, initiative, sizeX, sizeY);
            definition.Archetype = UnitArchetype.StageBoss;
            return definition;
        }

        public static GameState State(int width = 8, int height = 8, IEventBus events = null,
            IGameRules rules = null)
        {
            events = events ?? new EventBus();
            var grid = new GridMap(width, height, events);
            var units = new UnitRegistry(grid, events);
            var state = new GameState(grid, units, rules ?? new GameRules(), events, new XorShiftRandom(1234));
            state.Pathfinder = new AStarPathfinder(grid);
            state.Skills = new SkillLibrary();
            state.Damage = new DamageCalculator();
            state.Telegraph = new TelegraphService(events);
            state.Clash = new ClashResolver();
            state.Duel = new ClashDuel(state);
            state.BossPhase = new BossPhaseController(state.Rules);
            state.Knowledge = new RunKnowledge(events);
            state.Turn = new TurnManager(state);
            state.Snapshots = new SnapshotService(state);
            return state;
        }

        public static StageDefinition Stage(string id = "stage-1", int width = 8, int height = 8,
            int turnLimit = 25)
        {
            return new StageDefinition
            {
                Id = id,
                DisplayName = id,
                Width = width,
                Height = height,
                TurnLimit = turnLimit
            };
        }

        public static StatusContext StatusContextFor(UnitState unit, GameState state)
        {
            return new StatusContext(unit, state.Events, state.Units);
        }
    }
}
