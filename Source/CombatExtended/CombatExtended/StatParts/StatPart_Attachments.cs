using System;
using System.Linq;
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
            WeaponPlatform platform;
            if (!req.HasThing || !(req.Thing?.def is WeaponPlatformDef))
            {                
                return;
            }            
            platform = (WeaponPlatform)req.Thing;
            if (platform.Platform?.attachmentLinks == null)
            {
                return;
            }
            this.parentStat.TransformValue(platform.CurLinks.ToList(), ref val);
        }
    }
}
