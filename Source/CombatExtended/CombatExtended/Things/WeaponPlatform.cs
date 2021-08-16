using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class WeaponPlatform : ThingWithComps, IThingHolder
    {        
        public ThingOwner<Thing> attachments;

        private List<WeaponPlatformDef.WeaponGraphicPart> _defaultPart = new List<WeaponPlatformDef.WeaponGraphicPart>();

        private List<AttachmentDef> _additionList = new List<AttachmentDef>();
        private List<AttachmentDef> _removalList = new List<AttachmentDef>();
        private List<AttachmentDef> _targetConfig = new List<AttachmentDef>();

        /// <summary>
        /// The config that this weapon should have. Used for billing.
        /// </summary>
        public List<AttachmentDef> TargetConfig
        {
            get
            {
                return _targetConfig.ToList();
            }
            set
            {
                _targetConfig = value;
                UpdateConfiguration();
            }
        }      

        private AttachmentLink[] _curLinks;
        /// <summary>
        /// Return the current attachments links
        /// </summary>
        public AttachmentLink[] CurLinks
        {
            get
            {
                if(_curLinks == null || _curLinks.Count() != attachments.Count)
                    UpdateConfiguration();
                return _curLinks;
            }
        }

        /// <summary>
        /// Wether the target config match the current loadout
        /// </summary>
        public bool ConfigApplied
        {
            get
            {                                
                return _additionList.Count == 0 && _removalList.Count == 0;
            }
        }

        private CompEquippable _compEquippable;
        public CompEquippable CompEquippable
        {
            get
            {
                if (_compEquippable == null)
                    _compEquippable = GetComp<CompEquippable>();
                return _compEquippable;
            }
        }

        /// <summary>
        /// The wielder pawn
        /// </summary>
        public Pawn Wielder
        {
            get
            {
                if (CompEquippable == null || CompEquippable.PrimaryVerb == null || CompEquippable.PrimaryVerb.caster == null
                    || ((CompEquippable?.parent?.ParentHolder as Pawn_InventoryTracker)?.pawn is Pawn holderPawn && holderPawn != CompEquippable?.PrimaryVerb?.CasterPawn))                
                    return null;                
                return CompEquippable.PrimaryVerb.CasterPawn;
            }
        }

        /// <summary>
        /// Attachments that need to be removed
        /// </summary>
        public List<AttachmentDef> RemovalList
        {
            get
            {
                return _removalList;
            }
        }

        /// <summary>
        /// Attachments that need to be added
        /// </summary>
        public List<AttachmentDef> AdditionList
        {
            get
            {
                return _additionList;
            }
        }

        public List<WeaponPlatformDef.WeaponGraphicPart> VisibleDefaultParts
        {
            get
            {
                if (_defaultPart == null)
                    _defaultPart = new List<WeaponPlatformDef.WeaponGraphicPart>();
                return _defaultPart;
            }
        }
      
        private WeaponPlatformDef _platformDef;
        public WeaponPlatformDef Platform
        {
            get
            {
                if (_platformDef == null)
                    _platformDef = (WeaponPlatformDef)def;
                return _platformDef;
            }
        }

        private Dictionary<AttachmentDef, AttachmentLink> _LinkByDef = new Dictionary<AttachmentDef, AttachmentLink>();
        public Dictionary<AttachmentDef, AttachmentLink> LinkByDef
        {
            get
            {
                if(_LinkByDef.Count != Platform.attachmentLinks.Count)
                {
                    _LinkByDef.Clear();
                    foreach (AttachmentLink link in Platform.attachmentLinks)
                        _LinkByDef.Add(link.attachment, link);
                }
                return _LinkByDef;
            }
        }

        public WeaponPlatform()
        {
            this.attachments = new ThingOwner<Thing>(this);            
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.attachments;
        }

        public override void ExposeData()
        {
            base.ExposeData();           
            Scribe_Collections.Look(ref _additionList, "additionList", LookMode.Def);
            if (_additionList == null)
                _additionList = new List<AttachmentDef>();
            Scribe_Collections.Look(ref _removalList, "removalList", LookMode.Def);
            if (_removalList == null)
                _removalList = new List<AttachmentDef>();
            Scribe_Deep.Look(ref this.attachments, "attachments", this);
            Scribe_Collections.Look(ref this._targetConfig, "targetConfig", LookMode.Def);
            if (this._targetConfig == null)
                this._targetConfig = new List<AttachmentDef>();
            if (Scribe.mode != LoadSaveMode.Saving)
                UpdateConfiguration();
        }

        /// <summary>
        /// Return the attachments we need to either remove or add
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AttachmentDef> GetModificationList()
        {
            return AdditionList.Concat(RemovalList);            
        }                

        /// <summary>
        /// Create a random set of attachments
        /// </summary>
        public virtual void RandomiseAttachments()
        {
            if (Prefs.DevMode)
            {
                List<AttachmentDef> available = Platform.attachmentLinks
                                                           .Select(a => a.attachment)
                                                           .InRandomOrder()
                                                           .ToList();
                for (int i = 0; i < available.Count; i++)
                {
                    if (Rand.Chance(0.5f) && !attachments.Any(a => ((AttachmentDef)a.def).slotTags.Any(s => available[i].slotTags.Contains(s))))
                    {
                        this._targetConfig.Add(available[i]);
                        Thing attachment = ThingMaker.MakeThing(available[i]);
                        attachments.TryAdd(attachment);
                    }
                }
            }
            this.UpdateConfiguration();
        }       

        public override void PostPostMake()
        {
            base.PostPostMake();
            this.RandomiseAttachments();
        }

        /// <summary>
        /// Get attachment link for a given attachment def
        /// </summary>
        /// <param name="def">Attachment def</param>
        /// <returns>Said attachment link</returns>
        public AttachmentLink GetLink(AttachmentDef def)
        {
            return LinkByDef.TryGetValue(def, out var link) ? link : null;
        }

        /// <summary>
        /// Used to update the internel config lists
        /// </summary>
        public void UpdateConfiguration()
        {
            _removalList.Clear();
            _additionList.Clear();
            /*
             * <=========   attachments =========> 
             */
            _curLinks = attachments.Select(t => LinkByDef[t.def as AttachmentDef]).ToArray();

            foreach (AttachmentLink link in Platform.attachmentLinks)
            {
                AttachmentDef def = link.attachment;
                bool inConfig = _targetConfig.Any(d => d.index == def.index);
                bool inContainer = attachments.Any(thing => thing.def.index == def.index);
                if (inConfig && !inContainer)                
                    _additionList.Add(def);                    
                else if (!inConfig && inContainer)
                    _removalList.Add(def);                                    
            }
            /*
             * <========= default parts =========> 
             */
            _defaultPart.Clear();
            foreach (WeaponPlatformDef.WeaponGraphicPart part in Platform.defaultGraphicParts)
            {
                /*
                 * We add default parts that enable use to change the graphics of the weapon
                 */
                if (part.slotTags == null || part.slotTags.All(s => _curLinks.All(l => !l.attachment.slotTags.Contains(s))))                
                    _defaultPart.Add(part);                                
            }           
        }
    }
}
