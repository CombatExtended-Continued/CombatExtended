using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public class WeaponPlatform_VerbManager
    {
        private const int VERB_EXPIRE_TICKS = 5000;

        private int altVerbIndex = -1;        
        private int altVerbExpireAt = -1;

        public List<Attachment_AmmoUser> ammoUsers;

        public readonly WeaponPlatform weapon;      

        /// <summary>
        /// return the actucal verbTracker from Compequippable
        /// </summary>
        private VerbTracker VerbTracker
        {
            get
            {
                return Equippable.verbTracker;
            }
        }

        /// <summary>
        /// return the source weapon CompEquippable
        /// </summary>
        public CompEquippable Equippable
        {
            get
            {
                return weapon.Equippable;
            }
        }

        /// <summary>
        /// Get the number of verbs available
        /// </summary>
        public int VerbCount
        {
            get
            {
                return weapon.def.verbs?.Count ?? 0;
            }
        }

        /// <summary>
        /// Return the original Primary verb of this weapon
        /// </summary>
        public Verb PrimaryVerb
        {
            get
            {
                VerbTracker tracker = VerbTracker;
                if (tracker.verbs == null)                
                    tracker.InitVerbsFromZero();                
                for (int i = 0; i < tracker.verbs.Count; i++)
                {
                    Verb verb = tracker.verbs[i];
                    if (verb.verbProps.isPrimary && !(verb is Verb_ShootUseAttachment))                    
                        return tracker.verbs[i];                    
                }
                return null;
            }
        }
        
        /// <summary>
        /// Return the current selected verb or the default verb for this weapon.
        /// </summary>
        public Verb SelectedVerb
        {
            get
            {
                if (altVerbIndex < 0 || altVerbIndex > VerbCount || GenTicks.TicksGame > altVerbExpireAt || altVerbExpireAt == -1)
                {
                    altVerbIndex = altVerbExpireAt =  -1;                    
                    return PrimaryVerb;
                }
                VerbProperties props = weapon.def.verbs[altVerbIndex];
                return VerbTracker.verbs.FirstOrFallback(v => v.verbProps == props, PrimaryVerb);
            }
            set
            {                
                if (value == PrimaryVerb || value == null)
                {
                    altVerbIndex = -1;
                    altVerbExpireAt = -1;
                }
                else
                {
                    altVerbIndex = weapon.def.verbs.IndexOf(value.verbProps);
                    altVerbExpireAt = GenTicks.TicksGame + VERB_EXPIRE_TICKS;
                }
            }
        }        

        public WeaponPlatform_VerbManager(WeaponPlatform weapon)
        {
            this.weapon = weapon;
        }      

        public void ExposeData()
        {
            Scribe_Values.Look(ref altVerbIndex, "altVerbIndex", -1);
            Scribe_Values.Look(ref altVerbExpireAt, "altVerbExpireAt", -1);
            Scribe_Collections.Look(ref ammoUsers, "ammoManagers", LookMode.Deep);
            if(ammoUsers == null)           
                InitializeAmmoManagers();
            foreach (Attachment_AmmoUser ammoUser in ammoUsers)
                ammoUser.verbManager = this;
        }

        /// <summary>
        /// Used to initialize ammo managers for verbs from attachments
        /// </summary>
        public void InitializeAmmoManagers()
        {
            // first remove old useless managers
            ammoUsers ??= new List<Attachment_AmmoUser>();
            ammoUsers.RemoveAll(a => !weapon.attachments.Any(l => l.attachment == a.sourceAttachment));
            foreach (AttachmentLink link in weapon.attachments)
            {
                if (link.attachment.attachmentVerb != null && !ammoUsers.Any(a => a.sourceAttachment == link.attachment))
                {
                    ammoUsers.Add(new Attachment_AmmoUser()
                    {
                        sourceAttachment = link.attachment,                        
                        verbManager = this
                    });                    
                }
            }
        }

        /// <summary>
        /// return the ammoUser related to this attachment.
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public Attachment_AmmoUser GetAmmoUser(AttachmentDef attachment)
        {
            return ammoUsers.FirstOrFallback(a => a.sourceAttachment == attachment, null);  
        }
    }
}
