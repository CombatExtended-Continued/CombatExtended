using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class CompVariableAmmoUser : CompAmmoUser
    {
        protected List<AmmoSetDef> usableAmmoSets = new List<AmmoSetDef>();

        public AmmoSetDef SelectedAmmoSet;

        private AmmoSetDef currentlyHoveredOverAmmoSet = null;

        private string ammoSetContentDescCache;

        public virtual List<AmmoSetDef> UsableAmmoSets
        {
            get
            {
                if (usableAmmoSets.NullOrEmpty())
                {
                    usableAmmoSets.Add(Props.ammoSet);
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
                //turned out it was bold to assume similar ammosets always have same ammo types
                var tempAmmoLink = CurAmmoSet.ammoTypes.Where(l => l.ammo.ammoClass == currentAmmoInt.ammoClass);
                if (tempAmmoLink.Any())
                {
                    selectedAmmo = tempAmmoLink.First().ammo;
                }
                else
                {
                    selectedAmmo = CurAmmoSet.ammoTypes.First().ammo;
                }
            }
        }

        [Compatibility.Multiplayer.SyncMethod]
        private void SyncedSelectAmmoSet(AmmoSetDef caliber)
        {
            SelectedAmmoSet = caliber;
            RegenSelectedAmmo();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CE_SelectAmmoSet".Translate();
            command_Action.defaultDesc = "CE_SelectAmmoSetDesc".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", reportFailure: false);
            command_Action.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (AmmoSetDef caliber in UsableAmmoSets)
                {
                    FloatMenuOption item = new FloatMenuOption(
                        caliber.LabelCap,
                        delegate { SyncedSelectAmmoSet(caliber); },
                        caliber.ammoTypes.First().ammo,
                        priority: MenuOptionPriority.Default,
                        mouseoverGuiAction: delegate (Rect rect) { ContainedAmmoPopOut(rect, caliber); });
                    list.Add(item);
                }
                Find.WindowStack.Add(new FloatMenu(list));
            };
            yield return command_Action;
        }

        public void ContainedAmmoPopOut(Rect rect, AmmoSetDef ammoSet)
        {
            if (ammoSet != currentlyHoveredOverAmmoSet)
            {
                BuildAmmosetString(ammoSet);
            }
            TooltipHandler.TipRegion(rect, ammoSetContentDescCache);

        }

        public void BuildAmmosetString(AmmoSetDef ammoSetDef)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var v in ammoSetDef.ammoTypes)
            {
                stringBuilder.AppendLine(v.ammo.label);
            }
            ammoSetContentDescCache = stringBuilder.ToString();
        }
    }
}
