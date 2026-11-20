using System.Collections.Generic;
using BTH.Core.Ai;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Content.Brains
{
    /// <summary>
    /// Hiệu ứng vùng Telegraph: khi pha Enemy thi hành intent đã công bố, chỉ những đơn vị
    /// còn đứng trong vùng DangerCells mới trúng sát thương/trạng thái (cho phép người chơi giải đố né đòn).
    /// </summary>
    public sealed class TelegraphedZoneEffect : IGameEffect
    {
        private readonly IReadOnlyList<GridCoord> _cells;
        private readonly int _power;
        private readonly ElementId _element;
        private readonly bool _ignoresArmor;
        private readonly System.Func<IStatusEffect> _statusFactory;
        private readonly TerrainId _spawnTerrain;
        private readonly TerrainFlags _terrainFlags;
        private readonly int _hazardDmg;
        private readonly int _collapseTurns;

        public TelegraphedZoneEffect(
            IReadOnlyList<GridCoord> cells,
            int power,
            ElementId element,
            bool ignoresArmor = false,
            System.Func<IStatusEffect> statusFactory = null,
            TerrainId spawnTerrain = default,
            TerrainFlags terrainFlags = TerrainFlags.None,
            int hazardDmg = 0,
            int collapseTurns = 0)
        {
            _cells = cells ?? new List<GridCoord>();
            _power = power;
            _element = element;
            _ignoresArmor = ignoresArmor;
            _statusFactory = statusFactory;
            _spawnTerrain = spawnTerrain;
            _terrainFlags = terrainFlags;
            _hazardDmg = hazardDmg;
            _collapseTurns = collapseTurns;
        }

        public int Priority => 10;

        public void Resolve(EffectContext ctx)
        {
            if (!ctx.Units.TryGet(ctx.Source, out var caster) || !caster.IsAlive || caster.IsStaggered)
            {
                return;
            }

            var victims = ctx.Units.UnitsIn(_cells);
            for (int i = 0; i < victims.Count; i++)
            {
                var v = victims[i];
                if (v.Id == ctx.Source || !v.IsAlive || v.Faction == caster.Faction)
                {
                    continue;
                }

                if (_power > 0)
                {
                    var req = new DamageRequest(ctx.Source, v.Id, _power, _element, isSkill: true,
                        ignoresArmor: _ignoresArmor, hitFrom: caster.Position);
                    ctx.State.Damage.Apply(req, ctx.State);
                }

                if (_statusFactory != null && v.IsAlive)
                {
                    v.Statuses.Add(_statusFactory(), new StatusContext(v, ctx.Events, ctx.Units));
                }
            }

            if (!_spawnTerrain.Equals(TerrainId.None) && !string.IsNullOrEmpty(_spawnTerrain.Value))
            {
                for (int i = 0; i < _cells.Count; i++)
                {
                    var c = _cells[i];
                    if (ctx.State.Grid.InBounds(c))
                    {
                        ctx.State.Grid.SetTerrain(c, _spawnTerrain, _terrainFlags, 1, _hazardDmg);
                        if (_collapseTurns > 0)
                        {
                            ctx.State.Grid.ArmCollapse(c, _collapseTurns);
                        }
                    }
                }
            }

            ctx.Events?.Publish(new SkillUsedEvent(ctx.Source, ctx.Skill?.Id ?? "boss-skill", ctx.TargetCell));
        }
    }

    /// <summary>Di chuyển AI 1 bước về phía mục tiêu nếu không bị Root/Stagger.</summary>
    public sealed class StepTowardsEffect : IGameEffect
    {
        private readonly GridCoord _destination;

        public StepTowardsEffect(GridCoord destination)
        {
            _destination = destination;
        }

        public int Priority => 5;

        public void Resolve(EffectContext ctx)
        {
            if (!_destination.IsValid || !ctx.Units.TryGet(ctx.Source, out var self) ||
                !self.IsAlive || self.IsStaggered || self.IsRooted)
            {
                return;
            }

            if (ctx.State.Grid.InBounds(_destination) &&
                ctx.Units.CanPlace(self.Definition, _destination, self.Id))
            {
                var from = self.Position;
                ctx.Units.MoveTo(self.Id, _destination);
                ctx.Events?.Publish(new UnitMovedEvent(self.Id, from, _destination, 1));
            }
        }
    }

    public sealed class TacticalBossBrain : IEnemyBrain
    {
        public string Id { get; }
        private readonly string _primarySkillId;
        private readonly string _secondarySkillId;
        private readonly int _primaryPower;
        private readonly int _secondaryPower;
        private readonly ElementId _element;
        private readonly bool _useCrossOnEvenTurn;
        private readonly bool _useRowOnOddTurn;
        private readonly TerrainId _evenTerrain;
        private readonly TerrainFlags _evenFlags;
        private readonly int _evenHazard;
        private readonly int _evenCollapse;
        private readonly System.Func<IStatusEffect> _hitStatus;

        public TacticalBossBrain(
            string id,
            string primarySkillId,
            string secondarySkillId,
            int primaryPower,
            int secondaryPower,
            ElementId element,
            bool useCrossOnEvenTurn = true,
            bool useRowOnOddTurn = false,
            TerrainId evenTerrain = default,
            TerrainFlags evenFlags = TerrainFlags.None,
            int evenHazard = 0,
            int evenCollapse = 0,
            System.Func<IStatusEffect> hitStatus = null)
        {
            Id = id;
            _primarySkillId = primarySkillId;
            _secondarySkillId = secondarySkillId;
            _primaryPower = primaryPower;
            _secondaryPower = secondaryPower;
            _element = element;
            _useCrossOnEvenTurn = useCrossOnEvenTurn;
            _useRowOnOddTurn = useRowOnOddTurn;
            _evenTerrain = evenTerrain;
            _evenFlags = evenFlags;
            _evenHazard = evenHazard;
            _evenCollapse = evenCollapse;
            _hitStatus = hitStatus;
        }

        public EnemyIntent Plan(UnitState self, GameState state)
        {
            var player = state.Units.Player;
            if (self == null || !self.IsAlive || player == null || !player.IsAlive)
            {
                return EnemyIntent.Empty(self?.Id ?? UnitId.None);
            }

            bool evenTurn = (state.Turn.TurnIndex % 2) == 0;
            var targetCell = player.Position;
            var danger = new List<GridCoord>();

            if (evenTurn && _useCrossOnEvenTurn)
            {
                var raw = GridShapes.Cross(targetCell, 1);
                for (int i = 0; i < raw.Count; i++)
                {
                    if (state.Grid.InBounds(raw[i]) && !state.Grid.Get(raw[i]).BlocksLineOfSight)
                    {
                        danger.Add(raw[i]);
                    }
                }
            }
            else if (!evenTurn && _useRowOnOddTurn)
            {
                int dx = targetCell.X - self.Center.X;
                int dy = targetCell.Y - self.Center.Y;
                int dirX = System.Math.Abs(dx) >= System.Math.Abs(dy) ? System.Math.Sign(dx) : 0;
                int dirY = dirX == 0 ? (dy >= 0 ? 1 : -1) : 0;
                if (dirX == 0 && dirY == 0) dirX = -1;
                var raw = GridShapes.Row(self.Center, dirX, dirY, 4);
                for (int i = 0; i < raw.Count; i++)
                {
                    if (state.Grid.InBounds(raw[i]))
                    {
                        danger.Add(raw[i]);
                        if (state.Grid.Get(raw[i]).BlocksLineOfSight) break;
                    }
                }
                if (danger.Count == 0 && state.Grid.InBounds(targetCell))
                {
                    danger.Add(targetCell);
                }
            }
            else
            {
                danger.Add(targetCell);
                var adj = GridShapes.Neighbours4(targetCell);
                for (int i = 0; i < adj.Count; i++)
                {
                    if (state.Grid.InBounds(adj[i]) && GridCoord.Manhattan(adj[i], self.Center) <= 2)
                    {
                        danger.Add(adj[i]);
                        break;
                    }
                }
            }

            var effects = new List<IGameEffect>();
            GridCoord step = ChooseStepTowards(self, player.Position, state);
            if (step.IsValid && step != self.Position)
            {
                effects.Add(new StepTowardsEffect(step));
            }

            string skillId = evenTurn ? _primarySkillId : _secondarySkillId;
            int power = evenTurn ? _primaryPower : _secondaryPower;
            effects.Add(new TelegraphedZoneEffect(
                danger,
                power,
                _element,
                ignoresArmor: _element == ElementId.Piercing,
                statusFactory: evenTurn ? _hitStatus : null,
                spawnTerrain: evenTurn ? _evenTerrain : TerrainId.None,
                terrainFlags: evenTurn ? _evenFlags : TerrainFlags.None,
                hazardDmg: evenTurn ? _evenHazard : 0,
                collapseTurns: evenTurn ? _evenCollapse : 0));

            return new EnemyIntent(self.Id, skillId, targetCell, danger, effects, self.Definition.Initiative);
        }

        private static GridCoord ChooseStepTowards(UnitState self, GridCoord goal, GameState state)
        {
            if (GridCoord.Manhattan(self.Center, goal) <= 1)
            {
                return self.Position;
            }

            GridCoord best = self.Position;
            int bestDist = GridCoord.Manhattan(self.Position, goal);
            for (int i = 0; i < GridShapes.Directions4.Length; i++)
            {
                var cand = self.Position + GridShapes.Directions4[i];
                if (!state.Grid.InBounds(cand)) continue;
                var cell = state.Grid.Get(cand);
                if (cell.IsHazard && !self.Move.IgnoresHazard) continue;
                if (!state.Units.CanPlace(self.Definition, cand, self.Id)) continue;

                int d = GridCoord.Manhattan(cand, goal);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cand;
                }
            }
            return best;
        }
    }

    public static class BrainCatalog
    {
        public static EnemyBrainLibrary CreateLibrary()
        {
            var lib = new EnemyBrainLibrary();

            lib.Register(new TacticalBossBrain(
                "brain-melee", "imp-poke", "imp-poke", 2, 2, ElementId.Fire,
                useCrossOnEvenTurn: false, useRowOnOddTurn: false));

            lib.Register(new TacticalBossBrain(
                "brain-ranged", "gargoyle-bolt", "gargoyle-bolt", 3, 3, ElementId.Physical,
                useCrossOnEvenTurn: false, useRowOnOddTurn: true));

            lib.Register(new TacticalBossBrain(
                "brain-ally", "holy-sword-slash", "holy-pillar", 5, 5, ElementId.Holy,
                useCrossOnEvenTurn: true, useRowOnOddTurn: false));

            // Boss 1: Đao Phủ Tro Tàn — Chẵn: Earth Crack (chữ thập + làm nứt sàn 2 lượt sập), Lẻ: Bổ Rìu
            lib.Register(new TacticalBossBrain(
                "brain-boss-ashen", "earth-crack", "axe-cleave", 5, 4, ElementId.Physical,
                useCrossOnEvenTurn: true, useRowOnOddTurn: false,
                evenTerrain: TerrainId.Cracked, evenFlags: TerrainFlags.None, evenHazard: 0, evenCollapse: 2));

            // Boss 2: Vu Nữ Morbida — Chẵn: Grave Hands (chữ thập + đầm độc 3 HP + khóa chân Root), Lẻ: Độc Vũ
            lib.Register(new TacticalBossBrain(
                "brain-boss-morbida", "grave-hands", "poison-dance", 4, 4, ElementId.Necrotic,
                useCrossOnEvenTurn: true, useRowOnOddTurn: false,
                evenTerrain: TerrainId.Poison, evenFlags: TerrainFlags.IsHazard, evenHazard: 3, evenCollapse: 0,
                hitStatus: () => new RootStatus()));

            // Boss 3: Kỵ Sĩ Hư Không — Chẵn: Tia Thương Hư Không xuyên hàng 4 ô, Lẻ: Thương Đâm
            lib.Register(new TacticalBossBrain(
                "brain-boss-abyss", "void-spear-beam", "spear-thrust", 5, 4, ElementId.Piercing,
                useCrossOnEvenTurn: false, useRowOnOddTurn: true));

            // Boss 4: Quỷ Vương Malkor (2x2) — Chẵn: Brimstone Meteor (chữ thập + sập sàn 1 lượt), Lẻ: Cự Kiếm Hỏa Ngục (hàng lửa)
            lib.Register(new TacticalBossBrain(
                "brain-boss-malkor", "brimstone-meteor", "hellfire-greatsword", 6, 6, ElementId.Fire,
                useCrossOnEvenTurn: true, useRowOnOddTurn: true,
                evenTerrain: TerrainId.Cracked, evenFlags: TerrainFlags.None, evenHazard: 0, evenCollapse: 1));

            // Boss 5: Eosphoros Sa Ngã — Chẵn: Holy Pillar (chữ thập thánh quang), Lẻ: Thánh Quang Trảm (tia hàng dọc/ngang)
            lib.Register(new TacticalBossBrain(
                "brain-boss-fallen", "holy-pillar", "holy-light-slash", 6, 5, ElementId.Holy,
                useCrossOnEvenTurn: true, useRowOnOddTurn: true));

            return lib;
        }
    }
}
