using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class StatPart_AttachmentsMultiplier : StatPart_Attechmenets
    {
        public override string ExplanationPart(StatRequest req)
        {
            return "";
        }

        protected override void Transform(Thing attachment, ref float val)
        {
            float m = attachment.GetStatValue(this.parentStat);
            if (m != 0)
                val *= Mathf.Clamp(m, 0.1f, 1.9f);
        }
    }
}
