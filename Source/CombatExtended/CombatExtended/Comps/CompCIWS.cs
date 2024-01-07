using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public abstract class CompCIWS : ThingComp
    {
        public abstract bool TryFindTarget(IAttackTargetSearcher targetSearcher, out LocalTargetInfo result);
        public abstract bool CheckImpact(ProjectileCE projectile);
        public bool Active => (parent is Building_TurretGunCE turretCE) ? turretCE.Active : PowerTrader?.PowerOn ?? true;
        public CompProperties_CIWS Props => props as CompProperties_CIWS;

        private CompPowerTrader powerTrader;
        private bool cachedPowerTrader = false;
        private CompPowerTrader PowerTrader
        {
            get
            {
                if (!cachedPowerTrader)
                {
                    powerTrader = parent.TryGetComp<CompPowerTrader>();
                    cachedPowerTrader = true;
                }
                return powerTrader;
            }
        }
        public static IEnumerable<Gizmo> GetGizmos(IEnumerable<CompCIWS> all, List<CompCIWS> active)
        {
            if (!all.Any())
            {
                yield break;
            }
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.AddRange(all.Select(x => new FloatMenuOption(x.Props.label.Translate(), () =>
            {
                active.Clear();
                active.Add(x);
            })));
            options.Add(new FloatMenuOption("AllCiws".Translate(), () =>
            {
                active.Clear();
                active.AddRange(all);
            }));
            options.Add(new FloatMenuOption("CiwsDisabled".Translate(), () =>
            {
                active.Clear();
            }));
            yield return new Command_Action
            {
                defaultLabel = "CE_ChooseCIWS".Translate() + "...",
                defaultDesc = "CE_ChooseCIWSDesc".Translate(),
                icon = active.SequenceEqual(all) ? CIWS_All_Active_Icon : active.FirstOrDefault()?.CIWS_Icon ?? CIWS_Deactivated_Icon,
                action = delegate ()
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }
        static Texture CIWS_Deactivated_Icon => null;
        static Texture CIWS_All_Active_Icon => HumanEmbryo.ImplantIcon.Texture;
        protected virtual Texture CIWS_Icon => null;
    }
}
