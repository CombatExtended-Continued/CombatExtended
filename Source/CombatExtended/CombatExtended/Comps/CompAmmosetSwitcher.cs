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

        public CompEquippable _compEq;

        public CompEquippable CompEq
        {
            get
            {
                if (_compEq == null)
                {
                    _compEq = this.parent.TryGetComp<CompEquippable>();
                }

                return _compEq;
            }
        }

        public CompAmmoUser _compAmmo;

        public CompAmmoUser CompAmmo
        {
            get
            {
                if (_compAmmo == null)
                {
                    _compAmmo = this.parent.TryGetComp<CompAmmoUser>();
                }

                return _compAmmo;
            }
        }

        public CompFireModes _compFireModes;

        public CompFireModes CompFireModes
        {
            get
            {
                if (_compFireModes == null)
                {
                    _compFireModes = this.parent.TryGetComp<CompFireModes>();
                }

                return _compFireModes;
            }
        }

        public CompProperties_FireModes _compPropsFireModes;

        public CompProperties_FireModes CompPropsFireModes
        {
            get
            {
                if (_compPropsFireModes == null)
                {
                    _compPropsFireModes = this.parent.def.comps.Find(x => x is CompProperties_FireModes) as CompProperties_FireModes;
                }

                return _compPropsFireModes;
            }
        }

        public VerbProperties _defVerbProps;

        public VerbProperties DefVerbProps
        {
            get
            {
                if (_defVerbProps == null)
                {
                    _defVerbProps = this.parent.def.Verbs.Find(x => x is VerbPropertiesCE);
                }

                return _defVerbProps;
            }
        }

        public CompProperties_AmmoUser _compPropsAmmo;

        public CompProperties_AmmoUser CompPropsAmmo
        {
            get
            {
                if (_compPropsAmmo == null)
                {
                    _compPropsAmmo = (CompProperties_AmmoUser)this.parent.def.comps.Find(x => x is CompProperties_AmmoUser);
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
            if (!Props.oneAmmoHolder)
            {
                mainGunLoadedAmmo = CompAmmo.CurrentAmmo;
                mainGunMagCount = CompAmmo.CurMagCount;
            }

            CompAmmo.props = this.Props.propsUnderBarrel;
            CompEq.PrimaryVerb.verbProps = Props.verbPropsUnderBarrel;
            CompFireModes.props = this.Props.propsFireModesUnderBarrel;
            CompAmmo.CurMagCount = UnderBarrelMagCount;
            CompAmmo.CurrentAmmo = UnderBarrelLoadedAmmo;
            CompAmmo.SelectedAmmo = CompAmmo.CurrentAmmo;
            if (CompAmmo.Wielder != null)
            {
                CompAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();

                if (CompAmmo.Wielder.jobs?.curJob?.def == CE_JobDefOf.ReloadWeapon)
                {
                    CompAmmo.Wielder.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
            }
            CompEq.PrimaryVerb.verbProps.burstShotCount = this.Props.propsFireModesUnderBarrel.aimedBurstShotCount;
            usingUnderBarrel = true;
        }

        public void SwithToB()
        {
            if (!Props.oneAmmoHolder)
            {
                UnderBarrelLoadedAmmo = CompAmmo.CurrentAmmo;
                UnderBarrelMagCount = CompAmmo.CurMagCount;
            }

            CompAmmo.props = CompPropsAmmo;
            CompEq.PrimaryVerb.verbProps = DefVerbProps.MemberwiseClone();
            CompFireModes.props = CompPropsFireModes;
            CompAmmo.CurMagCount = mainGunMagCount;
            CompAmmo.CurrentAmmo = mainGunLoadedAmmo;
            CompAmmo.SelectedAmmo = CompAmmo.CurrentAmmo;
            if (CompAmmo.Wielder != null)
            {
                CompAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();

                if (CompAmmo.Wielder.jobs?.curJob?.def == CE_JobDefOf.ReloadWeapon)
                {
                    CompAmmo.Wielder.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
            }
            CompEq.PrimaryVerb.verbProps.burstShotCount = DefVerbProps.burstShotCount;
            usingUnderBarrel = false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (CompAmmo.Props.ammoSet == CompPropsAmmo.ammoSet)
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
                    UnderBarrelMagCount = CompAmmo.CurMagCount;
                    UnderBarrelLoadedAmmo = CompAmmo.CurrentAmmo;
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
                    if (!Props.oneAmmoHolder)
                    {
                        CompAmmo.CurMagCount = UnderBarrelMagCount;
                        CompAmmo.CurrentAmmo = UnderBarrelLoadedAmmo;
                    }

                    CompAmmo.props = this.Props.propsUnderBarrel;
                    CompEq.PrimaryVerb.verbProps = Props.verbPropsUnderBarrel;
                    CompFireModes.props = this.Props.propsFireModesUnderBarrel;

                    if (CompAmmo.Wielder != null)
                    {
                        CompAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();
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

        public bool oneAmmoHolder = false;

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