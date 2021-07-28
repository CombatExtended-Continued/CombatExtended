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

        private ThingWithComps previousWeapon;

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
            // Check if we need to flare
            if (!UsedAOEWeaponRecently && !(verb.EquipmentSource?.def.IsAOEWeapon() ?? false) && !StartFlareChecks(verb, castTarg, destTarg))
                return false;
            // Check if we need to use an AOE desturctive weapon
            if (!FlaredRecently && !(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && !StartAOEChecks(verb, castTarg, destTarg))
                return false;
            // TODO add a check for using smoke grenades
            return true;
        }

        public bool StartAOEChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if ((verb.EquipmentSource?.def.IsAOEWeapon() ?? false))
            {
                if (UsedAOEWeaponRecently || !(verb.EquipmentSource.TryGetComp<CompAmmoUser>()?.HasAmmoOrMagazine ?? true))
                {
                    if (previousWeapon != null && !previousWeapon.Destroyed && CompInventory.container.Contains(previousWeapon))
                    {
                        StartEquipWeaponJob(previousWeapon);
                        previousWeapon = null;
                        return false;
                    }
                    if (CompInventory.TryFindViableWeapon(out ThingWithComps weapon, useAOE: false))
                    {
                        StartEquipWeaponJob(weapon);
                        return false;
                    }
                    return true;
                }
            }
            if (UsedAOEWeaponRecently)
            {
                previousWeapon = null;
                return true;
            }
            float distance = castTarg.Cell.DistanceTo(SelPawn.Position);
            if (castTarg.HasThing && castTarg.Thing is Pawn pawn && (distance > 8 || SelPawn.HiddingBehindCover(pawn.positionInt)) && TargetIsSquad(pawn))
            {
                if (!OpportunisticallySwitchedRecently && CompInventory.TryFindRandomAOEWeapon(out ThingWithComps weapon, predicate: (g) => g.def.Verbs?.Any(t => t.range >= distance + 3) ?? false))
                {
                    previousWeapon = CurrentWeapon;
                    StartEquipWeaponJob(weapon);
                    lastOpportunisticSwitch = GenTicks.TicksGame;
                    return false;
                }
            }
            return true;
        }

        public bool StartFlareChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (VisibilityGoodAt(castTarg.Cell) || FlaredRecently || FlaredRecentlyByFaction)
            {
                if ((verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindViableWeapon(out ThingWithComps weapon, useAOE: !SelPawn.IsColonist))
                {
                    StartEquipWeaponJob(weapon);
                    return false;
                }
                return true;
            }
            if (!OpportunisticallySwitchedRecently && !(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindFlare(out ThingWithComps flareGun))
            {
                StartEquipWeaponJob(flareGun);
                lastOpportunisticSwitch = GenTicks.TicksGame;
                return false;
            }
            return true;
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false)
            {
                lastFlared = GenTicks.TicksGame;
                if (SelPawn.Faction != null)
                    factionLastFlare[SelPawn.Faction] = lastFlared;
            }
            if (verb.EquipmentSource?.def.IsAOEWeapon() ?? false)
                lastUsedAEOWeapon = GenTicks.TicksGame;
        }

        public void StartEquipWeaponJob(ThingWithComps gun)
        {
            SelPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, gun), JobCondition.InterruptForced, resumeCurJobAfterwards: true);
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

        private bool VisibilityGoodAt(IntVec3 target)
        {
            LightingTracker tracker = LightingTracker;
            if (!tracker.IsNight)
                return true;
            if (target.DistanceTo(SelPawn.Position) < 15)
                return true;
            if (tracker.CombatGlowAtFor(SelPawn.Position, target) >= 0.5f)
                return true;
            if (NightVisionEfficiency > 0.6)
                return true;
            if (target.Roofed(Map))
                return true;
            return false;
        }
    }
}
