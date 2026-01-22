using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaGravshipExpanded;

namespace CombatExtended.Compatibility.VGECompat;

public class GravshipTurretWrapperCE: Building_GravshipTurret
{
    // This is so that we can fire ShipCombatProjectile from Verb_ShootShipCE by converting Building_ShipTurretCE into something inheriting from Building_ShipTurret.
    // This class is only used when creating a new SaveOurShip2.ShipCombatProjectile and passing through a Building_ShipTurretCE as one of its constructor arguements.

    // This class uses reflection to automatically delegate all properties and fields. The performance impact "should" be minimal as once the delegation is complete subsequent access calls will be direct.
    // There will be an overhead for the initial conversion of each Building_ShipTurretCE though.

    public Building_GravshipTurrentCE turretCE;

    public GravshipTurretWrapperCE(Building_GravshipTurrentCE turretCE)
    {
        this.turretCE = turretCE;
    }
}
