using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WeaponPlatformDef : ThingDef
    {
        public class WeaponGraphicPart
        {
            /// <summary>
            /// Used for rendering a default part.
            /// </summary>
            public GraphicData partGraphicData;
            /// <summary>
            /// Used for rendering a default part's outline.
            /// </summary>
            public GraphicData outlineGraphicData;
            /// <summary>
            /// These tags will be used to determine if we need to render this part.
            /// It will only render if all of these are unoccupied.
            /// </summary>
            public List<string> slotTags;

            public bool HasOutline
            {
                get
                {
                    return outlineGraphicData != null;
                }
            }

            public bool HasPartMat
            {
                get
                {
                    return partGraphicData != null;
                }
            }

            private Texture2D _UIPartTex = null;
            public Texture2D UIPartTex
            {
                get
                {                    
                    if (_UIPartTex == null && HasPartMat)
                        _UIPartTex = (Texture2D)PartMat.mainTexture;
                    return _UIPartTex;
                }
            }

            private Texture2D _UIOutlineTex = null;
            public Texture2D UIOutlineTex
            {
                get
                {
                    if (_UIOutlineTex == null && HasOutline)
                        _UIOutlineTex = (Texture2D)OutlineMat.mainTexture;
                    return _UIOutlineTex;
                }
            }

            public Material PartMat
            {
                get
                {
                    return partGraphicData.Graphic.MatSingle;
                }
            }

            public Material OutlineMat
            {
                get
                {
                    return outlineGraphicData.Graphic.MatSingle;
                }
            }
        }        

        /// <summary>
        /// Contain attachmentlinks which are the binder for attachments
        /// </summary>
        public List<AttachmentLink> attachmentLinks;

        /// <summary>
        /// Contain the list WeaponGraphic parts. These are used in rendering when all of the slots they use are unoccupied
        /// </summary>
        public List<WeaponGraphicPart> defaultGraphicParts = new List<WeaponGraphicPart>();

        private Texture2D _UIWeaponTex = null;
        public Texture2D UIWeaponTex
        {
            get
            {                
                if (_UIWeaponTex == null)
                    _UIWeaponTex = (Texture2D)graphic.MatSingle.mainTexture;
                return _UIWeaponTex;
            }
        }

        public override void PostLoad()
        {            
            if (defaultGraphicParts == null)
                defaultGraphicParts = new List<WeaponGraphicPart>();
            if (attachmentLinks == null)
                attachmentLinks = new List<AttachmentLink>();

            base.PostLoad();
            // Add the inspect tab for attachments
            if (inspectorTabs == null)
                inspectorTabs = new List<Type>();
            inspectorTabs.Add(typeof(ITab_AttachmentView));

            if (inspectorTabsResolved == null)
                inspectorTabsResolved = new List<InspectTabBase>();            
            inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_AttachmentView)));
        }        

        /// <summary>
        /// Compatibility cache
        /// </summary>
        private Dictionary<Pair<AttachmentDef, AttachmentDef>, bool> _compatibilite = new Dictionary<Pair<AttachmentDef, AttachmentDef>, bool>();

        /// <summary>
        /// Used to find wether 2 attachments are compatible with each other
        /// </summary>
        /// <param name="first">First</param>
        /// <param name="second">Second</param>
        /// <returns>Wether if they are compatible</returns>
        public bool AttachmentsCompatible(AttachmentDef first, AttachmentDef second)
        {           
            if (first.index > second.index)            
                return AttachmentsCompatible(second, first);            
            Pair<AttachmentDef, AttachmentDef> key = new Pair<AttachmentDef, AttachmentDef>(first, second);
            if (_compatibilite.TryGetValue(key, out bool compatible))
                return compatible;
            return _compatibilite[key] = first.slotTags.All(s => !second.slotTags.Contains(s));
        }

        private Dictionary<Pair<AttachmentDef, WeaponGraphicPart>, bool> _removes = new Dictionary<Pair<AttachmentDef, WeaponGraphicPart>, bool>();

        /// <summary>
        /// Used to find wether an attachment will remove a default graphic part.
        /// </summary>
        /// <param name="first">First</param>
        /// <param name="second">Second</param>
        /// <returns>Wether if they are compatible</returns>
        public bool AttachmentRemoves(AttachmentDef attachment, WeaponGraphicPart part)
        {                        
            Pair<AttachmentDef, WeaponGraphicPart> key = new Pair<AttachmentDef, WeaponGraphicPart>(attachment, part);
            if (_removes.TryGetValue(key, out bool removes))
                return removes;
            return _removes[key] = attachment.slotTags.Any(s => part.slotTags.Contains(s));
        }
    
        /// <summary>
        /// Used to to cache the stat modifiers in links so we don't have to search for what is overriden
        /// </summary>
        public void PrepareStats()
        {
            if (attachmentLinks == null)
            {
                attachmentLinks = new List<AttachmentLink>();
                return;
            }
            HashSet<StatDef> stats = new HashSet<StatDef>();
            for (int i = 0; i < attachmentLinks.Count; i++)
            {
                bool processOffsets = true;
                bool processMultipliers = true;
                bool processReplaces = true;
                AttachmentLink link = attachmentLinks[i];
                // validate stats incase this was called before the attachment def has excuted postload
                if (!link.attachment.statsValidated)
                    link.attachment.ValidateStats();
                if (link.statReplacers == null)
                {
                    link.statReplacers = link.attachment.statReplacers.ToList() ?? new List<StatModifier>();
                    processReplaces = false;
                }
                // add override stats
                if (processReplaces && link.attachment.statReplacers.Count > 0)
                {
                    // copy StatModifier not in this link to this link
                    foreach (StatModifier modifier in link.attachment.statReplacers)
                    {
                        if (link.statReplacers.All(m => m.stat != modifier.stat))
                            link.statReplacers.Add(modifier);                        
                    }
                }
                if (link.statOffsets == null)
                {
                    link.statOffsets = link.attachment.statOffsets.ToList() ?? new List<StatModifier>();
                    processOffsets = false;
                }
                if (link.statMultipliers == null)
                {
                    link.statMultipliers = link.attachment.statMultipliers.ToList() ?? new List<StatModifier>();
                    processMultipliers = false;
                }
                if (processOffsets)
                {
                    // copy StatModifier not in this link to this link
                    foreach (StatModifier modifier in link.attachment.statOffsets)
                    {
                        if (link.statOffsets.All(m => m.stat != modifier.stat))
                            link.statOffsets.Add(modifier);
                    }
                }
                if (processMultipliers)
                {
                    // copy StatModifier not in this link to this link
                    foreach (StatModifier modifier in link.attachment.statMultipliers)
                    {                        
                        if (link.statMultipliers.All(m => m.stat != modifier.stat))
                            link.statMultipliers.Add(modifier);
                    }
                }
                // add a stat base with default value if it doesn't exists
                foreach (StatModifier modifier in link.attachment.statReplacers)
                {
                    if (statBases.All(s => s.stat != modifier.stat))
                        statBases.Add(new StatModifier() { value = modifier.stat.defaultBaseValue , stat = modifier.stat });
                }
                foreach (StatModifier modifier in link.attachment.statOffsets)
                {
                    if (statBases.All(s => s.stat != modifier.stat))
                        statBases.Add(new StatModifier() { value = modifier.stat.defaultBaseValue, stat = modifier.stat });
                }
                foreach (StatModifier modifier in link.attachment.statMultipliers)
                {
                    if (statBases.All(s => s.stat != modifier.stat))
                        statBases.Add(new StatModifier() { value = modifier.stat.defaultBaseValue, stat = modifier.stat });
                }
                // offset the textures
                link.PrepareTexture(this);
            }
        }
    }
}
