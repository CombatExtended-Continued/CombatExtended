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

        // OF COURSE the def is MapMeshAndRealTime, so the body lives in the map mesh
        // via Graphic (VFE helpfully dirties the whole mesh the instant state flips,
        // because why be efficient). This override is the ONLY thing that actually
        // shows the floor instead of the turret -- the dynamic DrawAt pass just
        // refuses to draw the body for this drawerType. Don't ask me why it's like this.
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

        // When it's in the floor, just... don't draw the gun. That's it. That's the whole
        // fix. The marker already comes from the mesh above. I am so tired.
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Submerged)
            {
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
