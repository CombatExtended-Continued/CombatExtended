using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public class FlyOverProjectileTracker : MapComponent
    {
        private HashSet<Projectile> projectiles = new HashSet<Projectile>();

        private HashSet<ProjectileCE> projectilesCE = new HashSet<ProjectileCE>();

        public IEnumerable<ProjectileCE> ProjectilesCE
        {
            get => projectilesCE;
        }
        public IEnumerable<Projectile> Projectiles
        {
            get => projectiles.Where(p => p?.Spawned ?? false);
        }

        public FlyOverProjectileTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if ((GenTicks.TicksGame + 13) % 30 == 0 && projectiles.Count > 0)
            {
                projectiles.RemoveWhere(p => p == null || p.Destroyed || !p.Spawned);
            }
            if ((GenTicks.TicksGame + 17) % 2000 == 0 && projectilesCE.Count > 0)
            {
                projectilesCE.RemoveWhere(p => p == null || p.Destroyed || !p.Spawned);
            }
            base.MapComponentTick();            
        }

        public void Register(ProjectileCE p)
        {
            if (!projectilesCE.Contains(p) && (p.def.projectile?.flyOverhead ?? false))
            {
                projectilesCE.Add(p);
            }
        }

        public void Unregister(ProjectileCE p)
        {
            if (projectilesCE.Contains(p) && (p.def.projectile?.flyOverhead ?? false))
            {
                projectilesCE.Remove(p);
            }
        }

        public void Register(Projectile p)
        {
            if (!projectiles.Contains(p) && (p.def.projectile?.flyOverhead ?? false))
            {
                projectiles.Add(p);
            }
        }       
    }
}

