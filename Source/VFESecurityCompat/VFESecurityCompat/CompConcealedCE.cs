using Verse;
using VFESecurity;
#nullable enable

namespace CombatExtended.Compatibility.VFES
{
    // VFE's CompConcealed + an invisible dummy parked on the same cell. dummy's
    // passability is what blocks pathing when deployed (turret: soft, barrier:
    // wall). submerged = dummy gone = free floor. no harmony, just the one cell.
    public class CompConcealedCE : ThingComp
    {
        private CompConcealed? concealed;
        private CompProperties_ConcealedCE? ceProps;
        private Thing? dummy;
        private bool lastSubmerged;

        public bool Submerged => concealed?.Submerged ?? false;

        public Graphic? SubmergedGraphic => concealed?.Props.submergedGraphic?.Graphic;

        private ThingDef? DummyDef => ceProps?.dummyDef;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            concealed = parent.GetComp<CompConcealed>();
            ceProps = props as CompProperties_ConcealedCE;
            if (concealed == null)
            {
                return;
            }
            // load: a saved dummy might already sit here, adopt it once. don't
            // spawn a dup, don't defer to tick 1. only thingGrid scan we do.
            if (respawningAfterLoad)
            {
                FindDummy();
            }
            lastSubmerged = concealed.Submerged;
            SyncDummy();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            DestroyDummy();
        }

        public override void CompTick()
        {
            if (concealed == null)
            {
                return;
            }
            // only act when Submerged actually flips. holding the ref, so no scan.
            bool sub = concealed.Submerged;
            if (sub != lastSubmerged)
            {
                lastSubmerged = sub;
                SyncDummy();
            }
        }

        private void DestroyDummy()
        {
            // we hold the ref, so just nuke it. if something else killed it
            // first, field's stale, nothing to do.
            if (dummy != null && !dummy.Destroyed)
            {
                dummy.Destroy();
                dummy = null;
            }
        }

        // load-only: re-adopt a saved dummy. nowhere else calls this.
        private void FindDummy()
        {
            if (dummy != null && !dummy.Destroyed)
            {
                return;
            }
            ThingDef? dummyDef = DummyDef;
            if (dummyDef != null && parent.Spawned)
            {
                dummy = parent.Map.thingGrid.ThingAt(parent.Position, dummyDef);
            }
        }

        private void SyncDummy()
        {
            if (concealed == null || !parent.Spawned)
            {
                return;
            }
            bool wantDummy = !concealed.Submerged;
            bool haveDummy = dummy != null && !dummy.Destroyed;

            // already matched, move on
            if (wantDummy == haveDummy)
            {
                return;
            }
            Map map = parent.Map;
            if (wantDummy)
            {
                ThingDef? dummyDef = DummyDef;
                if (dummyDef != null)
                {
                    Thing made = ThingMaker.MakeThing(dummyDef);
                    dummy = GenSpawn.Spawn(made, parent.Position, map, parent.Rotation);
                    map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
                }
            }
            else
            {
                DestroyDummy();
                map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
            }
        }
    }
}
#nullable restore
