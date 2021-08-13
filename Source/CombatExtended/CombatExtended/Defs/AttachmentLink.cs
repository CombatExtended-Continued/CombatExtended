using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class AttachmentLink
    {
        public AttachmentDef attachment;
        public Vector2 drawOffset = Vector2.zero;        

        public List<StatModifier> statOffsets;
        public List<StatModifier> statMultipliers;
        public List<StatModifier> statReplacers;        

        private Material attachmentMat;
        private Material outlineMat;
        private WeaponPlatformDef parent;        

        public WeaponPlatformDef Parent
        {
            get
            {
                return parent;
            }
        }

        public bool HasOutline
        {
            get
            {
                return attachment.outlineGraphicData != null || outlineMat != null;
            }
        }

        public bool HasAttachmentMat
        {
            get
            {
                return attachment.attachmentGraphicData != null || attachmentMat != null;
            }
        }

        public Material AttachmentMat
        {
            get
            {
                return attachmentMat ?? attachment.attachmentGraphicData.Graphic.MatSingle;
            }
        }

        public Material OutlineMat
        {
            get
            {
                return outlineMat ?? attachment.outlineGraphicData.Graphic.MatSingle;
            }
        }

        public bool HasDrawOffset
        {
            get
            {
                return drawOffset.x != 0 || drawOffset.y != 0;
            }
        }

        private Texture2D _UIAttachmentTex = null;
        public Texture2D UIAttachmentTex
        {
            get
            {
                if (_UIAttachmentTex == null)
                    _UIAttachmentTex = (Texture2D)AttachmentMat.mainTexture;
                return _UIAttachmentTex;
            }
        }

        private Texture2D _UIOutlineTex = null;
        public Texture2D UIOutlineTex
        {
            get
            {
                if (_UIOutlineTex == null)
                    _UIOutlineTex = (Texture2D)OutlineMat.mainTexture;                
                return _UIOutlineTex;
            }
        }        

        public void PrepareTexture(WeaponPlatformDef parent)
        {
            this.parent = parent;
            if (!HasDrawOffset)
                return;            

            attachmentMat = new Material(attachment.attachmentGraphicData.Graphic.MatSingle);           
            attachmentMat.mainTextureOffset = drawOffset;            

            if (HasOutline)
            {                
                outlineMat = new Material(attachment.outlineGraphicData.Graphic.MatSingle);                
                outlineMat.mainTextureOffset = drawOffset;                                
            }
        }        

        // Incase the above method stop working
        // 
        // private Texture2D LoadTexture(string path)
        // {
        //    Texture2D texture2D = null;
        //    if (File.Exists(path))
        //    {
        //        byte[] data = File.ReadAllBytes(path);
        //        texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, mipChain: true);
        //        texture2D.LoadImage(data);
        //        texture2D.Compress(highQuality: true);
        //        texture2D.name = Path.GetFileNameWithoutExtension(attachment.defName + Path.GetFileName(path)) + "_CE_" + this.parent.defName;
        //        texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        //    }
        //    return texture2D;
        // }
    }
}
