using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility.VFES
{
    public class Building_ConcealedTurretCE : Building_TurretGunCE
    {
        private CompConcealed concealedComp;

        public override bool IsEverThreat
        {
            get
            {
                return concealedComp?.Submerged ?? base.IsEverThreat;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (concealedComp != null && concealedComp.Submerged && concealedComp?.Props.submergedGraphic != null)
                {
                    return concealedComp.Props.submergedGraphic.Graphic;
                }
                return base.Graphic;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            concealedComp = GetComp<CompConcealed>();
        }
    }
}
