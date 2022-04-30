using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompUnderBarrel : CompRangedGizmoGiver
    {
        public CompProperties_UnderBarrel Props => (CompProperties_UnderBarrel)this.props;

        public CompEquippable compEq => this.parent.TryGetComp<CompEquippable>();

        public CompAmmoUser compAmmo => this.parent.TryGetComp<CompAmmoUser>();

        public CompFireModes compFireModes => this.parent.TryGetComp<CompFireModes>();

        public CompProperties_FireModes compPropsFireModes => this.parent.def.comps.Find(x => x is CompProperties_FireModes) as CompProperties_FireModes;

        public VerbProperties defVerbProps => this.parent.def.Verbs.Find(x => x is VerbPropertiesCE);

        public CompProperties_AmmoUser compPropsAmmo => (CompProperties_AmmoUser)this.parent.def.comps.Find(x => x is CompProperties_AmmoUser);

        public AmmoDef mainGunLoadedAmmo;

        public int mainGunMagCount;

        public AmmoDef UnderBarrelLoadedAmmo;

        public int UnderBarrelMagCount;

        public bool usingUnderBarrel;
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
                };
            }
        }

        public override string TransformLabel(string label)
        {
            /*if (!(compAmmo.Props.ammoSet == compPropsAmmo.ammoSet))
            {
                return this.parent.Label + " (" + compAmmo.Props.ammoSet.label + ")";
            }*/
            return base.TransformLabel(label);
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
    }
}