using System;
using System.Linq;
using System.Reflection;
using AOCA2EVacuum;
using AOCSpearForms;
using AOCB2H;
using AOCTCB;
using CombatExtended;
using CombatExtended.Compatibility;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AOC17CE
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(Intake);
            new Harmony("local.aoc17.ce.compatibility").PatchAll(typeof(Startup).Assembly);
            // CE can load before AOC; apply the unique localized description
            // after language injection so AOC's old "12 bullets" text cannot win.
            LongEventHandler.ExecuteWhenFinished(UpdateIntakeDescription);
        }

        private static void UpdateIntakeDescription()
        {
            var ability = DefDatabase<AbilityDef>.GetNamedSilentFail("AOC_A2EIntake");
            if (ability != null) ability.description = "AOC17CE_UnlimitedIntakeDescription".Translate();
            var shortSpear = DefDatabase<ThingDef>.GetNamedSilentFail("AOC_MonoSpear");
            string empNote = "AOC17CE_ShortSpearEMPNote".Translate();
            if (shortSpear != null && !(shortSpear.description ?? "").Contains(empNote))
                shortSpear.description += "\n\n" + empNote;
        }

        // CE projectiles are not Verse.Projectile. Use CE's collision callback and
        // the original AOC sector, fuel/charge counter, waste provenance and sound.
        public static bool Intake(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            if (!(projectile is BulletCE) || projectile.Destroyed || projectile.Map == null ||
                projectile.landed || HediffComp_Intake.Active.Count == 0) return false;
            foreach (var intake in HediffComp_Intake.Active)
            {
                var pawn = intake.Pawn;
                if (!intake.IsActive || pawn.Map != projectile.Map || projectile.launcher == pawn) continue;
                Vector3 hit;
                if (!Sector.Intersects(from, to, pawn.DrawPos, intake.Direction,
                    intake.Settings.radius, intake.Settings.halfAngle, out hit)) continue;
                if (!IntakeWaste.Ensure(intake)) continue;
                intake.count++;
                intake.PlayBlockSound();
                FleckMaker.Static(hit, pawn.Map, FleckDefOf.MicroSparks, 0.5f);
                projectile.Destroy();
                return true;
            }
            return false;
        }
    }

    public static class IntakeWaste
    {
        // Native count is already saved with this activation's Hediff. Emit on
        // its first successful interception only, including after save/reload.
        // Normal vacuum cleaning never calls this helper and remains unchanged.
        public static bool Ensure(object instance)
        {
            return EnsureWithEmitter(instance, Waste.Emit);
        }

        internal static bool EnsureWithEmitter(object instance,
            Func<Map, IntVec3, int, bool> emit)
        {
            var intake = (HediffComp_Intake)instance;
            if (intake.count != 0) return true;
            var pawn = intake.Pawn;
            return emit(pawn.Map, pawn.Position, 1);
        }
    }

    // Keep the same one-bag policy if an unconverted vanilla Bullet reaches AOC's
    // own interceptor. Patch its entry point, not the shared cleaning emitter.
    [HarmonyPatch]
    public static class NativeIntakeWasteOnce
    {
        static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.TypeByName("AOCA2EVacuum.ProjectileIntercept"), "Prefix");

        public static bool Prefix(Projectile __0, Vector3 __1, Vector3 __2,
            ref bool __3, ref bool __result)
        {
            __result = true;
            if (!(__0 is Bullet) || __0.Map == null) return false;
            foreach (var intake in HediffComp_Intake.Active.ToArray())
            {
                var pawn = intake.Pawn;
                Vector3 hit;
                if (!intake.IsActive || pawn.Map != __0.Map || __0.Launcher == pawn ||
                    !Sector.Intersects(__1, __2, pawn.DrawPos, intake.Direction,
                        intake.Settings.radius, intake.Settings.halfAngle, out hit) ||
                    !IntakeWaste.Ensure(intake)) continue;
                intake.count++;
                intake.PlayBlockSound();
                var motion = (pawn.DrawPos + intake.Direction * 0.35f - hit).Yto0();
                if (motion.sqrMagnitude > 0.01f)
                {
                    var fleck = FleckMaker.GetDataStatic(hit, pawn.Map, FleckDefOf.MicroSparks, 0.5f);
                    fleck.velocityAngle = motion.AngleFlat();
                    fleck.velocitySpeed = Mathf.Clamp(motion.magnitude / 0.25f, 2f, 15f);
                    fleck.airTimeLeft = 0.25f;
                    pawn.Map.flecks.CreateFleck(fleck);
                }
                __0.Destroy();
                __3 = true;
                __result = false;
                return false;
            }
            return false;
        }
    }

    // The native getter also ends the Hediff at maxIntercepts, so removing a
    // check from the CE callback alone would still stop the shield after 12 hits.
    // Keep every native validity condition except the counter comparison. The
    // unchanged Disappears comp remains responsible for the 300-tick lifetime.
    [HarmonyPatch]
    public static class UnlimitedIntake
    {
        static MethodBase TargetMethod() => AccessTools.PropertyGetter(
            AccessTools.TypeByName("AOCA2EVacuum.HediffComp_Intake"), "IsActive");

        public static bool Prefix(object __instance, ref bool __result)
        {
            var intake = (HediffComp_Intake)__instance;
            var pawn = intake.Pawn;
            __result = pawn != null && pawn.Spawned && !pawn.Dead && !pawn.Downed &&
                pawn.equipment?.Primary?.def.defName == "AOC_A2E";
            return false;
        }
    }

    // The CE verb overrides TryCastShot, so AOC's patch of the vanilla method
    // does not run. Forward successful CE attacks to the original passive code.
    [HarmonyPatch(typeof(Verb_MeleeAttackCE), nameof(Verb_MeleeAttackCE.TryCastShot))]
    public static class SpearPassiveCE
    {
        public static void Postfix(Verb_MeleeAttackCE __instance, bool __result)
        {
            PassiveLightningOnHit.Postfix(__instance, __result);
        }
    }

    public class Verb_ThrowB2HCE : Verb_ShootCEOneUse { }

    // An apparel-reloadable utility must consume a fuel charge, not the backpack.
    public class Verb_CleanerBackpackCE : Verb_ShootCE
    {
        public override bool Available() => base.Available() && CompReloadable != null && CompReloadable.CanBeUsed(out _);

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            var job = JobMaker.MakeJob(JobDefOf.UseVerbOnThingStaticReserve, target);
            job.verbToUse = this;
            CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    // AOC looks specifically for Verb_MeleeAttackDamage; CE replaces that with
    // Verb_MeleeAttackCE. Preserve the original sequence and push/cooldown rules.
    [HarmonyPatch]
    public static class ScrubSequenceCE
    {
        static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("AOCSkinBrush.CompScrubSequence"), "CompPostTick");

        public static bool Prefix(object __instance, ref int ___nextTick, ref bool ___done)
        {
            var sequence = (AOCSkinBrush.CompScrubSequence)__instance;
            Pawn pawn = sequence.Pawn;
            var target = sequence.target;
            var weapon = sequence.weapon;
            var settings = (AOCSkinBrush.CompProperties_ScrubSequence)sequence.props;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Position != sequence.origin ||
                pawn.pather.Moving || pawn.equipment?.Primary != weapon || target == null ||
                !target.Spawned || target.Dead || target.Map != pawn.Map ||
                !AOCSkinBrush.ScrubReach.CanReach(pawn, target, settings.reach))
            { ___done = true; return false; }
            if (--___nextTick > 0) return false;
            if (pawn.stances.FullBodyBusy) { if (___nextTick <= -90) ___done = true; return false; }
            ___done = true;
            var original = weapon.GetComp<CompEquippable>()?.AllVerbs.OfType<Verb_MeleeAttackCE>()
                .FirstOrDefault(v => v.maneuver != null);
            if (original == null) return false;
            pawn.health.AddHediff(sequence.cooldown);
            pawn.health.AddHediff(sequence.overheat);
            var strike = new Verb_ScrubStrikeCE
            {
                caster = pawn, verbTracker = original.verbTracker, verbProps = original.verbProps,
                tool = original.tool, maneuver = original.maneuver, reach = settings.reach
            };
            if (strike.Execute(target) && target.Spawned && !target.Dead)
                AOCSkinBrush.CompScrub.PushTarget(pawn, target, sequence.pushCells);
            return false;
        }
    }

    public class Verb_ScrubStrikeCE : Verb_MeleeAttackCE
    {
        public float reach;
        public override bool CanHitTarget(LocalTargetInfo target) =>
            AOCSkinBrush.ScrubReach.CanReach(CasterPawn, target.Pawn, reach);
        public bool Execute(Pawn target) { currentTarget = target; return TryCastShot(); }
    }

    public class Projectile_B2HCE : ProjectileCE_Explosive
    {
        private AOCB2H.Settings Settings => def.projectile.damageDef.GetModExtension<AOCB2H.Settings>();

        public override void Impact(Thing hitThing)
        {
            bool wasLanded = landed;
            base.Impact(hitThing);
            if (!wasLanded && !Destroyed && landed && Settings?.linkSound != null)
                Settings.linkSound.PlayOneShot(new TargetInfo(Position, Map));
        }

        public override void Tick()
        {
            int previous = ticksToDetonation;
            base.Tick();
            if (Destroyed || !Spawned || !landed || Settings == null) return;
            int cue = Math.Max(0, def.projectile.explosionDelay - Settings.chargeCueTick);
            if (previous > cue && ticksToDetonation <= cue)
                Settings.chargeSound?.PlayOneShot(new TargetInfo(Position, Map));
            if ((Find.TickManager.TicksGame + thingIDNumber) % 6 == 0)
                Visuals.Dust(Position.ToVector3Shifted(), Map, def.projectile.explosionRadius,
                    Animation.PrimeProgress(ticksToDetonation, def.projectile.explosionDelay), Settings);
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (landed && Spawned && Settings != null)
                Visuals.Prime(Position.ToVector3Shifted(), def.projectile.explosionRadius,
                    Animation.PrimeProgress(ticksToDetonation, def.projectile.explosionDelay), Settings);
        }
    }

    // Patching after all mod assemblies load avoids an early dependency on AOC
    // when CE occurs first in ModsConfig. All native save fields remain intact.
    [HarmonyPatch]
    public static class AimedSequenceCE
    {
        static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("AOCSpearForms.CompFormSequence"), "CompPostTick");

        public static bool Prefix(object __instance, ref int ___nextTick, ref bool ___done)
        {
            var sequence = (CompFormSequence)__instance;
            if (!((CompProperties_FormSequence)sequence.props).ranged)
            {
                if (sequence.form?.defName == "AOC_LongMonoSpear")
                    return PierceSequenceCE.Tick(sequence, ref ___nextTick, ref ___done);
                return true;
            }
            Pawn pawn = sequence.Pawn;
            var target = sequence.target;
            var weapon = sequence.weapon;
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Position != sequence.origin ||
                pawn.pather.Moving || pawn.equipment?.Primary != weapon || weapon.def != sequence.form ||
                target == null || !target.Spawned || target.Dead || target.Downed || target.Map != pawn.Map)
            { ___done = true; return false; }
            if (--___nextTick > 0) return false;
            // The ability can finish while a recovery stance is still active.
            // Preserve the saved sequence until CE is ready to fire.
            if (pawn.stances.FullBodyBusy) { ___nextTick = 1; return false; }
            ___done = true;
            var original = weapon.GetComp<CompEquippable>()?.AllVerbs.OfType<Verb_ShootCE>().FirstOrDefault();
            if (original == null) return false;
            var properties = (VerbPropertiesCE)typeof(object).GetMethod("MemberwiseClone",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(original.verbProps, null);
            properties.warmupTime = 0;
            properties.burstShotCount = 1;
            properties.range = 36f;
            properties.defaultProjectile = DefDatabase<ThingDef>.GetNamed("AOC17CE_AimedJavelin");
            var shot = new Verb_AimedJavelinCE
            { caster = pawn, verbTracker = original.verbTracker, verbProps = properties };
            // Validate the skill verb so the normal throw's 32-cell range does
            // not reject targets within Gungnir's 36-cell ability range.
            if (shot.CanHitTarget(target) && shot.TryStartCastOn(target))
                pawn.health.AddHediff(sequence.cooldown);
            return false;
        }
    }

    public class Verb_AimedJavelinCE : Verb_ShootCE
    {
        public override int ShotsPerBurst => 1;
        public override ThingDef Projectile => DefDatabase<ThingDef>.GetNamed("AOC17CE_AimedJavelin");
        protected override bool ShouldAim => false;
    }

    public class Bullet_AimedJavelinCE : BulletCE
    {
        public override void Impact(Thing hitThing)
        {
            var pawn = hitThing as Pawn;
            Map map = Map;
            IntVec3 cell = pawn?.Position ?? IntVec3.Invalid;
            base.Impact(hitThing);
            if (map != null && cell.IsValid && cell.InBounds(map))
                map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, cell));
            if (pawn != null && pawn.Spawned && !pawn.Dead && pawn.BodySize <= 1.5f)
                pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("AOC_JavelinSlow"));
        }
    }

    public class Bullet_DelayedTCBCE : BulletCE
    {
        public override void Impact(Thing hitThing)
        {
            if (Map != null && hitThing != null && hitThing.Spawned)
            {
                var settings = def.GetModExtension<DelayedBlastExtension>() ?? new DelayedBlastExtension();
                Map.GetComponent<DelayedBlastMap>()?.Add(new DelayedCharge
                {
                    target = hitThing, attacker = launcher, projectile = def, weapon = equipmentDef,
                    dueTick = Find.TickManager.TicksGame + Math.Max(1, settings.delayTicks),
                    damage = Math.Max(0, settings.damage), radius = Math.Max(0, settings.radius),
                    penetration = Math.Max(0, settings.armorPenetration)
                });
            }
            base.Impact(hitThing);
        }
    }
}
