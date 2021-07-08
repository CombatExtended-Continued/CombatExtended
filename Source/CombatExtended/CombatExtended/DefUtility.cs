using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public static class DefUtility
    {
        /// <summary>
        /// A bitmap that store flags. The real size of this one is 2048 byte.
        /// </summary>
        private static FlagArray isVisibleLayerArray = new FlagArray(ushort.MaxValue);

        /// <summary>
        /// Used to create and initialize def related flags that are often checked but require more than 2 or 3 steps to caculate.
        /// Should be only called when all defs are loaded.
        /// </summary>
        public static void Initialize()
        {
            // Process all apparel defs
            foreach (ApparelLayerDef layer in DefDatabase<ApparelLayerDef>.AllDefs)
                ProcessApparelLayer(layer);

            // Process all apparel defs
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel))
                ProcessApparel(def);
        }

        /// <summary>
        /// Check is this apparel is in shell or an outer visible layer
        /// </summary>
        /// <param name="def">Apparel def</param>
        /// <returns>Visability</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVisibleLayer(this ThingDef def)
        {
            if (!def.IsApparel) throw new ArgumentException("Argument need to be apparel!");
            return isVisibleLayerArray[def.index];
        }

        /// <summary>
        /// Check is this apparellayer is in shell or an outer visible layer
        /// </summary>
        /// <param name="def">Apparel layer def</param>
        /// <returns>Visability</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVisibleLayer(this ApparelLayerDef def)
        {
            return isVisibleLayerArray[def.index];
        }

        /// <summary>
        /// Prepare apparel def by caching isVisibleLayer.
        /// </summary>
        /// <param name="def">Apparel def</param>        
        private static void ProcessApparel(ThingDef def)
        {
            ApparelLayerDef layer = def.apparel.LastLayer;

            isVisibleLayerArray[def.index] = isVisibleLayerArray[layer.index]; // set isVisibleLayerArray from the layer index since layers are processed first.
        }

        /// <summary>
        /// Prepare apparellayerdef def by caching isVisibleLayer.
        /// </summary>
        /// <param name="def">Apparel def</param>        
        private static void ProcessApparelLayer(ApparelLayerDef layer)
        {
            isVisibleLayerArray[layer.index] = IsVisibleLayer_Internal(layer);
        }

        /// <summary>
        /// Used rendering of CE custom apparel layers.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private static bool IsVisibleLayer_Internal(ApparelLayerDef layer)
        {
            /* 
             * Belt is not actually a pre-shell layer, but we want to treat it as such in this patch,
             * to avoid rendering bugs with utility items (e.g: broadshield pack)                        
             */
            return true
                && layer.drawOrder >= ApparelLayerDefOf.Shell.drawOrder
                && layer != ApparelLayerDefOf.Belt
                && !(layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false);
        }
    }
}
