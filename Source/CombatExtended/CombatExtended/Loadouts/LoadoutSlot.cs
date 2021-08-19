using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;


// Considered having a Generic and Specific slot and pass that through an interface but instead of a bunch of slot.<type>Def != null there'd be a bunch of
// slot is <type>Def and slot as <type>Def... equally messy IMO and might still have some null checks tossed around.
namespace CombatExtended
{
    // this has been reduced to a thingCount at this point, with the exception of the added default count bit
    // -- Fluffy
    
    /// <summary>
    /// LoadoutSlot contains details about what items a Pawn's inventory should contain.
    /// </summary>
    public class LoadoutSlot : IExposable
    {
        #region Fields
        
        private const int _defaultCount = 1;        
        private int _count;        
        private Def _def;
        private Type _type; // to help with save/load.
        private List<AttachmentDef> _attachments = new List<AttachmentDef>();
        private LoadoutCountType _countType = LoadoutCountType.pickupDrop; // default mode for new loadout slots.

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor for Specific slots which look for specific a ThingDef in order to fulfill their storage requirements.
        /// </summary>
        /// <param name="def">ThingDef to look for</param>
        /// <param name="count">int indicating number of items of ThingDef to store.</param>
        public LoadoutSlot( ThingDef def, int count = 1 )
        {
        	_type = typeof(ThingDef);
            _count = count;
            _def = def;

            _count = _count < 1 ? 1 : _count;
        }
        
        /// <summary>
        /// Constructor for Generic slots which use a lambda to determine what is picked up and stored.
        /// </summary>
        /// <param name="def">LoadoutGenericDef to use for picking up items.</param>
        /// <param name="count">Optional int how many should be picked up. If left blank then the default value from the LoadoutGenericDef is used.</param>
        public LoadoutSlot(LoadoutGenericDef def, int count = 0)
        {
        	_type = typeof(LoadoutGenericDef);
        	if ( count < 1)
        		count = def.defaultCount;
        	
        	_count = count < 1 ? _count = 1 : _count = count;
        	_countType = def.defaultCountType;
        	_def = def;
        }

        /// <summary>
        /// Constructor exists for scribe and shouldn't generally be used.
        /// </summary>
        public LoadoutSlot()
        {
            // for scribe; if Count is set default will be overwritten. Def is always stored/loaded.
            _count = _defaultCount;
        }

        #endregion Constructors

        #region Properties

        public int count { get { return _count; } set { _count = value; } }
        public LoadoutCountType countType { get { return _countType; } set { _countType = value; } }
        public bool allowAllAttachments { get { return attachments.Count == 0; } }
        public ThingDef thingDef { get { return (_def is ThingDef) ? (ThingDef)_def : null; } }
        public WeaponPlatformDef weaponPlatformDef { get { return (_def is WeaponPlatformDef) ? (WeaponPlatformDef)_def : null; } }
        public List<AttachmentDef> attachments { get { return _attachments; } }        
        public LoadoutGenericDef genericDef { get { return (_def is LoadoutGenericDef) ? (LoadoutGenericDef)_def : null; } }
        public bool isWeaponPlatform { get { return _def is WeaponPlatformDef; } }
        public List<AttachmentLink> attachmentLinks { get { return weaponPlatformDef.attachmentLinks.Where(l => attachments.Contains(l.attachment)).ToList(); } }

        // hand out some properties/fields of internal def for when user doesn't need to know specific/generic.
        public string label { get { return _def.label; } }
        public string LabelCap { get { return _def.LabelCap; } }
        
        // hide where the bulk/mass came from.  Higher level doesn't care as long as it has a number.
        public float bulk
        {
            get
            {
                float val = (thingDef != null ? thingDef.GetStatValueAbstract(CE_StatDefOf.Bulk) : genericDef.bulk);
                // add the offsets from attachments
                if (isWeaponPlatform) CE_StatDefOf.Bulk.TransformValue(attachmentLinks, ref val);                
                return val;
            }
        }
        public float mass
        {
            get
            {
                float val = (thingDef != null ? thingDef.GetStatValueAbstract(StatDefOf.Mass) : genericDef.mass);
                // add the offsets from attachments
                if (isWeaponPlatform) StatDefOf.Mass.TransformValue(attachmentLinks, ref val);
                return val;
            }
        }
        
        //public ThingDef def { get { return _def; } set { _def = value; } }

        #endregion Properties

        #region Methods
        
        /// <summary>
        /// Used during Rimworld Save/Load.
        /// </summary>
        /// <remarks>passed by ref since during load the contents of the variable is restored from the save.</remarks>
        public void ExposeData()
        {
            Scribe_Values.Look( ref _count, "count", _defaultCount );
            Scribe_Values.Look(ref _type, "DefType");
            Scribe_Collections.Look(ref _attachments, "Attachments", LookMode.Def);
            if (_attachments == null)
                _attachments = new List<AttachmentDef>();
            ThingDef td = thingDef;
            LoadoutGenericDef gd = genericDef;
            if (_type == typeof(ThingDef))
            	Scribe_Defs.Look(ref td, "def");
            if (_type == typeof(LoadoutGenericDef))
            	Scribe_Defs.Look(ref gd, "def");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            	_def = (_type == typeof(ThingDef) ? td as Def : gd as Def);
            //Scribe_Defs.LookDef( ref _def, "def" );
            
            // when saving _def is defined.  When loading _def should have gotten it's contents by now.
            if (genericDef != null)
            	Scribe_Values.Look(ref _countType, "countType", LoadoutCountType.pickupDrop);
        }
        
        /// <summary>
        /// Used to determine if two slots are referring to the same def without exposing the internal def directly.
        /// </summary>
        /// <param name="slot">The other slot to compare to.</param>
        /// <returns>bool true if both LoadoutSlots refer to the same def.</returns>
        public bool isSameDef(LoadoutSlot slot)
        {
        	Def def = (slot.thingDef as Def) ?? (slot.genericDef as Def);
            if (isWeaponPlatform && slot.isWeaponPlatform && _def == def)
            {
                int count = slot.attachments.Intersect(attachments).Count();
                return count == slot.attachments.Count && count == attachments.Count;
            }
        	return _def == def;
        }
        
        /// <summary>
        /// Handles copying self to a new LoadoutSlot.
        /// </summary>
        /// <returns>new LoadoutSlot containing the same key properties as self.</returns>
        /// <remarks>The stored def isn't deep copied and shouldn't be.</remarks>
        public LoadoutSlot Copy()
        {
        	if (genericDef != null)
        	{
        		LoadoutSlot slot = new LoadoutSlot(genericDef, _count);
        		slot.countType = _countType;
                // add the current attachments
                if (isWeaponPlatform)
                    slot.attachments.AddRange(attachments);
        		return slot;
        	} // else if (thingDef != null)
        	return new LoadoutSlot(thingDef, _count);
        }

        #endregion Methods
    }
}