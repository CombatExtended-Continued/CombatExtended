using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;

namespace CombatExtended
{
    public class TurretTracker : MapComponent
    {
        public HashSet<Building_Turret> Turrets = new HashSet<Building_Turret>();

        public TurretTracker(Map map) : base(map)
        {
        }

        public void Register(Building_Turret t)
        {
            if (!Turrets.Contains(t))
            {
                Turrets.Add(t);
            }
        }

        public void Unregister(Building_Turret t)
        {
            if (Turrets.Contains(t))
            {
                Turrets.Remove(t);
            }
        }
    }
}
