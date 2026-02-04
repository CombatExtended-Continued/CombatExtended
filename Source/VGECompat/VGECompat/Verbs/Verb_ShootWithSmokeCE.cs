using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Verbs/Verb_ShootWithSmoke.cs
#endregion

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

