using System;
using System.ComponentModel;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public abstract class StatPart_Attechmenets : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || !(req.Thing is WeaponPlatform))
                return;
            WeaponPlatform platform = (WeaponPlatform)req.Thing;
            if (platform == null || platform.attachments == null)
                return;
            for (int i = 0; i < platform.attachments.Count; i++)
                Transform(platform.attachments[i], ref val);
        }

        protected abstract void Transform(Thing attachment, ref float val);
    }
}
