using System;
using UnityEngine;
using Verse;

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
        /// <summary>
        /// Path to UI icon
        /// </summary>
        public string iconPath;
        /// <summary>
        /// The icon texture
        /// </summary>
        [Unsaved(true)]
        public Texture2D iconTex;       
    }
}
