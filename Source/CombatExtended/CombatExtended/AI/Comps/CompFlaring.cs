using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public partial class CompFlaring : ICompTactics
    {
        private int lastFlared = -1;

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

        public bool FlareRecently
        {
            get
            {
                return GenTicks.TicksGame - lastFlared < GenTicks.TickLongInterval;
            }
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (TargetVisible(castTarg.Cell) || FlareRecently)
            {
                if ((verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindViableWeapon(out ThingWithComps weapon, useAOE: !SelPawn.IsColonist))
                {
                    EquipWeapon(weapon);
                    return false;
                }
                return true;
            }
            if (!(verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false) && CompInventory.TryFindFlare(out ThingWithComps flareGun))
            {
                EquipWeapon(flareGun);
                return false;
            }
            return true;
        }


        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (verb?.EquipmentSource?.def.IsIlluminationDevice() ?? false)
                lastFlared = GenTicks.TicksGame;
        }

        private void EquipWeapon(ThingWithComps gun)
        {
            SelPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, gun), JobCondition.InterruptForced, resumeCurJobAfterwards: true);
        }

        private bool TargetVisible(IntVec3 target)
        {
            LightingTracker tracker = LightingTracker;
            if (!tracker.IsNight)
                return true;
            if (target.DistanceTo(SelPawn.Position) < 20)
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
