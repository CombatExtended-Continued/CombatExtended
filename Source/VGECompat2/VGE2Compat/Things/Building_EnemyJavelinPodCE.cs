#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using UnityEngine;

namespace CombatExtended.Compatibility.VGECompat2;

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
