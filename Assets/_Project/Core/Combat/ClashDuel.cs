using System;
using System.Collections.Generic;
using BTH.Core.Run;
using BTH.Core.Units;

namespace BTH.Core.Combat
{
    /// <summary>
    /// Trận Duel sau khi boss xuống dưới 70% HP (Docs/QUYET_DINH.md):
    /// chỉ chọn chiêu Clash mỗi vòng; thắng → gắn hiệu ứng + đòn kết liễu;
    /// hoà → mỗi bên mất 10% uy lực; thua → ăn đòn BossPower.
    /// </summary>
    public sealed class ClashDuel : IClashDuel
    {
        private readonly GameState _state;
        private DuelDefinition _definition;
        private DuelState _duel;
        private UnitState _player;
        private UnitState _boss;

        public ClashDuel(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public DuelState State => _duel;

        public DuelDefinition Definition => _definition;

        public bool IsActive => _duel != null;

        public void Start(GameState state, DuelDefinition definition, UnitState player, UnitState boss)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (player == null || boss == null)
            {
                throw new ArgumentNullException(player == null ? nameof(player) : nameof(boss));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (definition.BossSkillIds == null || definition.BossSkillIds.Count == 0)
            {
                throw new InvalidOperationException("Duel cần ít nhất một chiêu của boss.");
            }

            if (definition.PlayerSkillIds == null || definition.PlayerSkillIds.Count == 0)
            {
                throw new InvalidOperationException("Duel cần ít nhất một chiêu cho người chơi.");
            }

            if (_state.Skills == null)
            {
                throw new InvalidOperationException("Duel cần ISkillLibrary.");
            }

            _definition = definition;
            _player = player;
            _boss = boss;
            _duel = new DuelState
            {
                DuelId = definition.Id ?? string.Empty
            };

            // Chốt A7: status cũ (Flanked/Guard/Poison) không mang vào Duel.
            _player.Statuses.Clear();
            _boss.Statuses.Clear();

            _state.Events.Publish(new ClashStartedEvent(_boss.Id, definition.BossSkillIds[0]));
            EnterRound(1);
        }

        public IReadOnlyList<ClashOption> GetOptions()
        {
            var options = new List<ClashOption>();
            if (_duel == null || _duel.IsOver || _duel.AwaitingFinisher || _definition == null ||
                _state.Skills == null)
            {
                return options;
            }

            for (int i = 0; i < _definition.PlayerSkillIds.Count; i++)
            {
                string id = _definition.PlayerSkillIds[i];
                if (!_state.Skills.TryGet(id, out SkillDefinition skill) || skill == null || !skill.IsClashSkill)
                {
                    continue;
                }

                int bonus = BonusFor(skill);
                int power = ClashMath.EffectivePower(skill, bonus);
                ClashResult result = ClashMath.Compare(power, _duel.BossPower);
                var prediction = new ClashPrediction(result, power, _duel.BossPower, bonus);
                options.Add(new ClashOption(skill.Id, skill.DisplayName, power, prediction));
            }

            return options;
        }

        public ClashRoundResult Choose(string playerSkillId)
        {
            if (_duel == null)
            {
                throw new InvalidOperationException("Duel chưa bắt đầu.");
            }

            if (_duel.IsOver)
            {
                throw new InvalidOperationException("Duel đã kết thúc.");
            }

            if (_duel.AwaitingFinisher)
            {
                throw new InvalidOperationException("Phải đánh kết liễu trước khi chọn chiêu tiếp.");
            }

            if (_state.Skills == null || !_state.Skills.TryGet(playerSkillId, out SkillDefinition skill) ||
                skill == null || !skill.IsClashSkill)
            {
                throw new ArgumentException("Chiêu không phải chiêu Clash: " + playerSkillId, nameof(playerSkillId));
            }

            int bonus = BonusFor(skill);
            int playerPower = ClashMath.EffectivePower(skill, bonus);
            int bossPower = _duel.BossPower;
            ClashResult result = ClashMath.Compare(playerPower, bossPower);

            IStatusEffect applied = null;
            int damageToPlayer = 0;
            int damageToBoss = 0;
            bool advanceRound = true;

            switch (result)
            {
                case ClashResult.Win:
                    applied = _definition.CreateWinEffect();
                    if (applied != null && _boss.IsAlive)
                    {
                        AddStatus(_boss, applied);
                    }

                    _duel.AwaitingFinisher = true;
                    _duel.FinishersRemaining = Math.Max(1, _definition.FinisherCount);
                    advanceRound = false;
                    break;

                case ClashResult.Draw:
                    damageToPlayer = ChipDamage(bossPower);
                    damageToBoss = ChipDamage(playerPower);
                    int dealtToPlayer = ApplyDirectDamage(_boss, _player, damageToPlayer);
                    int dealtToBoss = ApplyDirectDamage(_player, _boss, damageToBoss);
                    damageToPlayer = dealtToPlayer;
                    damageToBoss = dealtToBoss;
                    break;

                case ClashResult.Lose:
                    damageToPlayer = ApplyDirectDamage(_boss, _player, bossPower);
                    applied = _definition.CreateLoseEffect();
                    if (applied != null && _player.IsAlive)
                    {
                        AddStatus(_player, applied);
                    }

                    break;
            }

            _duel.LastResult = result;
            _state.Events.Publish(new ClashResolvedEvent(_boss.Id, skill.Id, result, playerPower, bossPower));

            var round = new ClashRoundResult(result, playerPower, bossPower, damageToPlayer, damageToBoss, applied);
            CheckDeaths();
            if (!_duel.IsOver && advanceRound)
            {
                EnterRound(_duel.Round + 1);
            }

            return round;
        }

        public bool TryFinisher(out DamageResult damage)
        {
            damage = default;
            if (_duel == null || !_duel.AwaitingFinisher || _player == null || _boss == null ||
                _state.Damage == null)
            {
                return false;
            }

            bool any = false;
            while (_duel.FinishersRemaining > 0 && _boss.IsAlive && _player.IsAlive)
            {
                var request = new DamageRequest(
                    _player.Id,
                    _boss.Id,
                    _player.Definition.Attack,
                    _player.Definition.NativeElement,
                    false,
                    false,
                    _player.Center);
                damage = _state.Damage.Apply(request, _state);
                any = true;
                _duel.FinishersRemaining--;
                if (damage.Killed)
                {
                    break;
                }
            }

            _duel.AwaitingFinisher = false;
            if (!any)
            {
                return false;
            }

            CheckDeaths();
            if (!_duel.IsOver)
            {
                EnterRound(_duel.Round + 1);
            }

            return true;
        }

        public DuelSnapshot Capture()
        {
            if (_duel == null)
            {
                return null;
            }

            return new DuelSnapshot(
                _duel.DuelId,
                _duel.Round,
                _duel.BossSkillIndex,
                _duel.BossSkillId,
                _duel.BossPower,
                _duel.BossElement,
                _duel.AwaitingFinisher,
                _duel.FinishersRemaining,
                _duel.LastResult,
                _duel.IsOver,
                _duel.Winner,
                _duel.SeenBossSkills);
        }

        public void Restore(DuelSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Clear();
                return;
            }

            _definition = _state.Stage != null ? _state.Stage.Duel : null;
            _player = _state.Units.Player;
            _boss = _state.Units.Boss;
            _duel = new DuelState
            {
                DuelId = snapshot.DuelId,
                Round = snapshot.Round,
                BossSkillIndex = snapshot.BossSkillIndex,
                BossSkillId = snapshot.BossSkillId,
                BossPower = snapshot.BossPower,
                BossElement = snapshot.BossElement,
                AwaitingFinisher = snapshot.AwaitingFinisher,
                FinishersRemaining = snapshot.FinishersRemaining,
                LastResult = snapshot.LastResult,
                IsOver = snapshot.IsOver,
                Winner = snapshot.Winner
            };
            _duel.RestoreSeen(snapshot.SeenBossSkills);
        }

