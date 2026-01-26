using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;


public abstract class Building_TurretGunCEWithVGEAdapter : Building_TurretGunCE
{
    private Building_GravshipTurret composite;

    public virtual Building_GravshipTurret ToBuilding_GravshipTurret
    {
        get => composite;
        set { composite = value; }
    }
    public abstract Building_GravshipTurret GetBuilding_GravshipTurret(Building_TurretGunCEWithVGEAdapter instance);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // filling the composite element
        composite = GetBuilding_GravshipTurret(this);
        composite.gun = Gun;
    }
}

