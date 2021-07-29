using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class AttachmentLink
    {
        public AttachmentDef attachment;

        public AttachmentDrawSettings drawSettings;

        //[Unsaved(false)]
        //public Material material;  

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

        private Vector3? _offset;
        public Vector3 Offset
        {
            get
            {
                if (drawSettings == null) return Vector3.zero;
                return (_offset ?? (_offset = new Vector3(this.drawSettings.offset.x, 0, this.drawSettings.offset.y))).Value;
            }
        }
    }
}
