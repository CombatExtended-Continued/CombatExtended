using UnityEngine;
using Verse;
using VFESecurity;
#nullable enable
namespace CombatExtended.Compatibility.VFES
{
    public class Building_ConcealedTurretCE : Building_TurretGunCE
    {
        private CompConcealed? concealedComp;
        private bool lastSubmerged;

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
                if (concealedComp != null && concealedComp.Submerged && concealedComp.Props.submergedGraphic != null)
                {
                    return concealedComp.Props.submergedGraphic.Graphic;
                }
                return base.Graphic;
            }
        }

        // When submerged the turret is hidden in the floor: draw only the floor
        // graphic (linked to VFE's concealedComp.Props.submergedGraphic) and skip
        // the turret top/gun so it looks like a plain floor tile.
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
            concealedComp = GetComp<CompConcealed>();
            lastSubmerged = Submerged;
            UpdateFillAndPassability();
        }

        public override void Tick()
        {
            base.Tick();
            if (Submerged != lastSubmerged)
            {
                lastSubmerged = Submerged;
                UpdateFillAndPassability();
            }
        }

        // When submerged the turret acts like a floor (walkable, no cover).
        // When active it becomes a turret: passable-through and provides cover.
        private void UpdateFillAndPassability()
        {
            if (Submerged)
            {
                def.passability = Traversability.Standable;
                def.fillPercent = 0f;
            }
            else
            {
                def.passability = Traversability.PassThroughOnly;
                def.fillPercent = 0.85f;
            }
            Map?.pathing.RecalculatePerceivedPathCostAt(Position);
        }
    }
}
#nullable restore
