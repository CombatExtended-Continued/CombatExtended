using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace CombatExtended.Compatibility.SOS2Compat
{
    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    public class Harmony_TurretConversion
    {
        public static bool Prepare()
        {
            return true;
        }

        // Converts all Building_ShipTurret into Building_ShipTurretCE (Except for spinals and torpedos)
        public static void Postfix(Map __instance)
        {
            Map map = __instance;

            List<Thing> thingsToRemove = new List<Thing>();
            List<Thing> thingsToSpawn = new List<Thing>();

            // Iterate through all things on the map
            foreach (Thing thing in map.listerThings.AllThings)
            {
                Building_ShipTurretCE turretCE = thing as Building_ShipTurretCE;
                if (turretCE != null)
                {
                    continue; // Skip over anything that is already our type.
                    // Added this as a valid Building_ShipTurretCE could be converted to Building_ShipTurret from the wrapper and don't know how else to filter them out
                }
                Building_ShipTurret turretSOS2 = thing as Building_ShipTurret;
                if (turretSOS2 == null || turretSOS2.Destroyed)
                {
                    continue; // thing isnt the right class so skip it
                }

                ThingDef turretDef = turretSOS2.def;
                Type ceTurretType = Type.GetType("CombatExtended.Compatibility.SOS2Compat.Building_ShipTurretCE");

                if (turretDef.thingClass != ceTurretType)
                {
                    // The turret isn't supposed to be using a CE turret type, likely spinal or torpedo
                    continue;
                }
                Log.Message("CombatExtended SOS2Compat :: Building_ShipTurret found with a thingDef class of Building_ShipTurretCE, marking for replacement.");
                // Create a new ShipTurretCE
                Building_ShipTurretCE newTurret = (Building_ShipTurretCE)ThingMaker.MakeThing(turretDef);

                // Update necessary properties
                newTurret.Position = turretSOS2.Position;
                newTurret.Rotation = turretSOS2.Rotation;
                newTurret.hitPointsInt = turretSOS2.hitPointsInt;
                newTurret.factionInt = turretSOS2.factionInt;

                // Add/Remove must be done outside of iterating over AllThings.
                // Mark new turret to be added to map
                thingsToSpawn.Add(newTurret);

                // Mark old turret for destruction
                thingsToRemove.Add(turretSOS2);
            }
            // Remove all the old turrets
            foreach (Thing toRemove in thingsToRemove)
            {
                if (toRemove != null && !toRemove.Destroyed)
                {
                    Log.Message($"CombatExtended SOS2Compat :: Destroying turret {toRemove}");
                    toRemove.Destroy();
                }
            }
            // Spawn all the new turrets
            foreach (Thing toSpawn in thingsToSpawn)
            {
                Log.Message($"CombatExtended SOS2Compat :: Spawning turret {toSpawn}");
                GenSpawn.Spawn(toSpawn, toSpawn.Position, map, toSpawn.Rotation);
            }
        }
    }
}
