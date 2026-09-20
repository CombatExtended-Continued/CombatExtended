using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

// -- Patchception --
// We are gonna patch ScenPart_PlayerPawnsArriveMethod_DoGravship_Patch.DistributeIntoPipeNet method to inject ammo into our turrets
// It wasn't intended to do that at first place, but we need to be smart
[HarmonyPatch(typeof(ScenPart_PlayerPawnsArriveMethod_DoGravship_Patch), nameof(ScenPart_PlayerPawnsArriveMethod_DoGravship_Patch.DistributeIntoPipeNet))]
[HarmonyBefore("vanillaexpanded.gravship")] // Use priority to avoid crash
public class ScenPart_PlayerPawnsArriveMethod_DoGravship_Patch_DistributeIntoPipeNet_Patch
{
    public static void Postfix(Map map, IntVec3 center, List<Thing> spawned)
    {
        var CETurretCompAmmo = spawned
            .OfType<Building_TurretGunCE>()
            .Select(turret => turret.Gun)
            .Select(turretGun => turretGun.TryGetComp<CompAmmoUser>());

        foreach (var ammoComp in CETurretCompAmmo)
        {
            ammoComp.ResetAmmoCount();
        }
    }
}

