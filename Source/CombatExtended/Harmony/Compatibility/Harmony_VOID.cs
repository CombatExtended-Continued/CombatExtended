using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility
{
    public static class Harmony_VOID
    {
        private static Type Utils_Patch_HarmonyPatches
        {
            get
            {
                return AccessTools.TypeByName("EncounterFramework.Utils");
            }
        }

        [HarmonyPatch]
        public static class Harmony_Utils_Patch
        {

            public static bool Prepare()
            {
                return Utils_Patch_HarmonyPatches != null;
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method("EncounterFramework.Utils:DoGeneration");
            }

            public static void Postfix(Map map)
            {
                var buildingList = map.listerBuildings.allBuildingsNonColonist.ToList();
                foreach (var thing in buildingList)
                {
                    if (thing is Building_Turret turret)
                    {
                        ReplaceTurret(turret);
                    }
                }
            }

            private static void ReplaceTurret(Thing oldThing)
            {
                //Saving all the old turrets information
                var map = oldThing.Map;
                var position = oldThing.Position;
                var stuff = oldThing.Stuff;
                var faction = oldThing.Faction;
                var def = oldThing.def;

                oldThing.Destroy();

                //Make new Turret
                if (ThingMaker.MakeThing(def, stuff) is not Building_TurretGunCE newThing)
                {
                    return;
                }
                GenPlace.TryPlaceThing(newThing, position, map, ThingPlaceMode.Direct);
                newThing.SetFaction(faction);

                //Turret Ammo
                var ammoComp = newThing.CompAmmo;
                var ammoTypes = ammoComp.Props.ammoSet.ammoTypes;
                foreach (var ammoType in ammoTypes)
                {
                    if (ammoType.ammo.ammoClass == CE_AmmoCategoryDefOf.ExplosiveAP)
                    {
                        ammoComp.CurrentAmmo = ammoType.ammo;
                    }
                }
                ammoComp.CurMagCount = ammoComp.MagSize;

                //Force power on
                var newPowerTrader = newThing.TryGetComp<CompPowerTrader>();
                if (newPowerTrader != null)
                {
                    newPowerTrader.PowerOn = true;
                }

            }
        }
    }
}
