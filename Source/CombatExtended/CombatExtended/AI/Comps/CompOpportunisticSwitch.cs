using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompOpportunisticSwitch : ICompTactics
    {
        private enum TargetType
        {
            None = 0,
            Pawn = 1,
            Turret = 2
        }

        private const int COOLDOWN_OPPORTUNISTIC_TICKS = 1400;

        private const int COOLDOWN_TICKS = 2000;

        private const int COOLDOWN_FACTION_TICKS = 600;

        private int lastFlared = -1;

        private int lastUsedAEOWeapon = -1;

        private int lastOptimizedWeapon = -1;

        private int lastOpportunisticSwitch = -1;

        private static Dictionary<Faction, int> factionLastFlare = new Dictionary<Faction, int>();

        public override int Priority => 500;

        public LightingTracker LightingTracker
        {
            get
            {
                return Map.GetLightingTracker();
            }
        }

        private int _NVEfficiencyAge = -1;
        private float _NVEfficiency = -1;
        public float NightVisionEfficiency
        {
            get
            {
                if (_NVEfficiency == -1 || GenTicks.TicksGame - _NVEfficiencyAge > GenTicks.TickRareInterval)
                {
                    _NVEfficiency = SelPawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency);
                    _NVEfficiencyAge = GenTicks.TicksGame;
                }
                return _NVEfficiency;
            }
        }

        public bool OpportunisticallySwitchedRecently
        {
            get
            {
                return (GenTicks.TicksGame - lastOpportunisticSwitch < COOLDOWN_OPPORTUNISTIC_TICKS);
            }
        }

        public bool FlaredRecently
        {
            get
            {
                return lastFlared != -1 && GenTicks.TicksGame - lastFlared < COOLDOWN_TICKS;
            }
        }

        public bool OptimizedWeaponRecently
        {
            get
            {
                return lastOptimizedWeapon != -1 && GenTicks.TicksGame - lastOptimizedWeapon < COOLDOWN_TICKS;
            }
        }

        public virtual bool ShouldRun
        {
            get
            {
                return !(SelPawn.Faction?.IsPlayer ?? false);
            }
        }

        public bool FlaredRecentlyByFaction
        {
            get
            {
                if (SelPawn.factionInt != null && factionLastFlare.TryGetValue(SelPawn.factionInt, out int ticks)
                    && GenTicks.TicksGame - ticks < COOLDOWN_FACTION_TICKS) return true;
                return false;
            }
        }

        public bool UsedAOEWeaponRecently
        {
            get
            {
                return lastUsedAEOWeapon != -1 && GenTicks.TicksGame - lastUsedAEOWeapon < COOLDOWN_TICKS;
            }
        }

        public CompOpportunisticSwitch()
        {
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!ShouldRun) return true;
            if (OpportunisticallySwitchedRecently) return true;
            if (TryFlare(verb, castTarg, destTarg))
                return false;
            if (TryUseAOE(verb, castTarg, destTarg))
                return false;
            return true;
        }

        public bool TryUseAOE(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!UsedAOEWeaponRecently && !(verb.EquipmentSource?.def.IsAOEWeapon() ?? false))
            {
                TargetType targetType = TargetType.None;
                float distance = castTarg.Cell.DistanceTo(SelPawn.Position);

                if (castTarg.HasThing &&
                    (TargetingPawns(castTarg.Thing, distance, out targetType) || TargetingTurrets(castTarg.Thing, distance, out targetType) || Rand.Chance(0.1f)))
                {
                    // TODO add the ability to switch to EMP ammo or weapons
                    if (targetType == TargetType.Turret)
                    {
                    }
                    if (CompInventory.TryFindRandomAOEWeapon(out ThingWithComps weapon, checkAmmo: true, predicate: (g) => g.def.Verbs?.Any(t => t.range >= distance + 3) ?? false))
                    {
                        lastOpportunisticSwitch = GenTicks.TicksGame;

                        var nextVerb = weapon.def.verbs.First(v => !v.IsMeleeAttack);
                        var targtPos = AI_Utility.FindAttackedClusterCenter(SelPawn, castTarg.Cell, weapon.def.verbs.Max(v => v.range), 4, (pos) =>
                        {
                            return GenSight.LineOfSight(SelPawn.Position, pos, Map, skipFirstCell: true);
                        });
                        var job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, weapon, targtPos.IsValid ? targtPos : castTarg.Cell);
                        job.maxNumStaticAttacks = 1;

                        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        return true;
                    }
                }
            }
            bool TargetingPawns(Thing thing, float distance, out TargetType targetType)
            {
                targetType = TargetType.None;
                if (thing is Pawn pawn && (distance > 8 || SelPawn.HiddingBehindCover(pawn.positionInt)) && TargetIsSquad(pawn))
                {
                    targetType = TargetType.Pawn;
                    return true;
                }
                return false;
            }
            bool TargetingTurrets(Thing thing, float distance, out TargetType targetType)
            {
                targetType = TargetType.None;
                if (thing is Building_Turret && (distance > 8 || SelPawn.HiddingBehindCover(thing.positionInt)))
                {
                    targetType = TargetType.Turret;
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool TryFlare(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!FlaredRecently && !FlaredRecentlyByFaction && !Map.VisibilityGoodAt(SelPawn, castTarg.Cell, NightVisionEfficiency))
            {
                if (!(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindFlare(out ThingWithComps flareGun, checkAmmo: true))
                {
                    float range = flareGun.def.verbs.Max(v => v.range);
                    VerbProperties nextVerb = flareGun.def.verbs.First(v => !v.IsMeleeAttack);
                    if (range >= castTarg.Cell.DistanceTo(SelPawn.Position))
                    {
                        lastOpportunisticSwitch = GenTicks.TicksGame;

                        IntVec3 targtPos = AI_Utility.FindAttackedClusterCenter(SelPawn, castTarg.Cell, flareGun.def.verbs.Max(v => v.range), 8, (pos) =>
                        {
                            return !nextVerb.requireLineOfSight || !pos.Roofed(Map);
                        });
                        Job job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, flareGun, targtPos.IsValid ? targtPos : castTarg.Cell);
                        job.maxNumStaticAttacks = 1;

                        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        return true;
                    }
                }
            }
            if ((verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && !(SelPawn.jobs?.curDriver is IJobDriver_Tactical))
            {
                if (CompInventory.TryFindViableWeapon(out ThingWithComps weapon))
                {
                    SelPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, weapon), JobCondition.InterruptForced);
                    return true;
                }
            }
            return false;
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (verb.EquipmentSource?.def.IsIlluminationDevice() ?? false)
            {
                lastFlared = GenTicks.TicksGame;
                if (SelPawn.Faction != null)
                    factionLastFlare[SelPawn.Faction] = lastFlared;
            }
            if (verb.EquipmentSource?.def.IsAOEWeapon() ?? false)
                lastUsedAEOWeapon = GenTicks.TicksGame;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUsedAEOWeapon, "lastUsedAEOWeapon", -1);
            Scribe_Values.Look(ref lastFlared, "lastFlared", -1);
            Scribe_Values.Look(ref lastOptimizedWeapon, "lastOptimizedWeapon", -1);
            Scribe_Values.Look(ref lastOpportunisticSwitch, "lastOpportunisticSwitch", -1);
        }

        private bool TargetIsSquad(Pawn pawn)
        {
            int hostiles = 0;
            foreach (Pawn other in pawn.Position.PawnsInRange(pawn.Map, 4))
            {
                if (other.Faction == null)
                    continue;
                if (other.Faction.HostileTo(SelPawn.Faction))
                {
                    hostiles += 1;
                    continue;
                }
                return false;
            }
            return hostiles > 1;
        }
    }
}
