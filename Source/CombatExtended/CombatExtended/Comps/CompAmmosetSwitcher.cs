using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Utilities;

namespace CombatExtended
{
    public class CompUnderBarrel : CompRangedGizmoGiver
    {
        public CompProperties_UnderBarrel Props => (CompProperties_UnderBarrel)this.props;

        public CompEquippable CompEq() => this.parent.TryGetComp<CompEquippable>();
        
         public CompFireModes _compEq;
        
        public CompFireModes compEq
        {
            get
            {
                if(_compEq == null)
                {
                    _compEq = CompEq();
                }
                
                return _compEq;
            }
        }

        public CompAmmoUser CompAmmo() => this.parent.TryGetComp<CompAmmoUser>();
        
        public CompFireModes _compAmmo;
        
        public CompFireModes compAmmo
        {
            get
            {
                if(_compAmmo == null)
                {
                    _compAmmo = CompAmmo();
                }
                
                return _compAmmo;
            }
        }


        public CompFireModes CompFireModes() => this.parent.TryGetComp<CompFireModes>();
        
        public CompFireModes _compFireModes;
        
        public CompFireModes compFireModes
        {
            get
            {
                if(_compFireModes == null)
                {
                    _compFireModes = CompFireModes();
                }
                
                return  _compFireModes;
            }
        }


        public CompProperties_FireModes CompPropsFireModes() => this.parent.def.comps.Find(x => x is CompProperties_FireModes) as CompProperties_FireModes;
        
        public CompProperties_FireModes _compPropsFireModes;
        
        public CompProperties_FireModes compPropsFireModes
        {
            get
            {
                if(_compPropsFireModes == null)
                {
                    _compPropsFireModes = CompPropsFireModes();
                }
                
                return  _compPropsFireModes;
            }
        }

        public VerbProperties DefVerbProps() => this.parent.def.Verbs.Find(x => x is VerbPropertiesCE);
        
        public VerbProperties _defVerbProps;
        
        public VerbProperties defVerbProps
        {
            get
            {
                if(_defVerbProps == null)
                {
                    _defVerbProps = DefVerbProps()
                }
                
                return _defVerbProps;
            }
        }

        public CompProperties_AmmoUser CompPropsAmmo() => (CompProperties_AmmoUser)this.parent.def.comps.Find(x => x is CompProperties_AmmoUser);
        
        public CompProperties_AmmoUser _compPropsAmmo;
        
        public CompProperties_AmmoUser compPropsAmmo
        {
            get
            {
                if(_compPropsAmmo == null)
                {
                    _compPropsAmmo = CompPropsAmmo()
                }
                
                return _compPropsAmmo;
            }
        }

        public AmmoDef mainGunLoadedAmmo;

        public int mainGunMagCount;

        public AmmoDef UnderBarrelLoadedAmmo;

        public int UnderBarrelMagCount;

        public bool usingUnderBarrel;

