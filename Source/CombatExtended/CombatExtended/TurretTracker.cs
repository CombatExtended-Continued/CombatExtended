using System.Collections.Generic;
using System.Text;
using Verse;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class TurretTracker : MapComponent
    {
        public List<Building_TurretGunCE> Turrets = new List<Building_TurretGunCE>();
        
        public TurretTracker(Map map) : base(map)
        {
        }
        
        public void Register(Building_TurretGunCE t)
        {
            if (!Turrets.Contains(t)) {
                Turrets.Add(t);
            }
        }

        public void Unregister(Building_TurretGunCE t)
        {
            if (Turrets.Contains(t)) {
                Turrets.Remove(t);
            }
        }
    }
}
