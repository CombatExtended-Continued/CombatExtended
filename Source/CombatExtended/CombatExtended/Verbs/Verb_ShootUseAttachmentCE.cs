using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootUseAttachment : Verb_LaunchProjectileCE
    {
        /// <summary>
        /// The parent weapon that has the parent attachment attached to.
        /// </summary>
        public WeaponPlatform Weapon
        {
            get
            {
                return EquipmentSource as WeaponPlatform;
            }
        }        
        /// <summary>
        /// Number of shots per one burst.
        /// </summary>
        public override int ShotsPerBurst
        {
            get
            {
                return VerbPropsCE.burstShotCount;
            }
        }
        /// <summary>
        /// The current projectile.
        /// </summary>
        public override ThingDef Projectile
        {
            get
            {
                return AmmoUser != null ? (AmmoUser.SelectedProjectile ?? verbProps.defaultProjectile) : verbProps.defaultProjectile;
            }
        }
        /// <summary>
        /// The parent attachment.
        /// </summary>
        public AttachmentDef Attachment
        {
            get
            {
                return VerbPropsCE.requiresAttachment;
            }
        }
        /// <summary>
        /// The parent AmmoUser. Contain data about the current attachment magazine.
        /// </summary>
        public Attachment_AmmoUser AmmoUser
        {
            get
            {
                return (EquipmentSource as WeaponPlatform).verbManager?.GetAmmoUser(Attachment) ?? null;
            }
        }
        /// <summary>
        /// Contain properties related to ammo.
        /// </summary>
        public AttachmentVerb.AttachmentVerb_AmmoUserProperties AmmoProps
        {
            get
            {
                return AmmoUser?.AmmoProps ?? null;
            }
        }
        /// <summary>
        /// Wether this verb is active or not
        /// </summary>
        public virtual bool Enabled
        {
            get
            {
                if (VerbPropsCE.requiresAttachment == null)
                    throw new Exception("CE: Verb_AttachmentShootCE cannot have requiresAttachment in VerbPropsCE be null");

                List<AttachmentLink> links = Weapon.attachments;
                for (int i = 0; i < links.Count; i++)
                {
                    if (links[i].attachment == VerbPropsCE.requiresAttachment)
                        return true;
                }                
                return false;
            }
        }

        public Verb_ShootUseAttachment()
        {            
        }

        /// <summary>
        /// Wether this verb is usable on the given target
        /// </summary>
        /// <param name="target">Target</param>
        /// <returns>Wether this verb is usable on Target</returns>
        public override bool IsUsableOn(Thing target)
        {            
            return Enabled && base.IsUsableOn(target);
        }

        /// <summary>
        /// Wether this verb is available. Include ammo checks.
        /// </summary>
        /// <returns>Wether available or not</returns>
        public override bool Available()
        {            
            if (AmmoUser != null && !AmmoUser.HasMagazine)
            {
                // if the pawn have ammo, start a reload job for this attachment.
                if (AmmoUser.HasAmmo)
                    AmmoUser.TryStartReload();
                else
                    AmmoUser.Notify_OutOfAmmo();
                return false;
            }
            return Enabled && base.Available();
        }

        /// <summary>
        /// Will attempt to start the casting process on a target. This is done before the warmup
        /// </summary>
        /// <param name="castTarg">castTarg</param>
        /// <param name="destTarg">destTarg</param>
        /// <param name="surpriseAttack">surpriseAttack</param>
        /// <param name="canHitNonTargetPawns">canHitNonTargetPawns</param>
        /// <param name="preventFriendlyFire">preventFriendlyFire</param>
        /// <returns></returns>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false)
        {
            // check reloading before starting
            if (AmmoUser != null)
            {
                if (!AmmoUser.HasMagazine)
                {
                    AmmoUser.TryStartReload();
                    return false;
                }
            }
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire);
        }

        /// <summary>
        /// Try send the projectiles towords the target.
        /// </summary>
        /// <returns>Wether launching the projectiles was successful</returns>
        public override bool TryCastShot()
        {
            if (AmmoUser != null)
            {
                // try reduce the magazine projectile count
                if (!AmmoUser.TryReduceAmmoCount(VerbPropsCE.ammoConsumedPerShotCount))                
                    return false;                
            }
            if (base.TryCastShot())
            {
                if (ShooterPawn != null)                
                    ShooterPawn.records.Increment(RecordDefOf.ShotsFired);
                // try start reloading if this attachment uses ammo
                if (AmmoUser != null)
                {
                    if (!AmmoUser.HasMagazine && AmmoUser.HasAmmo)
                        AmmoUser.TryStartReload();
                    else if (!AmmoUser.HasMagazine)
                        AmmoUser.Notify_OutOfAmmo();
                }
                // draw empty casing
                if (VerbPropsCE.ejectsCasings && projectilePropsCE.dropsCasings)                
                    CE_Utility.ThrowEmptyCasing(caster.DrawPos, caster.Map, DefDatabase<FleckDef>.GetNamed(projectilePropsCE.casingMoteDefname));                
                return true;
            }
            return false;
        }
    }
}
