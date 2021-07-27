using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public partial class CompFlaring : ICompTactics
    {
        private int lastFlared = -1;

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

        public bool FlaredRecently
        {
            get
            {
                return (GenTicks.TicksGame - lastFlared < GenTicks.TickLongInterval);
            }
        }

        public bool FlaredRecentlyByFaction
        {
            get
            {
                if (SelPawn.factionInt != null && factionLastFlare.TryGetValue(SelPawn.factionInt, out int ticks)
                    && GenTicks.TicksGame - ticks < GenTicks.TickLongInterval / 2) return true;
                return false;
            }
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
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
            if (!(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindFlare(out ThingWithComps flareGun))
            {
                StartEquipWeaponJob(flareGun);
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
        }

        public void StartEquipWeaponJob(ThingWithComps gun)
        {
            SelPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, gun), JobCondition.InterruptForced, resumeCurJobAfterwards: true);
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
