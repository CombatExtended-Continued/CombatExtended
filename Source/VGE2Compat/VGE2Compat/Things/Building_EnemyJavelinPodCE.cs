#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Buildings/Building_EnemyJavelinPod.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using UnityEngine;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Building_EnemyJavelinPodCE : Building_EnemyMechTurretCE
{
    public override Material TurretTopMaterial
    {
        get
        {
            if (burstCooldownTicksLeft <= 0)
            {
                return def.building.turretGunDef.building.turretTopLoadedMat;
            }
            return base.TurretTopMaterial;
        }
    }
}
