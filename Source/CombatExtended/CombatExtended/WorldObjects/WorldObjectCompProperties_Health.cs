using System;
using RimWorld;
using RimWorld.Planet;

namespace CombatExtended.WorldObjects;
public class WorldObjectCompProperties_Health : WorldObjectCompProperties
{
    public bool destoyedInstantly = false;
    public float healthModifier = -1f;
    public float chanceToNegateDamage = 0f;
    public bool techLevelNoImpact = false;

    public WorldObjectCompProperties_Health()
    {
        this.compClass = typeof(HealthComp);
    }
}

