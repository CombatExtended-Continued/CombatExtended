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
        private static Dictionary<Def, Texture2D> _cachedDefIcons = new Dictionary<Def, Texture2D>();

        public static Texture2D IconTexture(this Def def)
        {
            // check cache
            if (_cachedDefIcons.ContainsKey(def))
            {
                return _cachedDefIcons[def];
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
                _cachedDefIcons.Add(def, rdef.products.First().thingDef.IconTexture());
                return _cachedDefIcons[def];
            }

            // animals need special treatment ( this will still only work for animals, pawns are a whole different can o' worms ).
            if (pdef != null)
            {
                try
                {
                    _cachedDefIcons.Add(def, (pdef.lifeStages.Last().bodyGraphicData.Graphic.MatFront.mainTexture as Texture2D).Crop());
                    return _cachedDefIcons[def];
                }
                catch
                {
                }
            }

            // if not buildable it probably doesn't have an icon.
            if (bdef == null)
            {
                _cachedDefIcons.Add(def, null);
                return null;
            }

            // if def built != def listed.
            if (
                (tdef != null) &&
                (tdef.entityDefToBuild != null)
            )
            {
                _cachedDefIcons.Add(def, tdef.entityDefToBuild.IconTexture().Crop());
                return _cachedDefIcons[def];
            }

            _cachedDefIcons.Add(def, bdef.uiIcon.Crop());
            return bdef.uiIcon.Crop();
        }

        public static Texture2D Crop(this Texture2D tex)
        {
            return tex;
        }
    }
}