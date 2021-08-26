using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatPart_Attachments : StatPart
    {
        public StatPart_Attachments()
        {
        }       

        public override string ExplanationPart(StatRequest req)
        {
            WeaponPlatform platform;
            if (!req.HasThing || !(req.Thing?.def is WeaponPlatformDef))
            {
                return "";
            }
            platform = (WeaponPlatform)req.Thing;
            if (platform.Platform?.attachmentLinks == null)
            {
                return "";
            }
            return this.parentStat.ExplainAttachmentsStat(platform.CurLinks);
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            WeaponPlatform platform;
            if (!req.HasThing || !(req.Thing?.def is WeaponPlatformDef))
            {                
                return;
            }            
            platform = req.Thing as WeaponPlatform;
            if (platform == null || platform.Platform.attachmentLinks == null)
            {
                return;
            }
            this.parentStat.TransformValue(platform.CurLinks.ToList(), ref val);
        }
    }
}