        public void SwitchToUB()
        {
            mainGunLoadedAmmo = compAmmo.CurrentAmmo;
            mainGunMagCount = compAmmo.CurMagCount;

            compAmmo.props = this.Props.propsUnderBarrel;
            compEq.PrimaryVerb.verbProps = Props.verbPropsUnderBarrel;
            compFireModes.props = this.Props.propsFireModesUnderBarrel;
            compAmmo.CurMagCount = UnderBarrelMagCount;
            compAmmo.CurrentAmmo = UnderBarrelLoadedAmmo;
            compAmmo.SelectedAmmo = compAmmo.CurrentAmmo;
            if (compAmmo.Wielder != null)
            {
                compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();

                if (compAmmo.Wielder.jobs?.curJob?.def == CE_JobDefOf.ReloadWeapon)
                {
                    compAmmo.Wielder.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
            }
            compEq.PrimaryVerb.verbProps.burstShotCount = this.Props.propsFireModesUnderBarrel.aimedBurstShotCount;
            usingUnderBarrel = true;
        }

        public void SwithToB()
        {
            UnderBarrelLoadedAmmo = compAmmo.CurrentAmmo;
            UnderBarrelMagCount = compAmmo.CurMagCount;

            compAmmo.props = compPropsAmmo;
            compEq.PrimaryVerb.verbProps = defVerbProps.MemberwiseClone();
            compFireModes.props = compPropsFireModes;
            compAmmo.CurMagCount = mainGunMagCount;
            compAmmo.CurrentAmmo = mainGunLoadedAmmo;
            compAmmo.SelectedAmmo = compAmmo.CurrentAmmo;
            if (compAmmo.Wielder != null)
            {
                compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();

                if (compAmmo.Wielder.jobs?.curJob?.def == CE_JobDefOf.ReloadWeapon)
                {
                    compAmmo.Wielder.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
            }
            compEq.PrimaryVerb.verbProps.burstShotCount = defVerbProps.burstShotCount;
            usingUnderBarrel = false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (compAmmo.Props.ammoSet == compPropsAmmo.ammoSet)
            {
                yield return new Command_Action
                {

                    defaultLabel = "CE_SwitchAmmmoSetToUnderBarrel".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload"),
                    defaultDesc = "CE_UBGLStats".Translate() +
                    "\n " + "WarmupTime".Translate() + ": " + Props.verbPropsUnderBarrel.warmupTime
                    + "\n " + "Range".Translate() + ": " + Props.verbPropsUnderBarrel.range
                    + "\n " + "CE_AmmoSet".Translate() + ": " + Props.propsUnderBarrel.ammoSet.label
                    + "\n " + "CE_MagazineSize".Translate() + ": " + Props.propsUnderBarrel.magazineSize
                    ,
                    action = delegate
                    {
                        SwitchToUB();
                    }
                };
            }
            else
            {
                yield return new Command_Action
                {

                    defaultLabel = "CE_SwitchAmmmoSetToNormalRifle".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload"),
                    action = delegate
                    {
                        SwithToB();
                    }
                };
            }
        }

        public override void Initialize(CompProperties props)
        {
            if (this.parent.def.weaponTags.NullOrEmpty())
            {
                this.parent.def.weaponTags = new List<string>() { "NoSwitch" };
            }
            else if (!this.parent.def.weaponTags.Contains("NoSwitch"))
            {
                this.parent.def.weaponTags.Add("NoSwitch");
            }
            base.Initialize(props);
        }

        public override void PostExposeData()
        {

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (usingUnderBarrel)
                {
                    UnderBarrelMagCount = compAmmo.CurMagCount;
                    UnderBarrelLoadedAmmo = compAmmo.CurrentAmmo;
                }
            }
            Scribe_Values.Look(ref usingUnderBarrel, "usingUnderBarrel");
            Scribe_Defs.Look(ref mainGunLoadedAmmo, "mainGunAmmo");
            Scribe_Defs.Look(ref UnderBarrelLoadedAmmo, "UnderBarrelAmmo");
            Scribe_Values.Look(ref mainGunMagCount, "magCountMainGun");
            Scribe_Values.Look(ref UnderBarrelMagCount, "UnderBarrelMagCount");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (usingUnderBarrel)
                {
                    compAmmo.CurMagCount = UnderBarrelMagCount;
                    compAmmo.CurrentAmmo = UnderBarrelLoadedAmmo;

                    compAmmo.props = this.Props.propsUnderBarrel;
                    compEq.PrimaryVerb.verbProps = Props.verbPropsUnderBarrel;
                    compFireModes.props = this.Props.propsFireModesUnderBarrel;

                    if (compAmmo.Wielder != null)
                    {
                        compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();
                    }
                }
            }
            base.PostExposeData();

        }
    }

    public class CompProperties_UnderBarrel : CompProperties
    {

        public CompProperties_AmmoUser propsUnderBarrel;

        public VerbPropertiesCE verbPropsUnderBarrel;

        public CompProperties_FireModes propsFireModesUnderBarrel;

        public List<string> targetTags;

        public bool targetHighVal;

        public CompProperties_UnderBarrel()
        {
            this.compClass = typeof(CompUnderBarrel);
        }
    }

    public class VerbShootCE_AdvancedUBGL : Verb_ShootCE
    {
        public CompUnderBarrel UBGLComp => EquipmentSource.TryGetComp<CompUnderBarrel>();
        public override int ShotsPerBurst
        {
            get
            {
                if (this.CompFireModes.CurrentFireMode == FireMode.AutoFire)
                {
                    if (UBGLComp?.usingUnderBarrel ?? false)
                    {
                        return UBGLComp.Props.verbPropsUnderBarrel.burstShotCount;
                    }
                }
                return base.ShotsPerBurst;
            }
        }

        public bool nonPlayer => Caster.Faction != Faction.OfPlayer;

        public Pawn launcherBoy;

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false)
        {
            if (UBGLComp.Props.targetHighVal)
            {
                if (nonPlayer)
                {
                    if (castTarg.Thing is Pawn p && (p != launcherBoy))
                    {
                        var pawns = p.Position.PawnsInRange(p.Map, 8).ToList();

                        var launchyBoy = pawns.Find(x => x.equipment?.Primary?.def.weaponTags?.Any(y => UBGLComp.Props.targetTags.Contains(y)) ?? false);

                        if (launchyBoy != null && this.CanHitTargetFrom(Caster.Position, launchyBoy))
                        {
                            UBGLComp.SwitchToUB();

                            launcherBoy = launchyBoy;

                            var infoLaunchyBoy = new LocalTargetInfo(launchyBoy);

                            this.OrderForceTarget(infoLaunchyBoy);

                            return false;
                        }
                        else
                        {
                            UBGLComp.SwithToB();
                        }
                    }
                    else
                    {
                        UBGLComp.SwithToB();
                    }
                }
            }
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire);
        }
    }
}
