using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_JavelinLauncher.cs
#endregion

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
