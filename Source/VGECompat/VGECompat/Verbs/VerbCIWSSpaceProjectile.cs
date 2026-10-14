using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;
public class VerbCIWSSpaceProjectile : VerbCIWS<Projectile_Space>
{
    public new VerbProperties_CIWSSSpaceProjectile Props => verbProps as VerbProperties_CIWSSSpaceProjectile;

    public override IEnumerable<Projectile_Space> Targets => Caster.Map?.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).OfType<Projectile_Space>() ?? Array.Empty<Projectile_Space>();

    protected override bool IsFriendlyTo(Projectile_Space thing) => false;

    // I did not want to implement a Projectile_SpaceCE, so I used core rimworld props to predict projectile positions.
    protected override IEnumerable<Vector3> PredictPositions(Projectile_Space target, int tickCount)
    {
        if (tickCount > GenTicks.TicksPerRealSecond)
        {
            tickCount = GenTicks.TicksPerRealSecond;
        }
        Vector3 ep = target.ExactPosition.Yto0();
        Vector3 direction = (target.destination - target.origin).normalized;
        Vector3 velocity = direction * target.def.projectile.SpeedTilesPerTick;
        for (int i = 1; i < tickCount; i++)
        {
            yield return ep + velocity * i;
        }
    }
}
public class VerbProperties_CIWSSSpaceProjectile : VerbProperties_CIWS
{
    public VerbProperties_CIWSSSpaceProjectile()
    {
        this.verbClass = typeof(VerbCIWSSpaceProjectile);
        this.holdFireIcon = "ThirdParty/Vanilla Gravship Expanded - Chapter 1/UI/Buttons/CE_CIWS_SpaceProjectile";
        this.holdFireLabel = "HoldCloseInSpaceProjectilesFire";
        this.holdFireDesc = "HoldCloseInSpaceProjectilesFireDesc";
    }
    public override bool Interceptable(ThingDef targetDef) => targetDef.projectile.speed < maximumSpeed && typeof(Projectile_Space).IsAssignableFrom(targetDef.thingClass) && base.Interceptable(targetDef);
    public float maximumSpeed = 80;
    public bool shouldInterceptUnpredictable = true;
    protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && typeof(Projectile_Space).IsAssignableFrom(x.thingClass) && x.projectile.speed < maximumSpeed);
}
