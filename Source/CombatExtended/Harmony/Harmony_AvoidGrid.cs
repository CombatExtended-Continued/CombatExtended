using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(AvoidGrid), "Regenerate")]
    static class Harmony_AvoidGrid_Regenerate
    {
        static bool Prefix(AvoidGrid __instance)
        {
            __instance.gridDirty = false;
            __instance.grid.Clear(0);
            List<Building> allBuildingsColonist = __instance.map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                if (allBuildingsColonist[i].def.building.ai_combatDangerous)
                {
                    var building_TurretGun = allBuildingsColonist[i] as Building_TurretGunCE;
                    if (building_TurretGun != null)
                    {
                        PrintAvoidGridAroundTurret(__instance, building_TurretGun);
                    }
                }
            }
            __instance.ExpandAvoidGridIntoEdifices();
            return false;
        }

        static void PrintAvoidGridAroundTurret(Verse.AI.AvoidGrid __instance, Building_TurretGunCE tur)
        {
            float range = tur.GunCompEq.PrimaryVerb.verbProps.range;
            float num = tur.GunCompEq.PrimaryVerb.verbProps.EffectiveMinRange(true);
            int num2 = GenRadial.NumCellsInRadius(range + 4f);
            for (int i = num < 1f ? 0 : GenRadial.NumCellsInRadius(num); i < num2; i++)
            {
                IntVec3 intVec = tur.Position + GenRadial.RadialPattern[i];
                if (intVec.InBounds(tur.Map) && intVec.WalkableByNormal(tur.Map) && GenSight.LineOfSight(intVec, tur.Position, tur.Map, true, null, 0, 0))
                {
                    __instance.IncrementAvoidGrid(intVec, 45);
                }
            }
        }
    }

}
