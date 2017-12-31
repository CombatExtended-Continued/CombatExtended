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
        
        // Color[] arrays are left-to-right, BOTTOM-TO-TOP
        /* 
         * _____________
         * |a|b|_| |_|y|X|
         * |_|_|_| |_|_|z|
         * |_|_|_| |_|_|_|
         * |_ _ _   _ _ _|
         * |v|_|_| |_|_|_|
         * |w|_|_| |_|_|_|
         * |0|1|_|_|_|_|m|
         * 
         * 0: first index
         * 1: second index
         * 
         * m: index width - 1
         * w: index width
         * v: index 2 * width
         * 
         * z: last index - width
         * a: last index - width + 1
         * b: last index - width + 2
         * 
         * y: first-to-last index
         * X: last index
         * 
         *--------------------------------
         * 
         * Meanwhile, the heightRange output must be left-to-right, TOP-TO-BOTTOM
         * 
         */
        
        public static IntRange CropVertical(Color[] array, int width, int height)
        {
        	var heightRange = new IntRange(0, height - 1); // min: top row, max: bottom row
        	
        	  // Shifting the max up
        	int i = 0;				// bottom left pixel (0)
        	while (array[i].a < alphaThreshold)
        	{
        		i++;					// pixel one to the right (1, .. , m)
        		if (i % width == 0)		// previous pixel was the last in the row (m)
        		{
        			heightRange.max--;	// nothing found in this row, shift one up (B-t-T) / down (T-t-B)
        		}
        	}
        	
        	  // Shifting the min down
        	i = array.Length - 1;	// top right pixel (X)
        	while (array[i].a < alphaThreshold)
        	{
        		if (i % width == 0)		// previous pixel was the first in the row (a)
        		{
        			heightRange.min++;	// nothing found on this row, shift one down (B-t-T) / up (T-t-B)
        		}
        		i--;					// pixel one to the left (y, .. , b, a)
        	}
        	return heightRange;
        }
        
        public static IntRange CropHorizontal(Color[] array, int width, int height)
        {
        	var widthRange = new IntRange(0, width - 1);
        	
        	  // Shifting the min up
        	int i = 0;			// bottom left pixel (0)
        	while (array[i].a < alphaThreshold)
        	{
        		i += width; 			// pixel one above (w, v, .. , a)
        		if (i >= array.Length)	// last pixel was highest in the column (a)
        		{
        			widthRange.min++;	// nothing found in this column, shift one to the right
        			i = widthRange.min;	// pixel one to the right of the lowest in the previous column (1)
        		}
        	}
        	
        	  // Shifting the max down
        	i = array.Length - 1;		// top right, last index (X)
        	while (array[i].a < alphaThreshold)
        	{
        		i -= width;				// pixel one below (z, .. , m)
        		if (i <= 0)				// last pixel was lowest in the column (m)
        		{
        			widthRange.max--;	// nothing found in this column, shift one to the left
        			i = array.Length - (width - widthRange.max);
        								// pixel one to the left of the highest in the previous column (y)
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