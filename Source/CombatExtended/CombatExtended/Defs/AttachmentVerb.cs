using System;
namespace CombatExtended
{
    /// <summary>
    /// Used a config for the verb attached to an attachment/
    /// </summary>
    public class AttachmentVerb
    {
        /// <summary>
        /// Attachment ammo config.
        /// </summary>
        public class AttachmentVerb_AmmoUserProperties
        {
            public int magazineSize;
            public bool reloadOneAtATime;
            public float reloadTime;
            public AmmoSetDef ammoSet;
        }

        /// <summary>
        /// The inner verb.
        /// </summary>
        public VerbPropertiesCE verb;
        /// <summary>
        /// Ammo config.
        /// </summary>
        public AttachmentVerb_AmmoUserProperties ammo;
    }
}
