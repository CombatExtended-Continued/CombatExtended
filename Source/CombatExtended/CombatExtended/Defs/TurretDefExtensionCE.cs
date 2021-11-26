using System;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class TurretDefExtensionCE : DefModExtension
    {
        /// <summary>
        /// Minimum ciws shots for destroying a projectile.
        /// </summary>
        public int ciwsMinShotsRequired;
        /// <summary>
        /// Maximum ciws shots for destroying a projectile.
        /// </summary>
        public int ciwsMaxShotsRequired;
        /// <summary>
        /// Animation data for extra turret part.
        /// </summary>
        public AnimatedPart animatedPart;        
    }
}

