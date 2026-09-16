using CombatExtended.Compatibility.VGECompat;
using RimWorld.Planet;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Verb_AnticraftBeamCE : Verb_ShootWithVGETargeting
{
    protected override float GetGlobalMissRadiusForDist(float targDist)
    {
        return base.GetGlobalMissRadiusForDist(targDist) + Random.Range(0, 6.9f); // See https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Verbs/Verb_AnticraftBeam.cs
    }
}
