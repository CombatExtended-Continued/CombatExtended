using CombatExtended.Compatibility.VGECompat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaGravshipExpanded;

namespace CombatExtended.Compatibility.VGECompat;

public class Building_GravshipTurrentCE: Building_TurretGunCE
{
    #region License
    // Any VGE Code used for compatibility has been taken from the following source
    // https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurret.cs
    #endregion

    // Explicit type conversion that will be used in SOS2 code for turrets added to heat net turrets list.
    // Also allows conversion of Building_ShipTurretCE into Building_ShipTurret as the SaveOurShip2.ShipCombatProjectile class which is used in Verb_ShootShipCE requires a Building_ShipTurret to be passed into its constructor.
    public Building_GravshipTurret ToBuilding_ShipTurret()
    {
        return new GravshipTurretWrapperCE(this);
    }

}
