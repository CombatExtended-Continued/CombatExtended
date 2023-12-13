using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended.CombatExtended.Comps
{
    public class CompAmmoUserGeneric : CompAmmoUser
    {
        private List<AmmoSetDef> usableAmmoSets = new List<AmmoSetDef>();

        public AmmoSetDef SelectedAmmoSet;

        public List<AmmoSetDef> UsableAmmoSets
        {
            get
            {
                if (usableAmmoSets.NullOrEmpty())
                {
                    foreach (var def in DefDatabase<AmmoSetDef>.AllDefs.Where((AmmoSetDef def) => def.generated))
                    {
                        if (def.similarTo == null)
                        {
                            continue;
                        }
                        if (def.similarTo == this.Props.ammoSet)
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
            SelectedAmmoSet = UsableAmmoSets.First();
            base.Initialize(vprops);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CE_SelectAmmoSet".Translate();
            command_Action.defaultDesc = "CommandSelectMineralToScanForDesc".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", reportFailure: false);
            command_Action.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (AmmoSetDef caliber in usableAmmoSets)
                {
                    FloatMenuOption item = new FloatMenuOption(caliber.LabelCap, delegate
                    {
                        this.SelectedAmmoSet = caliber;
                    }, MenuOptionPriority.Default, null, null);
                    list.Add(item);
                }
                Find.WindowStack.Add(new FloatMenu(list));
            };
            yield return command_Action;
        }
    }
}
