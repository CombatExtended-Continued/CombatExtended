using System;
using Verse;
using CombatExtended.Utilities;
using System.Linq;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompGrenading : ICompTactics
    {
        private int lastUsedAEOWeapon = -1;

        private const int COOLDOWN_TICKS = 1200;

        private ThingWithComps previousWeapon;

        public override int Priority => 300;

        public bool UsedAOEWeaponRecently
        {
            get
            {
                return lastUsedAEOWeapon != -1 && GenTicks.TicksGame - lastUsedAEOWeapon < COOLDOWN_TICKS;
            }
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if ((verb.EquipmentSource?.def.IsAOEWeapon() ?? false))
            {
                if (previousWeapon != null && !previousWeapon.Destroyed && CompInventory.container.Contains(previousWeapon))
                {
                    StartEquipWeaponJob(previousWeapon);
                    previousWeapon = null;
                    return false;
                }
                if (UsedAOEWeaponRecently && CompInventory.TryFindViableWeapon(out ThingWithComps weapon, useAOE: false))
                {
                    StartEquipWeaponJob(weapon);
                    return false;
                }
                return true;
            }
            if (UsedAOEWeaponRecently)
            {
                previousWeapon = null;
                return true;
            }
            float distance = castTarg.Cell.DistanceTo(SelPawn.Position);
            if (castTarg.HasThing && castTarg.Thing is Pawn pawn && (distance > 8 || SelPawn.HiddingBehindCover(pawn.positionInt)) && TargetIsSquad(pawn))
            {
                if (CompInventory.TryFindRandomAOEWeapon(out ThingWithComps weapon, predicate: (g) => g.def.Verbs?.Any(t => t.range >= distance + 3) ?? false))
                {
                    previousWeapon = CurrentWeapon;
                    StartEquipWeaponJob(weapon);
                    return false;
                }
            }
            return true;
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (verb.EquipmentSource?.def.IsAOEWeapon() ?? false)
                lastUsedAEOWeapon = GenTicks.TicksGame;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUsedAEOWeapon, "lastUsedAEOWeapon", -1);
        }


        private bool TargetIsSquad(Pawn pawn)
        {
            return pawn.Position.PawnsInRange(pawn.Map, 4)?.Count(p => p.factionInt == pawn.factionInt) >= 2;
        }

        public void StartEquipWeaponJob(ThingWithComps gun)
        {
            SelPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, gun), JobCondition.InterruptForced, resumeCurJobAfterwards: true);
        }
    }
}
