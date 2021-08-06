using System;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatPart_AttachmentsOffset : StatPart_Attechmenets
    {
        public override string ExplanationPart(StatRequest req)
        {
            return "";
        }

        protected override void Transform(Thing attachment, ref float val)
        {
            val += attachment.GetStatValue(this.parentStat);
        }
    }
}
