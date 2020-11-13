using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;
using RimWorld;
using Verse.AI;

namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch(typeof(RimWorld.WorkGiver_LoadTorpedoTube), "FindAmmo")]
    class Harmony_Compatibility_SaveOurShip
    {
	public static void Postfix(Pawn pawn, Building_ShipTurret tube, ref Thing __result)
	{
	    if (__result!=null) {
		return;
	    }
	    StorageSettings allowedShellsSettings = ThingCompUtility.TryGetComp<CompChangeableProjectilePlural>(tube.gun).allowedShellsSettings;
	    Predicate<Thing> pred = (Thing x) =>
            {
	        if (!ForbidUtility.IsForbidden(x, pawn) && ReservationUtility.CanReserve(pawn, x, 1, -1, (ReservationLayerDef)null, false))
                {
                    if (allowedShellsSettings != null)
                    {
                        return allowedShellsSettings.AllowedToAccept(x);
                    }
                    return true;
                }
                return false;
            };
            var val = ThingDef.Named("ShipTorpedo_HighExplosive");
	    __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(val), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, pred);
	    if (__result!=null) {
		return;
	    }
	    val = ThingDef.Named("ShipTorpedo_EMP");
	    __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(val), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, pred);
	    if (__result!=null) {
		return;
	    }
	    val = ThingDef.Named("ShipTorpedo_Antimatter");
            __result = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(val), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, pred);
	}
	
    }
}
