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

        //Merge ammosets with identical ammo usage. I'm worried about its performance and I might want to caculate this during game start up.
        protected bool IsIdenticalToAny(AmmoSetDef def)
        {
            if (usableAmmoSets.NullOrEmpty())
            {
                return false;
            }
            foreach (var v in usableAmmoSets)
            {
                if (IsIdenticalTo(v, def))
                {
                    return true;
                }
            }
            return false;
        }

        protected bool IsIdenticalTo(AmmoSetDef a, AmmoSetDef b)
        {
            if (a.ammoTypes.Count != b.ammoTypes.Count)
            {
                return false;
            }
            HashSet<AmmoDef> list = new HashSet<AmmoDef>();
            foreach (var v in a.ammoTypes)
            {
                list.Add(v.ammo);
            }
            foreach (var n in b.ammoTypes)
            {
                if (!list.Contains(n.ammo))
                {
                    return false;
                }
            }
            return true;
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
            if (!Controller.settings.GenericAmmo)
            {
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
