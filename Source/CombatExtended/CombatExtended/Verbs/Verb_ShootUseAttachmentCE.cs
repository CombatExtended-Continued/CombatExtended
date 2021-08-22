using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootUseAttachment : Verb_LaunchProjectileCE
    {
        public WeaponPlatform Weapon
        {
            get
            {
                return EquipmentSource as WeaponPlatform;
            }
        }

        public bool Enabled
        {
            get
            {
                if (VerbPropsCE.requiresAttachment == null)
                    throw new Exception("CE: Verb_AttachmentShootCE cannot have requiresAttachment in VerbPropsCE be null");
                    
                List<AttachmentLink> links =  Weapon.attachments;
                for (int i =0; i < links.Count; i++)
                {
                    if (links[i].attachment == VerbPropsCE.requiresAttachment)
                        return true;
                }
                return false;
            }
        }

        public override int ShotsPerBurst
        {
            get
            {
                return VerbPropsCE.burstShotCount;
            }
        }

        public override ThingDef Projectile
        {
            get
            {
                return verbProps.defaultProjectile;
            }
        }

        public Verb_ShootUseAttachment()
        {
        }       

        public override bool IsUsableOn(Thing target)
        {
            return Enabled && base.IsUsableOn(target);
        }

        public override bool Available()
        {            
            return Enabled && base.Available();
        }      
    }
}
