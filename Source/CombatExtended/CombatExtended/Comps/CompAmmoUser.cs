using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CombatExtended
{
    public class CompAmmoUser : CompRangedGizmoGiver
    {
        #region Fields

        private int curMagCountInt = 0;
        private AmmoDef currentAmmoInt = null;
        private AmmoDef selectedAmmo;
        
        private Thing ammoToBeDeleted;

        public Building_TurretGunCE turret;         // Cross-linked from CE turret

        #endregion

        #region Properties

        public CompProperties_AmmoUser Props
        {
            get
            {
                return (CompProperties_AmmoUser)props;
            }
        }

        public int CurMagCount
        {
            get
            {
                return curMagCountInt;
            }
        }
        public CompEquippable CompEquippable
        {
            get { return parent.GetComp<CompEquippable>(); }
        }
        public Pawn Wielder
        {
            get
            {
                if (CompEquippable == null || CompEquippable.PrimaryVerb == null || CompEquippable.PrimaryVerb.caster == null)
                {
                    return null;
                }
                return CompEquippable.PrimaryVerb.CasterPawn;
            }
        }
		public Pawn Holder
		{
			get
			{
				return Wielder ?? (CompEquippable.parent.ParentHolder as Pawn_InventoryTracker)?.pawn;
			}
		}
        public bool UseAmmo
        {
            get
            {
                return Controller.settings.EnableAmmoSystem && Props.ammoSet != null;
            }
        }
        public bool HasAndUsesAmmoOrMagazine
        {
        	get
        	{
        		return !UseAmmo || HasAmmoOrMagazine;
        	}
        }
        public bool HasAmmoOrMagazine
        {
        	get
        	{
        		return (HasMagazine && CurMagCount > 0) || HasAmmo;
        	}
        }
        public bool CanBeFiredNow
        {
        	get
        	{
        		return !UseAmmo || ((HasMagazine && CurMagCount > 0) || (!HasMagazine && HasAmmo));
        	}
        }
        public bool HasAmmo
        {
            get
            {
                return CompInventory != null && CompInventory.ammoList.Any(x => Props.ammoSet.ammoTypes.Any(a => a.ammo == x.def));
            }
        }
        public bool HasMagazine { get { return Props.magazineSize > 0; } }
        public AmmoDef CurrentAmmo
        {
            get
            {
                return UseAmmo ? currentAmmoInt : null;
            }
        }
        public ThingDef CurAmmoProjectile => Props.ammoSet?.ammoTypes?.FirstOrDefault(x => x.ammo == CurrentAmmo).projectile;
        public CompInventory CompInventory
        {
            get
            {
                return Holder.TryGetComp<CompInventory>();
            }
        }
        private IntVec3 Position
        {
            get
            {
                if (Wielder != null) return Wielder.Position;
                else if (turret != null) return turret.Position;
                else if (Holder != null) return Holder.Position;
                else return parent.Position;
            }
        }
        private Map Map
        {
            get
            {
                if (Holder != null) return Holder.MapHeld;
                else if (turret != null) return turret.MapHeld;
                else return parent.MapHeld;
            }
        }
        public bool ShouldThrowMote => Props.throwMote && Props.magazineSize > 1;

        public AmmoDef SelectedAmmo
        {
            get
            {
                return selectedAmmo;
            }
            set
            {
                selectedAmmo = value;
                if (!HasMagazine && CurrentAmmo != value)
                {
                    currentAmmoInt = value;
                }
            }
        }

        #endregion Properties

        #region Methods

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);

            //curMagCountInt = Props.spawnUnloaded && UseAmmo ? 0 : Props.magazineSize;

            // Initialize ammo with default if none is set
            if (UseAmmo)
            {
                if (Props.ammoSet.ammoTypes.NullOrEmpty())
                {
                    Log.Error(parent.Label + " has no available ammo types");
                }
                else
                {
                    if (currentAmmoInt == null)
                        currentAmmoInt = (AmmoDef)Props.ammoSet.ammoTypes[0].ammo;
                    if (selectedAmmo == null)
                        selectedAmmo = currentAmmoInt;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curMagCountInt, "count", 0);
            Scribe_Defs.Look(ref currentAmmoInt, "currentAmmo");
            Scribe_Defs.Look(ref selectedAmmo, "selectedAmmo");
        }

        private void AssignJobToWielder(Job job)
        {
            if (Wielder.drafter != null)
            {
                Wielder.jobs.TryTakeOrderedJob(job);
            }
            else
            {
                ExternalPawnDrafter.TakeOrderedJob(Wielder, job);
            }
        }

        public bool Notify_ShotFired()
        {
        	if (ammoToBeDeleted != null)
        	{
        		ammoToBeDeleted.Destroy();
        		ammoToBeDeleted = null;
                CompInventory.UpdateInventory();
	            if (!HasAmmoOrMagazine)
	            {
	            	return false;
	            }
        	}
            return true;
        }
        
        public bool Notify_PostShotFired()
        {
            if (!HasAmmoOrMagazine)
            {
                DoOutOfAmmoAction();
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Reduces ammo count and updates inventory if necessary, call this whenever ammo is consumed by the gun (e.g. firing a shot, clearing a jam)
        /// </summary>
        public bool TryReduceAmmoCount()
        {
            if (Wielder == null && turret == null)
            {
                Log.Error(parent.ToString() + " tried reducing its ammo count without a wielder");
            }

            // Mag-less weapons feed directly from inventory
            if (!HasMagazine)
            {
                if (UseAmmo)
                {
                    if (!TryFindAmmoInInventory(out ammoToBeDeleted))
                    {
                        return false;
                    }
                    if (ammoToBeDeleted.def != CurrentAmmo)
                    {
                        currentAmmoInt = ammoToBeDeleted.def as AmmoDef;
                    }
					
                    if (ammoToBeDeleted.stackCount > 1)
                        ammoToBeDeleted = ammoToBeDeleted.SplitOff(1);
                }
                return true;
            }
            // If magazine is empty, return false
            if (curMagCountInt <= 0)
            {
                curMagCountInt = 0;
                return false;
            }
            // Reduce ammo count and update inventory
            curMagCountInt--;
            if (CompInventory != null)
            {
                CompInventory.UpdateInventory();
            }
            if (curMagCountInt < 0) TryStartReload();
            return true;
        }
		
        // really only used by pawns (JobDriver_Reload) at this point... TODO: Finish making sure this is only used by pawns and fix up the error checking.
        /// <summary>
        /// Overrides a Pawn's current activities to start reloading a gun or turret.  Has a code path to resume the interrupted job.
        /// </summary>
        public void TryStartReload()
        {
            if (!HasMagazine)
            {
                return;
            }
            if (Wielder == null && turret == null)
            	return;

            // secondary branch for if we ended up being called up by a turret somehow...
            if (turret != null)
            {
                turret.TryOrderReload();
                return;
            }

            if (UseAmmo)
            {
            	TryUnload();
            	
                // Check for ammo
                if (Wielder != null && !HasAmmo)
                {
                    DoOutOfAmmoAction();
                    return;
                }
            }

            // Issue reload job
            if (Wielder != null)
            {
            	Job reloadJob = TryMakeReloadJob();
            	if (reloadJob == null)
            		return;
            	reloadJob.playerForced = true;
                Wielder.jobs.StartJob(reloadJob, JobCondition.InterruptForced, null, Wielder.CurJob?.def != reloadJob.def, true);
            }
        }

        // used by both turrets (JobDriver_ReloadTurret) and pawns (JobDriver_Reload).
        /// <summary>
        /// Used to unload the weapon.  Ammo will be dumped to the unloading Pawn's inventory or the ground if insufficient space.  Any ammo that can't be dropped
        /// on the ground is destroyed (with a warning).
        /// </summary>
        /// <returns>bool, true indicates the weapon was already in an unloaded state or the unload was successful.  False indicates an error state.</returns>
        /// <remarks>
        /// Failure to unload occurs if the weapon doesn't use a magazine.
        /// </remarks>
        public bool TryUnload()
        {
            Thing outThing;
            return TryUnload(out outThing);
        }

        public bool TryUnload(out Thing droppedAmmo)
        {
            droppedAmmo = null;
        	if (!HasMagazine || (Holder == null && turret == null))
        		return false; // nothing to do as we are in a bad state;
        	
        	if (!UseAmmo || curMagCountInt == 0)
        		return true; // nothing to do but we aren't in a bad state either.  Claim success.
        		
            // Add remaining ammo back to inventory
            Thing ammoThing = ThingMaker.MakeThing(currentAmmoInt);
            ammoThing.stackCount = curMagCountInt;
            bool doDrop = false;

            if (CompInventory != null)
            	doDrop = (curMagCountInt != CompInventory.container.TryAdd(ammoThing, ammoThing.stackCount)); // TryAdd should report how many ammoThing.stackCount it stored.
            else
            	doDrop = true; // Inventory was null so no place to shift the ammo besides the ground.
            
            if (doDrop)
            {
            	// NOTE: If we get here from ThingContainer.TryAdd() it will have modified the ammoThing.stackCount to what it couldn't take.
                //Thing outThing;
                if (!GenThing.TryDropAndSetForbidden(ammoThing, Position, Map, ThingPlaceMode.Near, out droppedAmmo, turret.Faction != Faction.OfPlayer))
                {
                	Log.Warning(String.Concat(this.GetType().Assembly.GetName().Name + " :: " + this.GetType().Name + " :: ",
                	                         "Unable to drop ", ammoThing.LabelCap, " on the ground, thing was destroyed."));
                }
            }
            
            // don't forget to set the clip to empty...
            curMagCountInt = 0;
            
            return true;
        }
        
        /// <summary>
        /// Used to fetch a reload job for the weapon this comp is on.  Sets storedInfo to null (as if no job being replaced).
        /// </summary>
        /// <returns>Job using JobDriver_Reload</returns>
        /// <remarks>TryUnload() should be called before this in most cases.</remarks>
        public Job TryMakeReloadJob()
        {
        	if (!HasMagazine || (Holder == null && turret == null))
        		return null; // the job couldn't be created.
            
            return new Job(CE_JobDefOf.ReloadWeapon, Holder, parent);
        }
        
        private void DoOutOfAmmoAction()
        {
            if (ShouldThrowMote)
            {
                MoteMaker.ThrowText(Position.ToVector3Shifted(), Find.CurrentMap, "CE_OutOfAmmo".Translate() + "!");
            }
            if (Wielder != null && CompInventory != null && (Wielder.CurJob == null || Wielder.CurJob.def != JobDefOf.Hunt)) CompInventory.SwitchToNextViableWeapon();
        }

        public void LoadAmmo(Thing ammo = null)
        {
            if (Holder == null && turret == null)
			{
                Log.Error(parent.ToString() + " tried loading ammo with no owner");
                return;
            }

            int newMagCount;
            if (UseAmmo)
            {
                Thing ammoThing;
                bool ammoFromInventory = false;
                if (ammo == null)
                {
                    if (!TryFindAmmoInInventory(out ammoThing))
                    {
                        DoOutOfAmmoAction();
                        return;
                    }
                    ammoFromInventory = true;
                }
                else
                {
                    ammoThing = ammo;
                }
                currentAmmoInt = (AmmoDef)ammoThing.def;

                // If there's more ammo in inventory than the weapon can hold, or if there's greater than 1 bullet in inventory if reloading one at a time
                if ((Props.reloadOneAtATime ? 1 : Props.magazineSize) < ammoThing.stackCount)
                {
                    if (Props.reloadOneAtATime)
                    {
                        newMagCount = curMagCountInt + 1;
                        ammoThing.stackCount--;
                    }
                    else
                    {
                        newMagCount = Props.magazineSize;
                        ammoThing.stackCount -= Props.magazineSize;
                    }
                    if (CompInventory != null) CompInventory.UpdateInventory();
                }

                // If there's less ammo in inventory than the weapon can hold, or if there's only one bullet left if reloading one at a time
                else
                {
                    newMagCount = (Props.reloadOneAtATime) ? curMagCountInt + 1 : ammoThing.stackCount;
                    if (ammoFromInventory)
                    {
                        CompInventory.container.Remove(ammoThing);
                    }
                    else if (!ammoThing.Destroyed)
                    {
                        ammoThing.Destroy();
                    }
                }
            }
            else
            {
                newMagCount = (Props.reloadOneAtATime) ? (curMagCountInt + 1) : Props.magazineSize;
            }
            curMagCountInt = newMagCount;
            if (turret != null) turret.isReloading = false;
            if (parent.def.soundInteract != null) parent.def.soundInteract.PlayOneShot(new TargetInfo(Position,  Find.CurrentMap, false));
        }

        /// <summary>
        /// Resets current ammo count to a full magazine. Intended use is pawn/turret generation where we want raiders/enemy turrets to spawn with loaded magazines. DO NOT
        /// use for regular reloads, those should be handled through LoadAmmo() instead.
        /// </summary>
        /// <param name="newAmmo">Currently loaded ammo type will be set to this, null will load currently selected type.</param>
        public void ResetAmmoCount(AmmoDef newAmmo = null)
        {
            if (newAmmo != null)
            {
                currentAmmoInt = newAmmo;
            }
            curMagCountInt = Props.magazineSize;
        }

        public bool TryFindAmmoInInventory(out Thing ammoThing)
        {
            ammoThing = null;
            if (CompInventory == null)
            {
                return false;
            }

            // Try finding suitable ammoThing for currently set ammo first
            ammoThing = CompInventory.ammoList.Find(thing => thing.def == selectedAmmo);
            if (ammoThing != null)
            {
                return true;
            }

            // Try finding ammo from different type
            foreach (AmmoLink link in Props.ammoSet.ammoTypes)
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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            GizmoAmmoStatus ammoStatusGizmo = new GizmoAmmoStatus { compAmmo = this };
            yield return ammoStatusGizmo;

            if ((Wielder != null && Wielder.Faction == Faction.OfPlayer) || (turret != null && turret.Faction == Faction.OfPlayer && (turret.MannableComp != null || UseAmmo)))
            {
                Action action = null;
                if (Wielder != null) action = delegate { TryStartReload(); };
                else if (turret != null && turret.MannableComp != null) action = turret.TryOrderReload;

                // Check for teaching opportunities
                string tag;
                if(turret == null)
                {
                    if (HasMagazine) tag = "CE_Reload"; // Teach reloading weapons with magazines
                    else tag = "CE_ReloadNoMag";    // Teach about mag-less weapons
                }
                else
                {
                    if (turret.MannableComp == null) tag = "CE_ReloadAuto";  // Teach about auto-turrets
                    else tag = "CE_ReloadManned";    // Teach about reloading manned turrets
                }
                LessonAutoActivator.TeachOpportunity(ConceptDef.Named(tag), turret, OpportunityType.GoodToKnow);

                Command_Reload reloadCommandGizmo = new Command_Reload
                {
                    compAmmo = this,
                    action = action,
                    defaultLabel = HasMagazine ? "CE_ReloadLabel".Translate() : "",
                    defaultDesc = "CE_ReloadDesc".Translate(),
                    icon = CurrentAmmo == null ? ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true) : Def_Extensions.IconTexture(selectedAmmo),
                    tutorTag = tag
                };
                yield return reloadCommandGizmo;
            }
        }

		public override string TransformLabel(string label)
		{
            string ammoSet = UseAmmo && Controller.settings.ShowCaliberOnGuns ? " (" + Props.ammoSet.LabelCap + ") " : "";
            return  label + ammoSet;
		}

        /*
        public override string GetDescriptionPart()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("CE_MagazineSize".Translate() + ": " + GenText.ToStringByStyle(Props.magazineSize, ToStringStyle.Integer));
            stringBuilder.AppendLine("CE_ReloadTime".Translate() + ": " + GenText.ToStringByStyle((Props.reloadTime), ToStringStyle.FloatTwo) + " s");
            if (UseAmmo)
            {
                // Append various ammo stats
                stringBuilder.AppendLine("CE_AmmoSet".Translate() + ": " + Props.ammoSet.LabelCap + "\n");
                foreach(var cur in Props.ammoSet.ammoTypes)
                {
                    string label = string.IsNullOrEmpty(cur.ammo.ammoClass.LabelCapShort) ? cur.ammo.ammoClass.LabelCap : cur.ammo.ammoClass.LabelCapShort;
                    stringBuilder.AppendLine(label + ":\n" + cur.projectile.GetProjectileReadout());
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        */

        #endregion Methods
    }
}
