using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class AttachmentLink
    {
        public class AttachmentDrawSettings
        {
            public Vector2 offset = Vector2.zero;
        }

        public bool HasOffsets
        {
            get
            {
                return drawSettings != null;
            }
        }

        //[Unsaved(false)]
        //public Material material;  

        public AttachmentDef attachment;

        public AttachmentDrawSettings drawSettings;
    }
}
