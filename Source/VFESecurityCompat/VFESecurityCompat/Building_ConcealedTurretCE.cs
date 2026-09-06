using UnityEngine;
using Verse;
#nullable enable
namespace CombatExtended.Compatibility.VFES
{
    public class Building_ConcealedTurretCE : Building_TurretGunCE
    {
        private CompConcealedCE? concealedComp;

        public bool Submerged => concealedComp?.Submerged ?? false;

        public override bool Active => (!Submerged && base.Active);

        public override bool IsEverThreat
        {
            get
            {
                return concealedComp != null ? Submerged : base.IsEverThreat;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                Graphic? sub = concealedComp?.SubmergedGraphic;
                if (Submerged && sub != null)
                {
                    return sub;
                }
                return base.Graphic;
            }
        }

        // submerged = hidden in the floor. just draw the floor tile and skip the
        // gun so it doesn't poke out of the ground like an idiot.
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Submerged)
            {
                Graphic.Draw(drawLoc, Rotation, this);
                return;
            }
            base.DrawAt(drawLoc, flip);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            concealedComp = GetComp<CompConcealedCE>();
        }
    }
}
#nullable restore
