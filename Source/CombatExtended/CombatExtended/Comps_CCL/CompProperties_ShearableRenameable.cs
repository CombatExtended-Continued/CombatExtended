using System;
using RimWorld;
using Verse;

namespace CombatExtended;

public class CompProperties_ShearableRenameable : CompProperties_Shearable
{

    public string growthLabel = "";

    public CompProperties_ShearableRenameable()
    {
        this.compClass = typeof(CompShearableRenameable);
    }

}


