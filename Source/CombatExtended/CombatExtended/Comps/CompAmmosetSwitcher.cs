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
    public class CompUBGL: CompRangedGizmoGiver
    {
        public CompProperties_UBGL Props => (CompProperties_UBGL)this.props;

        public CompEquippable compEq => this.parent.TryGetComp<CompEquippable>();

        public CompAmmoUser compAmmo => this.parent.TryGetComp<CompAmmoUser>();

        public CompFireModes compFireModes => this.parent.TryGetComp<CompFireModes>();

        public CompProperties_FireModes compPropsFireModes => this.parent.def.comps.Find(x => x is CompProperties_FireModes) as CompProperties_FireModes;

        public VerbProperties defVerbProps => this.parent.def.Verbs.Find(x => x is VerbPropertiesCE);

        public CompProperties_AmmoUser compPropsAmmo => (CompProperties_AmmoUser)this.parent.def.comps.Find(x => x is CompProperties_AmmoUser);

        public AmmoDef mainGunLoadedAmmo;

        public int mainGunMagCount;

        public AmmoDef ubglLoadedAmmo;

        public int ubglMagCount;

        public bool usingUBGL;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (compAmmo.Props.ammoSet == compPropsAmmo.ammoSet)
            {
                yield return new Command_Action
                {

                    defaultLabel = "CE_SwitchAmmmoSetToUBGL".Translate(),
                    action = delegate
                    {
                        mainGunLoadedAmmo = compAmmo.CurrentAmmo;
                        mainGunMagCount = compAmmo.CurMagCount;

                        compAmmo.props = this.Props.propsUBGL;
                        compEq.PrimaryVerb.verbProps = Props.verbPropsUBGL;
                        compFireModes.props = this.Props.propsFireModesUBGL;
                        compAmmo.CurMagCount = ubglMagCount;
                        compAmmo.CurrentAmmo = ubglLoadedAmmo;
                        if (compAmmo.Wielder != null)
                        {
                            compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();
                        }
                        usingUBGL = true;
                    }
                };
            }
            else
            {
                yield return new Command_Action
                {

                    defaultLabel = "CE_SwitchAmmmoSetToNormalRifle".Translate(),
                    action = delegate
                    {
                        ubglLoadedAmmo = compAmmo.CurrentAmmo;
                        ubglMagCount = compAmmo.CurMagCount;

                        compAmmo.props = compPropsAmmo;
                        compEq.PrimaryVerb.verbProps = defVerbProps.MemberwiseClone();
                        compFireModes.props = compPropsFireModes;
                        compAmmo.CurMagCount = mainGunMagCount;
                        compAmmo.CurrentAmmo = mainGunLoadedAmmo;
                        if (compAmmo.Wielder != null)
                        {
                            compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();
                        }
                        usingUBGL = false;
                    }
                };
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref usingUBGL, "usingUBGL");
            Scribe_Defs.Look(ref mainGunLoadedAmmo, "mainGunAmmo");
            Scribe_Defs.Look(ref ubglLoadedAmmo, "ubglAmmo");
            Scribe_Values.Look(ref mainGunMagCount, "magCountMainGun");
            Scribe_Values.Look(ref ubglMagCount, "ubglMagCount");
            if (usingUBGL)
            {
                mainGunLoadedAmmo = compAmmo.CurrentAmmo;
                mainGunMagCount = compAmmo.CurMagCount;
                compAmmo.props = this.Props.propsUBGL;
                compEq.PrimaryVerb.verbProps = Props.verbPropsUBGL;
                compFireModes.props = this.Props.propsFireModesUBGL;
                compAmmo.CurMagCount = ubglMagCount;
                compAmmo.CurrentAmmo = ubglLoadedAmmo;
                if (compAmmo.Wielder != null)
                {
                    compAmmo.Wielder.TryGetComp<CompInventory>().UpdateInventory();
                }
            }
            base.PostExposeData();
        }
    }

    public class CompProperties_UBGL : CompProperties
    {

        public CompProperties_AmmoUser propsUBGL;

        public VerbPropertiesCE verbPropsUBGL;

        public CompProperties_FireModes propsFireModesUBGL;

        public CompProperties_UBGL()
        {
            this.compClass = typeof(CompUBGL);
        }
    }
}
