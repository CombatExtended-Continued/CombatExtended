using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
