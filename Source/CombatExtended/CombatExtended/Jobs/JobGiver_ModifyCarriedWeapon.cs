using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_ModifyCarriedWeapon : ThinkNode_JobGiver
    {
        private const int THROTTLE_COOLDOWN = 450;
        private static Dictionary<int, int> _throttle = new Dictionary<int, int>();

        public override float GetPriority(Pawn pawn)
        {            
            // throttle it a bit
            if (_throttle.TryGetValue(pawn.thingIDNumber, out int ticks) && GenTicks.TicksGame - ticks < THROTTLE_COOLDOWN)
                return -1f;
            _throttle[pawn.thingIDNumber] = GenTicks.TicksGame;
            // do some checks
            if (!pawn.RaceProps.Humanlike || (!pawn.IsColonist && !pawn.IsSlaveOfColony) || pawn?.equipment?.Primary == null)
                return -1;
            // check if this pawn can craft
            if (pawn.WorkTagIsDisabled(WorkTags.Crafting))
                return -1;            
            if (!(pawn.health?.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ?? false))
                return -1;
            // the more attachments need to be installed the higher the priority 
            if (pawn?.equipment?.Primary is WeaponPlatform platform)
            {
                platform.TrySyncPlatformLoadout(pawn);
                if (!platform.ConfigApplied)
                    return 20f + (platform.AdditionList?.Count ?? 0) * 8f + (platform.RemovalList?.Count ?? 0) * 8f;
            }
            return -1f;
        }        

        public override Job TryGiveJob(Pawn pawn)
        {
            // do some checks
            if (!pawn.RaceProps.Humanlike || (!pawn.IsColonist && !pawn.IsSlaveOfColony) || pawn?.equipment?.Primary == null)
                return null;
            // check if this pawn can craft
            if (pawn.WorkTagIsDisabled(WorkTags.Crafting))
                return null;
            if (!(pawn.health?.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ?? false))
                return null;
            // check if the primary weapon can use attachments
            if (pawn.equipment?.Primary is WeaponPlatform platform && !platform.ConfigApplied)
                return WorkGiver_ModifyWeapon.TryGetModifyWeaponJob(pawn, platform: platform);
            return null;
        }       
    }
}
