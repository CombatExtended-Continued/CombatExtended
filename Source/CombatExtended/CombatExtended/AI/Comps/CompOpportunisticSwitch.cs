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
        private const int COOLDOWN_OPPORTUNISTIC_TICKS = 600;

        private const int COOLDOWN_TICKS = 1200;

        private const int COOLDOWN_FACTION_TICKS = 600;

        private int lastFlared = -1;

        private int lastUsedAEOWeapon = -1;

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

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!ShouldRun) return true;
            if (OpportunisticallySwitchedRecently) return true;
            if (!StartFlareChecks(verb, castTarg, destTarg))
                return false;
            if (!StartAOEChecks(verb, castTarg, destTarg))
                return false;
            return true;
        }

        public bool StartAOEChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!UsedAOEWeaponRecently && !(verb.EquipmentSource?.def.IsAOEWeapon() ?? false))
            {
                float distance = castTarg.Cell.DistanceTo(SelPawn.Position);

                if (castTarg.HasThing && castTarg.Thing is Pawn pawn && (distance > 8 || SelPawn.HiddingBehindCover(pawn.positionInt)) && TargetIsSquad(pawn))
                {
                    if (CompInventory.TryFindRandomAOEWeapon(out ThingWithComps weapon, checkAmmo: true, predicate: (g) => g.def.Verbs?.Any(t => t.range >= distance + 3) ?? false))
                    {
                        var nextVerb = weapon.def.verbs.First(v => !v.IsMeleeAttack);
                        var targtPos = AI_Utility.FindAttackedClusterCenter(SelPawn, castTarg.Cell, weapon.def.verbs.Max(v => v.range), 4, (pos) =>
                        {
                            return GenSight.LineOfSight(SelPawn.Position, pos, Map, skipFirstCell: true);
                        });
                        var job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, weapon, targtPos.IsValid ? targtPos : castTarg.Cell);
                        job.maxNumStaticAttacks = 1;

                        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        return false;
                    }
                }
            }
            return true;
        }

        public bool StartFlareChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!FlaredRecently && !FlaredRecentlyByFaction && !Map.VisibilityGoodAt(SelPawn, castTarg.Cell, NightVisionEfficiency))
            {
                if (!(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindFlare(out ThingWithComps flareGun, checkAmmo: true))
                {
                    float range = flareGun.def.verbs.Max(v => v.range);
                    VerbProperties nextVerb = flareGun.def.verbs.First(v => !v.IsMeleeAttack);
                    if (range >= castTarg.Cell.DistanceTo(SelPawn.Position))
                    {
                        var targtPos = AI_Utility.FindAttackedClusterCenter(SelPawn, castTarg.Cell, flareGun.def.verbs.Max(v => v.range), 8, (pos) =>
                        {
                            return !nextVerb.requireLineOfSight || pos.Roofed(Map);
                        });
                        var job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, flareGun, targtPos.IsValid ? targtPos : castTarg.Cell);
                        job.maxNumStaticAttacks = 1;

                        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                        return false;
                    }
                }
            }
            return true;
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (!ShouldRun) return;
            if (verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false)
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
