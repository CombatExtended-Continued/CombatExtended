using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class WeaponPlatformDef : ThingDef
    {
        public List<AttachmentLink> attachments;

        public override void PostLoad()
        {
            base.PostLoad();
            //if (attachments == null)
            //{
            //    attachments = new List<AttachmentLink>();
            //    return;
            //}
            //Texture2D weapon = (Texture2D)this.graphicData.Graphic.MatSingle.mainTexture;
            //foreach (AttachmentLink link in attachments)
            //{
            //    AttachmentDef attachmentDef = link.attachment;
            //    Texture2D result = new Texture2D(weapon.width, weapon.height);
            //    Texture2D attachment = (Texture2D)attachmentDef.graphicData.Graphic.MatSingle.mainTexture;
            //    for (int i = 0; i < result.width; i++)
            //    {
            //        for (int j = 0; j < result.height; j++)
            //        {
            //            Color bottom = weapon.GetPixel(i, j);
            //            if (bottom.a == 0) continue;
            //            Color top = attachment.GetPixel(i, j);
            //            if (top.a == 0) continue;
            //            if (bottom.r == 0 && bottom.b == 0 && bottom.g == 0) bottom = top;
            //            result.SetPixel(i, j, bottom);
            //        }
            //    }
            //    link.material = new Material(attachmentDef.graphicData.Graphic.MatSingle);
            //    link.material.mainTexture = (Texture2D)result;
            //}
        }
    }
}
