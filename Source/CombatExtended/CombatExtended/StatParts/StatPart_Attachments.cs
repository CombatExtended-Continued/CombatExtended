using System;
using RimWorld;

namespace CombatExtended
{
    public class StatPart_Attachments : StatPart
    {
        public StatPart_Attachments()
        {
        }

        public override string ExplanationPart(StatRequest req)
        {
            return "";
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            WeaponPlatformDef platform;
            if (!req.HasThing || !(req.Thing?.def is WeaponPlatformDef))
            {
                return;
            }
            platform = (WeaponPlatformDef)req.Thing.def;
            if (platform?.attachmentLinks == null)
            {
                return;
            }
            this.parentStat.TransformValue(platform.attachmentLinks, ref val);
        }
    }
}
