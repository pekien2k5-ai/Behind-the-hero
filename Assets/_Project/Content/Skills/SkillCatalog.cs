using System.Collections.Generic;
using BTH.Core.Combat;
using BTH.Core.Grid;
using BTH.Core.Units;

namespace BTH.Content.Skills
{
    public sealed class DirectDamageBehaviour : ISkillBehaviour
    {
        public int DefaultPower { get; }
        private readonly bool _ignoresArmor;

        public DirectDamageBehaviour(int defaultPower, bool ignoresArmor = false)
        {
            DefaultPower = defaultPower;
            _ignoresArmor = ignoresArmor;
        }

        public void BuildEffects(EffectContext ctx)
        {
            int p = ctx.Skill != null && ctx.Skill.BoardPower > 0 ? ctx.Skill.BoardPower : DefaultPower;
            var targetId = ctx.Target.IsValid ? ctx.Target : (ctx.UnitAt(ctx.TargetCell)?.Id ?? UnitId.None);
            if (targetId.IsValid)
            {
                ctx.Queue.Enqueue(new DamageEffect(targetId, p, ctx.Element, isSkill: true, ignoresArmor: _ignoresArmor));
            }
        }
    }

    public sealed class GuardSkillBehaviour : ISkillBehaviour
    {
        public int DefaultPower => 0;

        public void BuildEffects(EffectContext ctx)
        {
            ctx.Queue.Enqueue(new ApplyStatusEffect(ctx.Source, new GuardStatus()));
        }
    }

    public sealed class TemporalStepBehaviour : ISkillBehaviour
    {
        public int DefaultPower => 0;

        public void BuildEffects(EffectContext ctx)
        {
            ctx.Queue.Enqueue(new TeleportEffect(ctx.Source, ctx.TargetCell));
            ctx.Queue.Enqueue(new ApplyStatusEffect(ctx.Source, new GuardStatus()));
        }
    }

    public sealed class CrossAoEBehaviour : ISkillBehaviour
    {
        public int DefaultPower { get; }
        private readonly int _radius;
        private readonly TerrainId _spawnTerrain;
        private readonly TerrainFlags _terrainFlags;
        private readonly int _hazardDmg;
        private readonly int _collapseTurns;

        public CrossAoEBehaviour(int defaultPower, int radius = 1, TerrainId spawnTerrain = default,
            TerrainFlags terrainFlags = TerrainFlags.None, int hazardDmg = 0, int collapseTurns = 0)
        {
            DefaultPower = defaultPower;
            _radius = radius;
            _spawnTerrain = spawnTerrain;
            _terrainFlags = terrainFlags;
            _hazardDmg = hazardDmg;
            _collapseTurns = collapseTurns;
        }

        public void BuildEffects(EffectContext ctx)
        {
            int p = ctx.Skill != null && ctx.Skill.BoardPower > 0 ? ctx.Skill.BoardPower : DefaultPower;
            var cells = GridShapes.Cross(ctx.TargetCell, _radius);
            foreach (var u in ctx.Units.UnitsIn(cells))
            {
                if (u.Id != ctx.Source && u.IsAlive)
                {
                    ctx.Queue.Enqueue(new DamageEffect(u.Id, p, ctx.Element, isSkill: true));
                }
            }
            if (!_spawnTerrain.Equals(TerrainId.None) && !string.IsNullOrEmpty(_spawnTerrain.Value))
            {
                foreach (var c in cells)
                {
                    if (ctx.State.Grid.InBounds(c))
                    {
                        ctx.Queue.Enqueue(new SpawnTerrainEffect(c, _spawnTerrain, _terrainFlags, 1, _hazardDmg, _collapseTurns));
                    }
                }
            }
            ctx.Queue.Enqueue(new CameraShakeEffect(0.35f, 0.25f));
        }
    }

    public sealed class GraveHandsBehaviour : ISkillBehaviour
    {
        public int DefaultPower => 4;

        public void BuildEffects(EffectContext ctx)
        {
            int p = ctx.Skill != null && ctx.Skill.BoardPower > 0 ? ctx.Skill.BoardPower : DefaultPower;
            var cells = GridShapes.Cross(ctx.TargetCell, 1);
            foreach (var u in ctx.Units.UnitsIn(cells))
            {
                if (u.Id != ctx.Source && u.IsAlive)
                {
                    ctx.Queue.Enqueue(new DamageEffect(u.Id, p, ElementId.Necrotic, isSkill: true));
                    ctx.Queue.Enqueue(new ApplyStatusEffect(u.Id, new RootStatus()));
                }
            }
            foreach (var c in cells)
            {
                if (ctx.State.Grid.InBounds(c))
                {
                    ctx.Queue.Enqueue(new SpawnTerrainEffect(c, TerrainId.Poison, TerrainFlags.IsHazard, 2, 3, 0));
                }
            }
        }
    }

