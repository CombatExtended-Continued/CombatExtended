using HarmonyLib;
using Verse;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(StartingPawnUtility), nameof(StartingPawnUtility.GeneratePossessions))]
    internal static class StartingPawnUtility_GenerateAmmo
    {
        static IntRange magRange = new IntRange(3, 5);

        internal static void Postfix(Pawn pawn)
        {
            var startingPossessions = Find.GameInitData.startingPossessions;
            if (startingPossessions.ContainsKey(pawn))
            {
                List<ThingDefCount> ammoList = new List<ThingDefCount>();
                foreach (var possession in startingPossessions[pawn])
                {
                    if (possession.thingDef.GetCompProperties<CompProperties_AmmoUser>() is CompProperties_AmmoUser ammoUser && ammoUser.ammoSet != null)
                    {
                        int count = ammoUser.AmmoGenPerMagOverride;
                        if (count <= 0)
                        {
                            count = Mathf.Max(ammoUser.magazineSize, 1);
                        }
                        count *= magRange.RandomInRange;

                        ammoList.Add(new ThingDefCount(ammoUser.ammoSet.ammoTypes.First().ammo, count));
                    }
                }
                foreach (var ammo in ammoList)
                {
                    startingPossessions[pawn].Add(ammo);
                }
            }
        }
    }
}
