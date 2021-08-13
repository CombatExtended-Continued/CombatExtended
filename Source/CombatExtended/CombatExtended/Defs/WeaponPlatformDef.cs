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
        /// <summary>
        /// Contain attachmentlinks which are the binder for attachments
        /// </summary>
        public List<AttachmentLink> attachmentLinks;

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
