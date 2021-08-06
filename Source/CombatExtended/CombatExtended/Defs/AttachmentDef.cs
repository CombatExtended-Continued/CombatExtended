using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class AttachmentDef : ThingDef
    {
        public List<string> slotTags;

        public List<string> attachmentTags;

        public GraphicData attachmentGraphicData;

        public GraphicData outlineGraphicData;

        public override void PostLoad()
        {
            base.PostLoad();
            if (slotTags == null)
                slotTags = new List<string>();
            if (attachmentTags == null)
                attachmentTags = new List<string>();
        }
    }
}
