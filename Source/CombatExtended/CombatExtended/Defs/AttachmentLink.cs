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
        public Vector2 drawScale = Vector2.one;
        public Vector2 drawOffset = Vector2.zero;        

        public List<StatModifier> statOffsets;
        public List<StatModifier> statMultipliers;
        public List<StatModifier> statReplacers;        
      
        private WeaponPlatformDef parent;

        public Mesh meshTop;
        public Mesh meshBot;

        public Mesh meshFlipTop;
        public Mesh meshFlipBot;

        public WeaponPlatformDef Parent
        {
            get
            {
                return parent;
            }
        }


        /// <summary>
        /// Wether the attachmnet uses an outline
        /// </summary>
        public bool HasOutline
        {
            get
            {
                return HasAttachmentMat && attachment.outlineGraphicData != null;
            }
        }

        /// <summary>
        /// Wether the attachmnet has any graphic data
        /// </summary>
        public bool HasAttachmentMat
        {
            get
            {
                return attachment.attachmentGraphicData != null;
            }
        }

        /// <summary>
        /// Return the actual attachment mat.
        /// </summary>
        public Material AttachmentMat
        {
            get
            {
                return attachment.attachmentGraphicData.Graphic.MatSingle;
            }
        }

        /// <summary>
        /// Return the attachment outline.
        /// </summary>
        public Material OutlineMat
        {
            get
            {
                return attachment.outlineGraphicData.Graphic.MatSingle;
            }
        }

        /// <summary>
        /// Return the drawOffset.
        /// </summary>
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

        /// <summary>
        /// Used to prepare and update the meshs used in rendering so we can apply both scaling and offsets at the level of vertices.
        /// This should be called when ever a change to scale or offset happen.
        /// </summary>
        /// <param name="parent"></param>
        public void PrepareTexture(WeaponPlatformDef parent)
        {
            this.parent = parent;                       
            this.meshTop = CE_MeshMaker.NewPlaneMesh(offset: this.drawOffset, scale: this.drawScale, CE_MeshMaker.DEPTH_TOP);
            this.meshBot = CE_MeshMaker.NewPlaneMesh(offset: this.drawOffset, scale: this.drawScale, CE_MeshMaker.DEPTH_BOT);
            this.meshFlipTop = CE_MeshMaker.NewPlaneMesh(offset: this.drawOffset, scale: this.drawScale, CE_MeshMaker.DEPTH_TOP, true);
            this.meshFlipBot = CE_MeshMaker.NewPlaneMesh(offset: this.drawOffset, scale: this.drawScale, CE_MeshMaker.DEPTH_BOT, true);          
        }

        /// <summary>
        /// Used to determine if this is compatible with another attachment
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CompatibleWith(AttachmentLink other)
        {
            return parent.AttachmentsCompatible(this.attachment, other.attachment);
        }

        // NOTE:
        //
        // Put the code below in PrepareTexture
        //
        // attachmentMat = new Material(attachment.attachmentGraphicData.Graphic.MatSingle);                       
        // attachmentMat.mainTextureOffset = drawOffset;
        // attachmentMat.mainTextureScale = new Vector2(2f - drawScale, 2f -  drawScale);            
        //
        // if (HasOutline)
        // {                
        //    outlineMat = new Material(attachment.outlineGraphicData.Graphic.MatSingle);                
        //    outlineMat.mainTextureOffset = drawOffset;
        //    outlineMat.mainTextureScale = new Vector2(2f -  drawScale, 2f - drawScale);                ;
        // }
        //
        // If the scaling above is broken at some point use these below to scale manually.
        //
        // private static readonly Color zeros = new Color(0f, 0f, 0f, 0f);
        //
        // private static Texture2D LoadAndScale(string path, float scale)
        // {                        
        //    Texture2D scaled = LoadTexture(path);
        //    Texture2D tex = new Texture2D(scaled.width, scaled.height, TextureFormat.Alpha8, mipChain: true);
        //    scaled = ScaleTexture(scaled, (int)(scaled.width * scale), (int)(scaled.height * scale));            
        //    int iMax = (int)Math.Min(tex.width, scaled.width);
        //    int jMax = (int)Math.Min(tex.height, scaled.height);
        //    for (int i = 0; i < tex.width; i++)
        //    {
        //        for (int j = 0; j < tex.height; j++)
        //            tex.SetPixel(i, j, zeros);
        //    }
        //    for (int i = 0;i < iMax; i++)
        //    {                
        //        for (int j = 0; j < jMax; j++)                                   
        //            tex.SetPixel(i, j, scaled.GetPixel(i, j));                
        //    }
        //    tex.Compress(highQuality: true);            
        //    tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        //    return tex;
        // }
        // private static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        // {
        //    Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
        //    float incX = (1.0f / (float)targetWidth);
        //    float incY = (1.0f / (float)targetHeight);
        //    for (int i = 0; i < result.height; ++i)
        //    {
        //        for (int j = 0; j < result.width; ++j)
        //        {
        //            Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
        //            result.SetPixel(j, i, newColor);
        //        }
        //    }
        //    result.Apply();
        //    return result;
        // }
        // private static Texture2D LoadTexture(string path)
        // {
        //    if (!File.Exists(path))
        //    {
        //        path =  Path.Combine(Controller.content.RootDir, Path.Combine("Textures", path + ".png"));
        //        if(Prefs.LogVerbose)
        //            Log.Message($"CE: fixed graphic path {path}");
        //    }
        //    Texture2D texture2D = null;
        //    if (File.Exists(path))
        //    {
        //        byte[] data = File.ReadAllBytes(path);
        //        texture2D = new Texture2D(2, 2, TextureFormat.Alpha8, mipChain: true);
        //        texture2D.LoadImage(data);
        //        texture2D.Compress(highQuality: true);                
        //        texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: false);
        //    }
        //    return texture2D;
        // }
    }
}
