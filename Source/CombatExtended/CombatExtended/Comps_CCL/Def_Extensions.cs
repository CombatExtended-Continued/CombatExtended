using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class Def_Extensions
    {
    	private static Dictionary<string, Texture2D> _cachedDefIcons = new Dictionary<string, Texture2D>();
		
        public static Texture2D IconTexture(this Def def)
        {
            // check cache
            if (_cachedDefIcons.ContainsKey(def.defName))
            {
                return _cachedDefIcons[def.defName];
            }
			
            // otherwise try to determine icon
            var bdef = def as BuildableDef;
            var tdef = def as ThingDef;
            var pdef = def as PawnKindDef;
            var rdef = def as RecipeDef;
            
            // recipes will be passed icon of first product, if defined.
            if (
                (rdef != null) &&
                (!rdef.products.NullOrEmpty())
            )
            {
        		_cachedDefIcons[def.defName] = rdef.products.First().thingDef.IconTexture();
                return _cachedDefIcons[def.defName];
            }

            // animals need special treatment ( this will still only work for animals, pawns are a whole different can o' worms ).
            if (pdef != null)
            {
                try
                {
        			_cachedDefIcons[def.defName] = (pdef.lifeStages.Last().bodyGraphicData.Graphic.MatFront.mainTexture as Texture2D).Crop();
                    
                    return _cachedDefIcons[def.defName];
                }
                catch
                {
                }
            }

            // if not buildable it probably doesn't have an icon.
            if (bdef == null)
            {
                return null;
            }

            // if def built != def listed.
            if (
                (tdef != null) &&
                (tdef.entityDefToBuild != null)
            )
            {
        		_cachedDefIcons[def.defName]= tdef.entityDefToBuild.IconTexture().Crop();
                return _cachedDefIcons[def.defName];
            }
			
            _cachedDefIcons[def.defName] = bdef.uiIcon;
            return _cachedDefIcons[def.defName];
        }
        
        /// <summary>
        /// Alpha value between 0 and 1f
        /// </summary>
        private const float alphaThreshold = 0.25f;
        
        public static IntRange CropVertical(Color[] array, int width, int height)
        {
        	var heightRange = new IntRange(0, height - 1);
        	
        		//topleft pixel
        	int i = 0;
        	while (array[i].a < alphaThreshold)
        	{
        		i++;
        			//rightmost pixel in row
        		if (i % width == 0)
        		{
        			heightRange.min++;
        		}
        	}
        		//bottomright pixel
        	i = array.Length - 1;
        	while (array[i].a < alphaThreshold)
        	{
        			//leftmost pixel in row
        		if (i % width == 0)
        		{
        			heightRange.max--;
        		}
        		i--;
        	}
        	return heightRange;
        }
        
        public static IntRange CropHorizontal(Color[] array, int width, int height)
        {
        	var widthRange = new IntRange(0, width - 1);
        	
        		//topleft pixel
        	int i = 0;
        	while (array[i].a < alphaThreshold)
        	{
        		i += width;
        			//bottommost pixel in column
        		if (i >= array.Length)
        		{
        			widthRange.min++;
        			i = widthRange.min;
        		}
        	}
        		//bottomright pixel
        	i = array.Length - 1;
        	while (array[i].a < alphaThreshold)
        	{
        		i -= width;
        			//topmost pixel in column
        		if (i <= 0)
        		{
        			widthRange.max--;
        			i = array.Length - (width - widthRange.max);
        		}
        	}
        	return widthRange;
        }
        
        public static Rect CropHorizontalVertical(Color[] array, int width, int height)
        {
        	var h = CropHorizontal(array, width, height);
        	var v = CropVertical(array, width, height);
        	return Rect.MinMaxRect(h.min, v.min, h.max, v.max);
        }
        
        public static Texture2D Crop(this Texture2D tex)
        {
        	int w; int h;
        	var array = tex.GetColorSafe(out w, out h);
        	
        	return tex.BlitCrop(CropHorizontalVertical(array, w, h));
        }
    }
}