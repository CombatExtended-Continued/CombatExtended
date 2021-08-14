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
        public float drawScale = 1f;
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
            if (!HasDrawOffset && drawScale == 1.0f)
                return;

            attachmentMat = new Material(attachment.attachmentGraphicData.Graphic.MatSingle);                       
            attachmentMat.mainTextureOffset = drawOffset;
            attachmentMat.mainTextureScale = new Vector2(2f - drawScale, 2f -  drawScale);            

            if (HasOutline)
            {                
                outlineMat = new Material(attachment.outlineGraphicData.Graphic.MatSingle);                
                outlineMat.mainTextureOffset = drawOffset;
                outlineMat.mainTextureScale = new Vector2(2f -  drawScale, 2f - drawScale);                ;
            }
        }

        private static readonly Color zeros = new Color(0f, 0f, 0f, 0f); 

        // NOTE:
        // 
        // If the scaling above is broken at some point use these below to scale manually.
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
        //}
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
        //}
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
