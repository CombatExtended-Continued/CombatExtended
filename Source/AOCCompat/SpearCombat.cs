using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AOCSpearForms;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AOC17CE
{
    // Replace the constructor at AOC's short-spear passive call site only.
    // Natural weather lightning and the long spear's visual event are untouched.
    [HarmonyPatch]
    public static class ShortSpearLightningCE
    {
        static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.TypeByName("AOCSpearForms.CompForms"), "RegisterPassiveHit");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Constructor(typeof(WeatherEvent_LightningStrike),
                new[] { typeof(Map), typeof(IntVec3) });
            var replacement = AccessTools.Constructor(typeof(WeatherEvent_ShortSpearLightningCE),
                new[] { typeof(Map), typeof(IntVec3), typeof(Pawn) });
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Newobj && Equals(instruction.operand, original))
                {
                    // Existing stack: map, cell. Parameter 1 is the attacking pawn.
                    // Move branch labels onto the load so branches cannot skip it.
                    var userLoad = new CodeInstruction(instruction);
                    userLoad.opcode = OpCodes.Ldarg_1;
                    userLoad.operand = null;
                    yield return userLoad;
                    yield return new CodeInstruction(OpCodes.Newobj, replacement);
                    replaced++;
                }
                else yield return instruction;
            }
            if (replaced != 1)
                throw new InvalidOperationException("AOC short-spear lightning: expected one constructor site, found " + replaced);
        }
    }

    public class WeatherEvent_ShortSpearLightningCE : WeatherEvent_LightningStrike
    {
        public const float EMPRadius = 1.9f;
        public const int EMPDamage = 5;
        private readonly Map context;
        private readonly IntVec3 cell;
        private readonly Pawn attacker;
        [ThreadStatic] internal static WeatherEvent_ShortSpearLightningCE active;

        internal static void ExcludeCaster(IntVec3 center, Map map, DamageDef damage,
            ref List<Thing> ignoredThings)
        {
            var current = active;
            if (current?.attacker == null || map != current.context || center != current.cell ||
                damage != DamageDefOf.Flame) return;
            // Copy an existing exclusion list rather than modifying caller data.
            ignoredThings = ignoredThings == null ? new List<Thing>() : new List<Thing>(ignoredThings);
            if (!ignoredThings.Contains(current.attacker)) ignoredThings.Add(current.attacker);
        }

        public WeatherEvent_ShortSpearLightningCE(Map map, IntVec3 strikeCell, Pawn user)
            : base(map, strikeCell)
        {
            context = map;
            cell = strikeCell;
            attacker = user;
        }

        public override void FireEvent()
        {
            // Preserve native visuals/fire while carrying the caster exclusion
            // into the explosion object, whose damage may occur after this call.
            var previous = active;
            active = this;
            try { base.FireEvent(); }
            finally { active = previous; }
            ApplyEMP();
        }

        private void ApplyEMP()
        {
            if (context == null || !cell.InBounds(context) || cell.Fogged(context)) return;
            GenExplosionCE.DoExplosion(cell, context, EMPRadius, DamageDefOf.EMP, attacker,
                damAmount: EMPDamage, armorPenetration: DamageDefOf.EMP.defaultArmorPenetration,
                weapon: DefDatabase<ThingDef>.GetNamedSilentFail("AOC_MonoSpear"),
                ignoredThings: attacker == null ? null : new List<Thing> { attacker },
                doVisualEffects: false, doSoundEffects: false);
        }
    }

    [HarmonyPatch(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))]
    public static class ShortSpearFlameExclusion
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(IntVec3 center, Map map, DamageDef damType,
            ref List<Thing> ignoredThings)
        {
            WeatherEvent_ShortSpearLightningCE.ExcludeCaster(center, map, damType, ref ignoredThings);
        }
    }

    // Full CE melee resolution for the long spear skill. Keep its original
    // sequence/save fields and original push routine, replacing only the strike.
    public static class PierceSequenceCE
    {
        public static bool Tick(object instance, ref int nextTick, ref bool done)
        {
            var sequence = (CompFormSequence)instance;
            var settings = (CompProperties_FormSequence)sequence.props;
            var pawn = sequence.Pawn;
            var target = sequence.target;
            var weapon = sequence.weapon;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed ||
                pawn.Position != sequence.origin || pawn.pather.Moving ||
                pawn.equipment?.Primary != weapon || weapon.def != sequence.form ||
                target == null || !target.Spawned || target.Dead || target.Downed ||
                target.Map != pawn.Map || !pawn.Position.InHorDistOf(target.Position, settings.reach) ||
                !GenSight.LineOfSight(pawn.Position, target.Position, pawn.Map))
            {
                done = true; return false;
            }
            if (--nextTick > 0) return false;
            // AOC adds this sequence during the ability cast. CE can still have
            // a busy recovery stance when its first tick arrives. Keep it queued
            // until the stance ends instead of consuming the sequence silently.
            if (pawn.stances.FullBodyBusy) { nextTick = 1; return false; }
            done = true;
            var original = weapon.GetComp<CompEquippable>()?.AllVerbs.OfType<Verb_MeleeAttackCE>()
                .FirstOrDefault(v => v.maneuver != null && v.ToolCE != null &&
                    v.ToolCE.capacities.Any(c => c.defName == "Stab"));
            if (original == null)
            {
                Log.Warning("AOC CE: Pierce cancelled: no CE stab verb on " + weapon.def.defName);
                return false;
            }

            var strike = new Verb_PierceStrikeCE
            {
                caster = pawn, verbTracker = original.verbTracker,
                verbProps = original.verbProps, maneuver = original.maneuver,
                tool = CreateSkillTool(original.ToolCE, settings.damage), reach = settings.reach
            };
            pawn.health.AddHediff(sequence.cooldown);
            // CE still resolves all damage, including parries and misses. The
            // ability's displacement does not depend on that attack result.
            strike.Execute(target);
            if (settings.pushCells > 0)
            {
                // Retains AOC's mech/size exclusions, obstacle checks and flyer.
                AccessTools.Method(typeof(CompFormSequence), "Push").Invoke(null,
                    new object[] { pawn, target, settings.pushCells, settings.maxPushBodySize });
            }
            return false;
        }

        public static ToolCE CreateSkillTool(ToolCE original, float baseDamage)
        {
            var copy = (ToolCE)typeof(object).GetMethod("MemberwiseClone",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(original, null);
            copy.power = baseDamage;
            // CE owns damage/skill/quality/critical multipliers. Preserve both
            // sharp and blunt penetration; never mutate the weapon's shared tool.
            return copy;
        }
    }

    public class Verb_PierceStrikeCE : Verb_MeleeAttackCE
    {
        public float reach;
        public override bool CanHitTarget(LocalTargetInfo target)
        {
            var user = CasterPawn;
            var victim = target.Pawn;
            return user != null && victim != null && user.Spawned && victim.Spawned &&
                user.Map == victim.Map && user.Position.InHorDistOf(victim.Position, reach) &&
                GenSight.LineOfSight(user.Position, victim.Position, user.Map);
        }
        public bool Execute(Pawn target) { currentTarget = target; return TryCastShot(); }
    }
}
