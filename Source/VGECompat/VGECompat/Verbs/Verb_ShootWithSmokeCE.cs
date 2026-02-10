using VanillaGravshipExpanded;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Verbs/Verb_ShootWithSmoke.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public class Verb_ShootWithSmokeCE : Verb_ShootWithVGETargeting
{
    public override bool TryCastShot()
    {
        bool result = base.TryCastShot();
        if (result)
        {
            for (var i = 0; i < 3; i++)
            {
                Verb_ShootWithSmoke.ThrowSmoke(caster.Position.ToVector3Shifted(), caster.Map, 1.5f);
            }
        }
        return result;
    }
}

