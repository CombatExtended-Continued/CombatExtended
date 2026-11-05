using CombatExtended.Compatibility.VGECompat;
using UnityEngine;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Verb_AnticraftBeamCE : Verb_ShootWithVGETargeting
{
    // See https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Verbs/Verb_AnticraftBeam.cs
    protected override float GetGlobalMissRadiusForDist(float targDist)
    {
        return base.GetGlobalMissRadiusForDist(targDist) + Random.Range(0, 6.9f);
    }
}