    public static class SkillCatalog
    {
        public static SkillLibrary CreateLibrary()
        {
            var lib = new SkillLibrary();
            foreach (var s in All())
            {
                lib.Add(s);
            }
            return lib;
        }

        public static IReadOnlyList<SkillDefinition> All()
        {
            var swiftSlash = Make("swift-slash", "Trảm Tốc (Swift Slash)", SkillTargeting.Unit, 1, 1,
                clashPower: 14, boardPower: 5, ElementId.Physical, ap: 1, cd: 0, isClash: true,
                new DirectDamageBehaviour(5));

            var bladeGuard = Make("blade-guard", "Hộ Kiếm (Blade Guard)", SkillTargeting.Self, 0, 0,
                clashPower: 15, boardPower: 0, ElementId.Physical, ap: 1, cd: 1, isClash: true,
                new GuardSkillBehaviour());

            var protect = Make("protect", "Khiên Hộ Mệnh (Protect)", SkillTargeting.Self, 0, 0,
                clashPower: 15, boardPower: 0, ElementId.Holy, ap: 1, cd: 1, isClash: true,
                new GuardSkillBehaviour());

            var necroticBurst = Make("necrotic-burst", "Bùng Nổ Hoại Tử (Necrotic Burst)", SkillTargeting.Cell, 1, 2,
                clashPower: 13, boardPower: 5, ElementId.Necrotic, ap: 1, cd: 1, isClash: true,
                new CrossAoEBehaviour(5, radius: 1));

            var necroticWard = Make("necrotic-ward", "Kháng Độc Hắc Ám (Necrotic Ward)", SkillTargeting.Self, 0, 0,
                clashPower: 13, boardPower: 0, ElementId.Necrotic, ap: 1, cd: 1, isClash: true,
                new GuardSkillBehaviour());

            var temporalStep = Make("temporal-step", "Bước Nhảy Thời Gian (Temporal Step)", SkillTargeting.Cell, 1, 3,
                clashPower: 16, boardPower: 0, ElementId.Temporal, ap: 1, cd: 2, isClash: true,
                new TemporalStepBehaviour());

            var dodge = Make("dodge", "Lách Mình (Dodge)", SkillTargeting.Self, 0, 0,
                clashPower: 14, boardPower: 0, ElementId.Temporal, ap: 1, cd: 1, isClash: true,
                new GuardSkillBehaviour());

            // Boss 1: Đao Phủ Tro Tàn
            var ashenFury = Make("ashen-fury", "Tro Tàn Cuồng Nộ", SkillTargeting.Unit, 1, 2,
                clashPower: 15, boardPower: 6, ElementId.Physical, ap: 1, cd: 1, isClash: false,
                new DirectDamageBehaviour(6));
            var axeCleave = Make("axe-cleave", "Bổ Rìu", SkillTargeting.Unit, 1, 1,
                clashPower: 10, boardPower: 5, ElementId.Physical, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(5));
            var earthCrack = Make("earth-crack", "Rìu Chấn Động (Earth Crack)", SkillTargeting.Cell, 1, 2,
                clashPower: 12, boardPower: 5, ElementId.Physical, ap: 1, cd: 2, isClash: false,
                new CrossAoEBehaviour(5, 1, TerrainId.Cracked, TerrainFlags.None, 0, 2));

            // Boss 2: Vu Nữ Morbida
            var plagueStorm = Make("plague-storm", "Bão Khí Độc Diệt Thế", SkillTargeting.Cell, 1, 3,
                clashPower: 14, boardPower: 5, ElementId.Necrotic, ap: 1, cd: 1, isClash: false,
                new CrossAoEBehaviour(5, 1, TerrainId.Poison, TerrainFlags.IsHazard, 3, 0));
            var poisonDance = Make("poison-dance", "Độc Vũ", SkillTargeting.Unit, 1, 2,
                clashPower: 9, boardPower: 4, ElementId.Necrotic, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(4));
            var graveHands = Make("grave-hands", "Bàn Tay Mộ Cổ (Grave Hands)", SkillTargeting.Cell, 1, 3,
                clashPower: 12, boardPower: 4, ElementId.Necrotic, ap: 1, cd: 2, isClash: false,
                new GraveHandsBehaviour());

            // Boss 3: Kỵ Sĩ Hư Không
            var voidSpearBeam = Make("void-spear-beam", "Tia Thương Hư Không", SkillTargeting.Unit, 1, 3,
                clashPower: 16, boardPower: 6, ElementId.Piercing, ap: 1, cd: 1, isClash: false,
                new DirectDamageBehaviour(6, ignoresArmor: true));
            var spearThrust = Make("spear-thrust", "Thương Đâm", SkillTargeting.Unit, 1, 2,
                clashPower: 11, boardPower: 5, ElementId.Piercing, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(5));
            var eyeShield = Make("eye-shield", "Khiên Mắt Quỷ", SkillTargeting.Self, 0, 0,
                clashPower: 12, boardPower: 0, ElementId.Necrotic, ap: 1, cd: 2, isClash: false,
                new GuardSkillBehaviour());

            // Boss 4: Quỷ Vương Malkor
            var hellfireGreatsword = Make("hellfire-greatsword", "Cự Kiếm Hỏa Ngục", SkillTargeting.Cell, 1, 3,
                clashPower: 18, boardPower: 7, ElementId.Fire, ap: 1, cd: 1, isClash: false,
                new CrossAoEBehaviour(7, 1, TerrainId.Lava, TerrainFlags.IsHazard, 4, 0));
            var fireball = Make("fireball", "Hỏa Cầu", SkillTargeting.Unit, 1, 4,
                clashPower: 12, boardPower: 6, ElementId.Fire, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(6));
            var brimstoneMeteor = Make("brimstone-meteor", "Thiên Thạch Hỏa Ngục (Brimstone)", SkillTargeting.Cell, 1, 5,
                clashPower: 16, boardPower: 6, ElementId.Fire, ap: 1, cd: 2, isClash: false,
                new CrossAoEBehaviour(6, 1, TerrainId.Cracked, TerrainFlags.None, 0, 1));

            // Boss 5: Eosphoros Sa Ngã & Ally
            var holyLightSlash = Make("holy-light-slash", "Thánh Quang Trảm", SkillTargeting.Unit, 1, 2,
                clashPower: 18, boardPower: 6, ElementId.Holy, ap: 1, cd: 1, isClash: false,
                new DirectDamageBehaviour(6));
            var holyStrike = Make("holy-strike", "Thánh Kích", SkillTargeting.Unit, 1, 2,
                clashPower: 12, boardPower: 5, ElementId.Holy, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(5));
            var holyPillar = Make("holy-pillar", "Cột Sáng Thiên Phạt (Holy Pillar)", SkillTargeting.Cell, 1, 3,
                clashPower: 15, boardPower: 6, ElementId.Holy, ap: 1, cd: 2, isClash: false,
                new CrossAoEBehaviour(6, 1));
            var holySwordSlash = Make("holy-sword-slash", "Kiếm Thánh Quang", SkillTargeting.Unit, 1, 1,
                clashPower: 14, boardPower: 6, ElementId.Holy, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(6));
            var divineBarrier = Make("divine-barrier", "Khiên Thánh Chữ Thập", SkillTargeting.Self, 0, 0,
                clashPower: 14, boardPower: 0, ElementId.Holy, ap: 1, cd: 2, isClash: false,
                new GuardSkillBehaviour());

            // Quái thường
            var impPoke = Make("imp-poke", "Đinh Ba Lửa", SkillTargeting.Unit, 1, 1,
                clashPower: 6, boardPower: 2, ElementId.Fire, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(2));
            var gargoyleBolt = Make("gargoyle-bolt", "Đạn Đá Hắc Ám", SkillTargeting.Unit, 1, 3,
                clashPower: 8, boardPower: 3, ElementId.Physical, ap: 1, cd: 0, isClash: false,
                new DirectDamageBehaviour(3));

            return new List<SkillDefinition>
            {
                swiftSlash, bladeGuard, protect, necroticBurst, necroticWard, temporalStep, dodge,
                ashenFury, axeCleave, earthCrack,
                plagueStorm, poisonDance, graveHands,
                voidSpearBeam, spearThrust, eyeShield,
                hellfireGreatsword, fireball, brimstoneMeteor,
                holyLightSlash, holyStrike, holyPillar, holySwordSlash, divineBarrier,
                impPoke, gargoyleBolt
            };
        }

        private static SkillDefinition Make(string id, string name, SkillTargeting targeting,
            int rMin, int rMax, int clashPower, int boardPower, ElementId element,
            int ap, int cd, bool isClash, ISkillBehaviour behaviour)
        {
            return new SkillDefinition
            {
                Id = id,
                DisplayName = name,
                Targeting = targeting,
                RangeMin = rMin,
                RangeMax = rMax,
                Power = clashPower,
                BoardPower = boardPower > 0 ? boardPower : behaviour.DefaultPower,
                Element = element,
                ApCost = ap,
                CooldownTurns = cd,
                RequiresLineOfSight = rMax > 1,
                AreaRadius = behaviour is CrossAoEBehaviour || behaviour is GraveHandsBehaviour ? 1 : 0,
                IsClashSkill = isClash,
                Behaviour = behaviour
            };
        }
    }
}
