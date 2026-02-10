using RimWorld;
using UnityEngine;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_JavelinLauncher.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public class Building_JavelinLauncherCE : Building_GravshipTurretCE
{
    public CompRefuelable RefuelableComp => Gun.TryGetComp<CompRefuelable>();
    public override Material TurretTopMaterial
    {
        get
        {
            if (RefuelableComp?.IsFull ?? false)
            {
                return def.building.turretGunDef.building.turretTopLoadedMat;
            }
            return def.building.turretTopMat;
        }
    }
}