        public void Clear()
        {
            _duel = null;
            _player = null;
            _boss = null;
        }

        private void EnterRound(int round)
        {
            if (_definition.RoundLimit > 0 && round > _definition.RoundLimit)
            {
                End(UnitId.None, ClashResult.Draw);
                return;
            }

            _duel.Round = round;
            int index = (round - 1) % _definition.BossSkillIds.Count;
            string bossSkillId = _definition.BossSkillIds[index];

            if (!_state.Skills.TryGet(bossSkillId, out SkillDefinition bossSkill) || bossSkill == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chiêu boss: " + bossSkillId);
            }

            _duel.BossSkillIndex = index;
            _duel.BossSkillId = bossSkillId;
            _duel.BossPower = bossSkill.Power;
            _duel.BossElement = bossSkill.Element;
            _duel.AddSeen(bossSkillId);
            _state.Knowledge?.MarkSeen(RunKnowledge.BossSkillSeenPrefix + bossSkillId);

            _state.Events.Publish(new DuelRoundStartedEvent(round, bossSkillId, bossSkill.Power));
        }

        private void CheckDeaths()
        {
            if (_duel == null || _duel.IsOver)
            {
                return;
            }

            if (_player != null && !_player.IsAlive)
            {
                End(_boss != null ? _boss.Id : UnitId.None, _duel.LastResult);
                return;
            }

            if (_boss != null && !_boss.IsAlive)
            {
                End(_player != null ? _player.Id : UnitId.None, _duel.LastResult);
            }
        }

        private void End(UnitId winner, ClashResult lastResult)
        {
            _duel.IsOver = true;
            _duel.Winner = winner;
            _duel.AwaitingFinisher = false;
            _duel.LastResult = lastResult;
            _state.Events.Publish(new DuelEndedEvent(lastResult, winner));
        }

        private int BonusFor(SkillDefinition skill)
        {
            if (_definition.Rules == null || _duel == null)
            {
                return 3; // ClashRuleTable mặc định cộng 3 khi không có bảng luật riêng.
            }

            return _definition.Rules.GetBonus(skill.Id, _duel.BossSkillId, skill.Element, _duel.BossElement);
        }

        private int ChipDamage(int power)
        {
            if (power <= 0)
            {
                return 0;
            }

            return (int)Math.Round(power * _definition.DrawChipRatio);
        }

        private int ApplyDirectDamage(UnitState source, UnitState target, int amount)
        {
            if (amount <= 0 || target == null || !target.IsAlive || _state.Damage == null)
            {
                return 0;
            }

            var request = new DamageRequest(
                source != null ? source.Id : UnitId.None,
                target.Id,
                amount,
                ElementId.None,
                false,
                true,
                source != null ? source.Center : target.Position);
            return _state.Damage.Apply(request, _state).FinalDamage;
        }

        private void AddStatus(UnitState target, IStatusEffect effect)
        {
            target.Statuses.Add(effect, new StatusContext(target, _state.Events, _state.Units));
        }
    }
}
