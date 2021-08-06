using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using HarmonyLib;
using Verse;

namespace CombatExtended
{
    public class WeaponPlatform : ThingWithComps, IThingHolder
    {
        public ThingOwner<Thing> attachments;

        private AttachmentDef[] currentAttachments;
        private AttachmentLink[] currentLinks;

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

        private AttachmentDef[] _availableAttachments;
        public AttachmentDef[] AvailableAttachments
        {
            get
            {
                if (_availableAttachments == null)
                    _availableAttachments = Platform.attachments.Select(a => a.attachment).ToArray();
                return _availableAttachments;
            }
        }

        public AttachmentLink[] CurLinks
        {
            get
            {
                if (currentAttachments == null || currentAttachments.Length < attachments.Count) Rebuild();
                else
                {
                    int i = 0;
                    foreach (var thing in attachments)
                    {
                        if (thing.def != currentAttachments[i++])
                        {
                            Rebuild();
                            break;
                        }
                    }
                }
                return currentLinks;
            }
        }

        public AttachmentDef[] CurAttachmentsDef
        {
            get
            {
                if (currentAttachments == null || currentAttachments.Length != attachments.Count) Rebuild();
                else
                {
                    int i = 0;
                    foreach (var thing in attachments)
                    {
                        if (thing.def != currentAttachments[i++])
                        {
                            Rebuild();
                            break;
                        }
                    }
                }
                return currentAttachments;
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
            Scribe_Deep.Look(ref this.attachments, "attachments", this);
            if (Scribe.mode != LoadSaveMode.Saving)
                Rebuild();
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            AddRandomAttachments();
        }

        public void AddRandomAttachments()
        {
            AttachmentDef[] available = AvailableAttachments;
            for (int i = 0; i < available.Length; i++)
            {
                if (Rand.Chance(0.5f))
                {
                    Thing attachment = ThingMaker.MakeThing(available[i]);
                    attachments.TryAdd(attachment);
                }
            }
            Rebuild();
        }

        private void Rebuild()
        {
            currentAttachments = new AttachmentDef[attachments.Count];
            currentLinks = new AttachmentLink[attachments.Count];
            int i = 0;
            foreach (Thing attachment in attachments)
            {
                currentAttachments[i] = (AttachmentDef)attachment.def;
                currentLinks[i] = Platform.attachments.First(a => a.attachment == attachment.def);
                i++;
            }
        }
    }
}
