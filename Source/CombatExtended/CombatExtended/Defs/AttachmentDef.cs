using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class AttachmentDef : ThingDef
    {        
        #region Attachments config

        /// <summary>
        /// Slot tags won't overlap on a given weapon.
        /// </summary>
        public List<string> slotTags;
        /// <summary>
        /// Used for AI loadout generation
        /// </summary>
        public List<string> attachmentTags;
        /// <summary>
        /// Contain the configuration of any verb attached to this attachment
        /// </summary>
        public AttachmentVerb attachmentVerb;

        #endregion 

        #region InGameRenderingData

        /// <summary>
        /// Used for rendering attachment.
        /// </summary>
        public GraphicData attachmentGraphicData;
        /// <summary>
        /// Used for rendering attachment.
        /// </summary>
        public GraphicData outlineGraphicData;

        #endregion

        #region UnifiedStatModifier

        public List<StatModifier> statOffsets;        
        public List<StatModifier> statMultipliers;
        public List<StatModifier> statReplacers;

        #endregion

        [Unsaved(allowLoading = false)]
        public bool statsValidated = false;
        
        /// <summary>
        /// Used to validate and sync the important stats that are not going to be require adding a stat part for or
        /// for those in stat bases.
        /// </summary>
        public void ValidateStats()
        {
            if (statsValidated)
            {
                Log.Warning($"CE: called ValidateStats for a valid attachment stat configuration! {this.defName}");
                return;
            }

            if (this.slotTags == null)
                this.slotTags = new List<string>();
            if (this.attachmentTags == null)
                this.attachmentTags = new List<string>();

            if (this.statOffsets == null)
                this.statOffsets = new List<StatModifier>();
            if (this.statMultipliers == null)
                this.statMultipliers = new List<StatModifier>();
            if (this.statReplacers == null)
                this.statReplacers = new List<StatModifier>();

            statsValidated = true;
            StatModifier modifier;
            
            modifier = this.statBases.FirstOrFallback(s => s.stat == StatDefOf.Mass, null);
            if (modifier != null && !this.statOffsets.Any(m => m.stat == StatDefOf.Mass))
                this.statOffsets.Add(modifier);

            modifier = this.statBases.FirstOrFallback(s => s.stat == CE_StatDefOf.Bulk, null);
            if (modifier != null && !this.statOffsets.Any(m => m.stat == CE_StatDefOf.Bulk))
                this.statOffsets.Add(modifier);

            modifier = this.statBases.FirstOrFallback(s => s.stat == StatDefOf.MarketValue, null);
            if (modifier != null && !this.statOffsets.Any(m => m.stat == StatDefOf.MarketValue))
                this.statOffsets.Add(modifier);            

            modifier = this.statBases.FirstOrFallback(s => s.stat == StatDefOf.Flammability, null);
            if (modifier != null && !this.statOffsets.Any(m => m.stat == StatDefOf.Flammability))
                this.statMultipliers.Add(modifier);

            modifier = this.statBases.FirstOrFallback(s => s.stat == CE_StatDefOf.MagazineCapacity, null);
            if (modifier != null && !this.statReplacers.Any(m => m.stat == CE_StatDefOf.MagazineCapacity))
                this.statReplacers.Add(modifier);
        }
    }
}
