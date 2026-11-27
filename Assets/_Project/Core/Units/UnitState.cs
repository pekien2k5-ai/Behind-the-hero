using System;
using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;

namespace BTH.Core.Units
{
    /// <summary>
    /// State của một unit trong run. Setter internal: chỉ action/effect/registry được sửa,
    /// UI và AI chỉ đọc qua IGameStateView.
    /// </summary>
    public sealed class UnitState
    {
        private readonly List<GridCoord> _footprint;
        private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>(StringComparer.Ordinal);

        internal UnitState(UnitId id, UnitDefinition definition, GridCoord position)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Id = id;
            Definition = definition;
            Hp = definition.MaxHp;
            Position = position;
            Statuses = new StatusSet();
            _footprint = new List<GridCoord>(Math.Max(1, definition.SizeX) * Math.Max(1, definition.SizeY));
            RebuildFootprint();
        }

        public UnitId Id { get; }

        public UnitDefinition Definition { get; }

        public UnitFaction Faction => Definition.Faction;

        public UnitArchetype Archetype => Definition.Archetype;

        public bool IsBoss => Definition.IsBoss;

        public int MaxHp => Definition.MaxHp;

        public int Hp { get; internal set; }

        public float HpRatio => MaxHp <= 0 ? 0f : (float)Hp / MaxHp;

        public bool IsAlive => Hp > 0;

        /// <summary>Đã phát UnitDiedEvent chưa — Kill gọi nhiều lần không được bắn trùng.</summary>
        internal bool DeathPublished { get; set; }

        public GridCoord Position { get; internal set; }

        public int ActionPoints { get; internal set; }

        public bool HasMovedThisTurn { get; internal set; }

        public bool HasActedThisTurn { get; internal set; }

        public StatusSet Statuses { get; }

        public int SizeX => Definition.SizeX < 1 ? 1 : Definition.SizeX;

        public int SizeY => Definition.SizeY < 1 ? 1 : Definition.SizeY;

        public IReadOnlyList<GridCoord> Footprint => _footprint;

        public GridCoord Center => new GridCoord(Position.X + (SizeX - 1) / 2, Position.Y + (SizeY - 1) / 2);

        public int MovePoints => Definition.Move.MovePoints;

        public MoveProfile Move => Definition.Move;

        public bool IsStaggered => Statuses.IsStaggered;

        public bool IsRooted => Statuses.IsRooted;

        public bool IsGuarding => Statuses.IsGuarding;

        public bool IsFlanked => Statuses.IsFlanked;

        internal void SetPosition(GridCoord position)
        {
            Position = position;
            RebuildFootprint();
        }

        internal void SetHp(int hp)
        {
            Hp = hp < 0 ? 0 : (hp > MaxHp ? MaxHp : hp);
        }

        internal void ResetTurnState()
        {
            HasMovedThisTurn = false;
            HasActedThisTurn = false;
        }

        public int GetCooldown(string skillId)
        {
            return skillId != null && _cooldowns.TryGetValue(skillId, out var turns) ? turns : 0;
        }

        internal void SetCooldown(string skillId, int turns)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            if (turns <= 0)
            {
                _cooldowns.Remove(skillId);
            }
            else
            {
                _cooldowns[skillId] = turns;
            }
        }

        /// <summary>Giảm hồi chiêu theo lượt của chủ thể (gọi ở EndTurn).</summary>
        internal void TickCooldowns()
        {
            if (_cooldowns.Count == 0)
            {
                return;
            }

            var keys = new List<string>(_cooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int next = _cooldowns[keys[i]] - 1;
                if (next <= 0)
                {
                    _cooldowns.Remove(keys[i]);
                }
                else
                {
                    _cooldowns[keys[i]] = next;
                }
            }
        }

        internal Dictionary<string, int> CopyCooldowns()
        {
            return new Dictionary<string, int>(_cooldowns, StringComparer.Ordinal);
        }

        internal void RestoreCooldowns(IReadOnlyDictionary<string, int> cooldowns)
        {
            _cooldowns.Clear();
            if (cooldowns == null)
            {
                return;
            }

            foreach (var pair in cooldowns)
            {
                _cooldowns[pair.Key] = pair.Value;
            }
        }

        public bool InCells(IReadOnlyList<GridCoord> cells)
        {
            if (cells == null)
            {
                return false;
            }

            for (int i = 0; i < _footprint.Count; i++)
            {
                for (int j = 0; j < cells.Count; j++)
                {
                    if (_footprint[i] == cells[j])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void RebuildFootprint()
        {
            _footprint.Clear();
            for (int dx = 0; dx < SizeX; dx++)
            {
                for (int dy = 0; dy < SizeY; dy++)
                {
                    _footprint.Add(new GridCoord(Position.X + dx, Position.Y + dy));
                }
            }
        }

        public override string ToString()
        {
            return Definition.Id + "#" + Id.Value + " " + Position;
        }
    }
}
