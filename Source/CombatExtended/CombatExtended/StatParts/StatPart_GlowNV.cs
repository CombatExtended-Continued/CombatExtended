using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;
/// <summary>
/// Vanilla darkness penalty (StatPart_Glow) with the penalty reduced in proportion to the pawn's
/// night vision efficiency, ignoring weapon optics. Used for move, work and reading speed.
/// </summary>
public class StatPart_GlowNV : StatPart_Glow
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        float before = val;
        base.TransformValue(req, ref val);
        // No penalty applied (lit, exempt pawn, not a pawn): nothing to reduce.
        if (before <= 0f || val >= before || req.Thing is not Pawn pawn)
        {
            return;
        }
        float nightVision = CE_Utility.NightVisionWithoutWeapon(pawn);
        if (nightVision > 0f)
        {
            // Equivalent to before * Lerp(glowFactor, 1, nightVision)
            val = Mathf.Lerp(val, before, nightVision);
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        string text = base.ExplanationPart(req);
        if (text.NullOrEmpty() || req.Thing is not Pawn pawn)
        {
            return text;
        }
        float nightVision = CE_Utility.NightVisionWithoutWeapon(pawn);
        if (nightVision <= 0f)
        {
            return text;
        }
        return text + "\n" + "CE_StatPart_GlowNV".Translate(nightVision.ToStringPercent());
    }
}
