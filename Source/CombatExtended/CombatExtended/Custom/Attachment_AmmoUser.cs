using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class Attachment_AmmoUser : IExposable, IReloadable
    {
        private int curMagCountInt = 0;
        private AmmoDef currentAmmoInt = null;
        private AmmoDef selectedAmmo;
        
        /// <summary>
        /// The source attachment
        /// </summary>
        public AttachmentDef sourceAttachment;
        
        /// <summary>
        /// The parent VerbManager.
        /// </summary>
        public WeaponPlatform_VerbManager verbManager;
              
        /// <summary>
        /// The currently selected projectile. Will return the default projectile if the inner value is null.
        /// </summary>
        public ThingDef SelectedProjectile
        {
            get
            {             
                if (currentAmmoInt == null)
                    return VerbProps.defaultProjectile;                
                return AmmoProps.ammoSet?.ammoTypes?.First(a => a.ammo == currentAmmoInt)?.projectile ?? VerbProps.defaultProjectile;
            }
        }
        /// <summary>
        /// The properties of the verb linked this ammoUser.
        /// </summary>
        public VerbPropertiesCE VerbProps
        {
            get
            {                
                return sourceAttachment.attachmentVerb?.verb ?? null;
            }
        }
        /// <summary>
        /// The ammo properties used for this ammo user.
        /// </summary>
        public AttachmentVerb.AttachmentVerb_AmmoUserProperties AmmoProps
        {
            get
            {
                return sourceAttachment.attachmentVerb?.ammo ?? null;
            }
        }
        /// <summary>
        /// Returns the current Pawn using this weapon
        /// </summary>
        public Pawn Holder
        {
            get
            {
                return verbManager.weapon.Wielder;
            }
        }
        public bool IsEquippedGun => Holder != null;
        private Pawn _pawn;
        private CompInventory _compInventory;
        /// <summary>
        /// Returns the compInventory of the pawn currently holding this weapon.
        /// </summary>
        public CompInventory CompInventory
        {
            get
            {
                if (_pawn == Holder && _compInventory != null)
                    return _compInventory;
                _pawn = Holder;
                _compInventory = _pawn.TryGetComp<CompInventory>();
                return _compInventory;
            }
        }
        /// <summary>
        /// Returns the current number of projectiles in the magazine.
        /// </summary>
        public int CurMagCount
        {
            get
            {
                return curMagCountInt;
            }
            set
            {
                curMagCountInt = value;
                if (value < 0) curMagCountInt = 0;
                if (value > MagSize) curMagCountInt = MagSize;
            }
        }
        /// <summary>
        /// Returns the currently selected ammo.
        /// </summary>
        public AmmoDef SelectedAmmo
        {
            get
            {
                return selectedAmmo;
            }
            set
            {
                selectedAmmo = value;
                if (MagazineEmpty && currentAmmoInt != value)                
                    currentAmmoInt = value;                
            }
        }
        /// <summary>
        /// Returns the current ammo in the magazine.
        /// </summary>
        public AmmoDef CurrentAmmo
        {
            get
            {
                return currentAmmoInt;
            }
        }        
        /// <summary>
        /// Return the magazine size
        /// </summary>
        public int MagSize
        {
            get
            {
                return AmmoProps.magazineSize;
            }
        }
        /// <summary>
        /// Return wether magazine is empty.
        /// </summary>
        public bool MagazineFull
        {
            get
            {
                return CurMagCount == AmmoProps.magazineSize;
            }
        }
        /// <summary>
        /// Return wether the current magazine is empty.
        /// </summary>
        public bool MagazineEmpty
        {
            get
            {
                return CurMagCount == 0;
            }
        }
        /// <summary>
        /// Return wether ammo is in the inventory of the parent pawn.
        /// </summary>
        public bool HasAmmo
        {
            get
            {               
                return CompInventory?.container.Any(t => AmmoProps.ammoSet.ammoTypes.Any(a => a.ammo == t.def)) ?? false;
            }
        }        
        /// <summary>
        /// Return wether this is capable of firing now
        /// </summary>
        public bool CanBeFiredNow
        {
            get
            {
                return !MagazineEmpty;
            }
        }
        /// <summary>
        /// return the ammoSet used by this reloadable.
        /// </summary>
        public AmmoSetDef AmmoSet
        {
            get
            {
                return AmmoProps.ammoSet;
            }
        }            
        /// <summary>
        /// Return available ammo
        /// </summary>
        public IEnumerable<ThingDefCount> AvailableAmmoDefs
        {
            get
            {
                if (CompInventory == null)
                {
                    foreach (ThingDefCount item in AmmoSet.ammoTypes.Select(l => new ThingDefCount(l.ammo, 0)))
                        yield return item;
                }
                else
                {
                    foreach (AmmoLink link in AmmoSet.ammoTypes)
                    {
                        AmmoDef ammo = link.ammo;

                        int count = CompInventory.container.Where(at => at.def == ammo).Sum(at => at.stackCount);
                        if (count > 0)
                            yield return new ThingDefCount(ammo, count);
                    }
                }
            }
        }

        public Attachment_AmmoUser()
        {
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref this.sourceAttachment, "sourceAttachment");
            Scribe_Defs.Look(ref this.selectedAmmo, "selectedAmmoA");
            Scribe_Defs.Look(ref this.currentAmmoInt, "currentAmmoIntA");            
            Scribe_Values.Look(ref this.curMagCountInt, "curMagCountInt");            
        }

        /// <summary>
        /// <para>Reduces ammo count and updates inventory if necessary, call this whenever ammo is consumed by the gun (e.g. firing a shot, clearing a jam). </para>
        /// <para>Has an optional argument for the amount of ammo to consume per shot, which defaults to 1; this caters for special cases such as different sci-fi weapons using up different amounts of the same energy cell ammo type per shot, or a double-barrelled shotgun that fires both cartridges at the same time (projectile treated as a single, more powerful bullet)</para>
        /// </summary>
        public bool TryReduceAmmoCount(int ammoConsumedPerShot = 1)
        {
            ammoConsumedPerShot = (ammoConsumedPerShot > 0) ? ammoConsumedPerShot : 1;
            
            // If magazine is empty, return false
            if (MagazineEmpty)
            {                
                if (!HasAmmo) Notify_OutOfAmmo();
                else TryStartReload();

                return false;
            }
            // Reduce ammo count and update inventory
            CurMagCount = (CurMagCount - ammoConsumedPerShot < 0) ? 0 : CurMagCount - ammoConsumedPerShot;
            if (MagazineEmpty)
            {                
                if (!HasAmmo) Notify_OutOfAmmo();                                    
                else TryStartReload();                
            }
            return true;
        }

        /// <summary>
        /// Try to unload the weapon and either carry the ammo or drop it.
        /// </summary>
        /// <param name="dropUnloadedAmmo">Wether to drop the ammo on the ground after unloading.</param>
        /// <returns>Wether weapon was unloaded or not.</returns>
        public bool TryUnload(bool forceUnload = false)
        {
            if (CurrentAmmo == null || curMagCountInt == 0)
            {
                if (!MagazineEmpty) Log.Warning($"CE: attachment_AmmoUser failed to unload weapon with currentAmmo = null {sourceAttachment} {Holder}");
                if (forceUnload) curMagCountInt = 0;                
                return curMagCountInt == 0;
            }                       
            int dropCount, carryCount;

            float ammoWeight = currentAmmoInt.GetStatValueAbstract(StatDefOf.Mass);
            float ammoBulk = currentAmmoInt.GetStatValueAbstract(CE_StatDefOf.Bulk);

            float availableWeight = CompInventory.GetAvailableWeight(true);
            float availableBulk = CompInventory.GetAvailableBulk(false);

            carryCount = (int) Mathf.Min(availableWeight / ammoWeight, availableBulk / ammoBulk, curMagCountInt);
            dropCount = curMagCountInt - carryCount;           
          
            if (carryCount != 0)
            {
                Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                ammoThing.stackCount = carryCount;
                if (CompInventory.container.TryAddOrTransfer(ammoThing, canMergeWithExistingStacks: true))
                    CurMagCount -= carryCount;
            }
            if (dropCount != 0)
            {
                Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
                ammoThing.stackCount = dropCount;
                if (GenThing.TryDropAndSetForbidden(ammoThing, Holder.Position, Holder.Map, ThingPlaceMode.Near, out _, false))
                    CurMagCount -= dropCount;
            }
            if (forceUnload) curMagCountInt = 0;
            return curMagCountInt == 0;
        }

        /// <summary>
        /// Try start reload job.
        /// </summary>
        /// <returns>Wether a reload job was started</returns>
        public bool TryStartReload()
        {                        
            if ((Holder?.jobs?.curJob?.def == CE_JobDefOf.ReloadWeaponAttachment)
                || (Holder?.jobs?.curDriver is JobDriver_ReloadAttachment)
                || (Holder?.jobs?.jobQueue?.jobs.Any(j => j.job?.def == CE_JobDefOf.ReloadWeaponAttachment) ?? false))            
                return false;            
            Job job = TryGetReloadingJob();
            if(job == null)
            {
                Notify_OutOfAmmo();
                return false;
            }            
            Holder.jobs.StartJob(job, JobCondition.InterruptForced, null, Holder.jobs?.curJob?.def != CE_JobDefOf.ReloadWeaponAttachment);
            return true;
        }

        public Job TryGetReloadingJob()
        {
            if ((Holder?.jobs?.curJob?.def == CE_JobDefOf.ReloadWeaponAttachment)
                || (Holder?.jobs?.curDriver is JobDriver_ReloadAttachment)
                || (Holder?.jobs?.jobQueue?.jobs.Any(j => j.job?.def == CE_JobDefOf.ReloadWeaponAttachment) ?? false))
                return null;
            if (Holder != null && TryFindAmmoInInventory(out Thing ammoThing))
            {
                Job job = JobMaker.MakeJob(CE_JobDefOf.ReloadWeaponAttachment, verbManager.weapon, ammoThing);
                job.count = 1;
                job.playerForced = true;                
                return job;
            }
            return null;
        }

        /// <summary>
        /// Out of Ammo action.
        /// </summary>
        public void Notify_OutOfAmmo()
        {
            if(verbManager.SelectedVerb?.verbProps == this.VerbProps)
                verbManager.SelectedVerb = null;                        
        }

        /// <summary>
        /// Reload using the ammothing and return wether the ammothing can be used for further reloading
        /// </summary>
        /// <param name="ammoThing">Ammo thing</param>
        /// <returns>wether to continue reloading</returns>
        public bool TryReloadAndResume(Thing ammoThing)
        {
            if (ammoThing?.Destroyed ?? true)
                return false;
            bool success = false;

            // if the current magazine is not empty and has a different ammo type, unload the current mag.
            if(ammoThing.def != currentAmmoInt && !MagazineEmpty)            
                TryUnload();

            // we check reloadOneAtATime if we are reloading one projectile at a time
            currentAmmoInt = ammoThing.def as AmmoDef;            
            selectedAmmo = currentAmmoInt;            
            int num = AmmoProps.reloadOneAtATime ? 1 : Math.Min(ammoThing.stackCount, AmmoProps.magazineSize - curMagCountInt);
            curMagCountInt += num;
            ammoThing.stackCount -= num;

            // delete ammo if successfull and no ammo left
            if (ammoThing.stackCount <= 0)
                ammoThing.Destroy();

            CompInventory?.UpdateInventory();
            return !MagazineFull && AmmoProps.reloadOneAtATime && success && !ammoThing.Destroyed && ammoThing.stackCount > 0;
        }        

        /// <summary>
        /// Returns ammo thing from the magazine.
        /// </summary>
        /// <param name="ammoThing"></param>
        /// <returns>Wether an ammo thing was found</returns>
        public bool TryFindAmmoInInventory(out Thing ammoThing)
        {            
            ammoThing = null;
            if (CompInventory == null)            
                return false;
            // Try finding suitable ammoThing for currently set ammo first
            CompInventory.UpdateInventory();
            ammoThing = CompInventory.ammoList.Find(thing => thing.def == selectedAmmo);
            if (ammoThing != null)            
                return true;
            //Current mag already has a few rounds in, and the inventory doesn't have any more of that type.
            //If we let this method pick a new selectedAmmo below, it would convert the already loaded rounds to a different type,
            //so for OneAtATime weapons, we stop the process here here.
            if (AmmoProps.reloadOneAtATime && CurMagCount > 0)            
                return false;            
            // Try finding ammo from different type
            foreach (AmmoLink link in AmmoProps.ammoSet.ammoTypes)
            {
                ammoThing = CompInventory.ammoList.Find(thing => thing.def == link.ammo);
                if (ammoThing != null)
                {
                    selectedAmmo = (AmmoDef)link.ammo;
                    return true;
                }
            }
            return false;
        }
    }
}
