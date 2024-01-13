using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class CompAmmoUserGeneric : CompAmmoUser
    {
        private List<AmmoSetDef> usableAmmoSets = new List<AmmoSetDef>();

        public AmmoSetDef SelectedAmmoSet;

        public AmmoSetDef UsedGenericAmmoSet => Props.ammoSet.similarTo ?? Props.ammoSet;

        public List<AmmoSetDef> UsableAmmoSets
        {
            get
            {
                if (usableAmmoSets.NullOrEmpty())
                {
                    foreach (var def in DefDatabase<AmmoSetDef>.AllDefs)
                    {
                        if (def.similarTo == null)
                        {
                            continue;
                        }
                        if (def.similarTo == UsedGenericAmmoSet && !def.ammoTypes.First().ammo.menuHidden)
                        {
                            usableAmmoSets.Add(def);
                        }
                    }
                }
                return usableAmmoSets;
            }
        }

        public override AmmoSetDef CurAmmoSet => SelectedAmmoSet;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref SelectedAmmoSet, "SelectedAmmoSet");
        }

        public override void Initialize(CompProperties vprops)
        {
            base.Initialize(vprops);
            SelectedAmmoSet = Props.ammoSet;
            RegenSelectedAmmo();
        }
        public void RegenSelectedAmmo()
        {
            if (currentAmmoInt == null)
            {
                currentAmmoInt = (AmmoDef)CurAmmoSet.ammoTypes[0].ammo;
            }
            if (selectedAmmo == null)
            {
                selectedAmmo = currentAmmoInt;
            }
            else
            {
                selectedAmmo = CurAmmoSet.ammoTypes.Where(l => l.ammo.ammoClass == currentAmmoInt.ammoClass).First().ammo;
            }
        }

        [Compatibility.Multiplayer.SyncMethod]
        private void SyncedSelectAmmoSet(AmmoSetDef caliber)
        {
            SelectedAmmoSet = caliber; RegenSelectedAmmo();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (!Controller.settings.GenericAmmo)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CE_SelectAmmoSet".Translate();
                command_Action.defaultDesc = "CommandSelectMineralToScanForDesc".Translate();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", reportFailure: false);
                command_Action.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (AmmoSetDef caliber in UsableAmmoSets)
                    {
                        FloatMenuOption item = new FloatMenuOption(caliber.LabelCap, delegate
                        {
                            SyncedSelectAmmoSet(caliber);
                        }, MenuOptionPriority.Default, null, null);
                        list.Add(item);
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                };
                yield return command_Action;
            }
        }
    }
}
