using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class AttachmentDef : ThingDef
    {
        public List<string> slotsTag;

        public override void PostLoad()
        {
            base.PostLoad();
            if (slotsTag == null)
                slotsTag = new List<string>();
        }
    }
}
