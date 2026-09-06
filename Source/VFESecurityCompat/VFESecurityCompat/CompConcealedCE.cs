using Verse;
using VFESecurity;
#nullable enable

namespace CombatExtended.Compatibility.VFES
{
    // Facade over VFE's CompConcealed that also manages an invisible "dummy"
    // thing parked on the same cell as the parent building. The dummy's
    // passability (chosen per-building via CompProperties_ConcealedCE.dummyDef)
    // is what makes the deployed structure block pathing:
    //   - turret dummy  : PassThroughOnly + pathCost  -> soft, avoidable obstacle
    //   - barrier dummy : Impassable                   -> hard wall
    // When submerged the dummy is removed, leaving the (Standable) parent def,
    // so the cell reverts to a free, standable floor. No Harmony on GenGrid /
    // pathfinding -- we only touch the local cell via the dummy's presence.
    public class CompConcealedCE : ThingComp
    {
        private CompConcealed? concealed;
        private CompProperties_ConcealedCE? ceProps;
        private Thing? dummy;
        private bool loaded;

        public bool Submerged => concealed?.Submerged ?? false;

        public Graphic? SubmergedGraphic => concealed?.Props.submergedGraphic?.Graphic;

        private ThingDef? DummyDef => ceProps?.dummyDef;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            concealed = parent.GetComp<CompConcealed>();
            ceProps = props as CompProperties_ConcealedCE;
            // On load, defer the reconcile to the first tick so any saved dummy
            // has already been instantiated (avoids spawning a duplicate).
            loaded = !respawningAfterLoad;
            if (loaded)
            {
                SyncDummy();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            DestroyDummy();
        }

        public override void CompTick()
        {
            if (!loaded)
            {
                loaded = true;
                SyncDummy();
            }
        }

        public override void CompTickRare()
        {
            SyncDummy();
        }

        private void DestroyDummy()
        {
            // normal path: we always hold the live ref, so just destroy it.
            // if it was already destroyed out from under us, the field is stale
            // and there is nothing left to do.
            if (dummy != null && !dummy.Destroyed)
            {
                dummy.Destroy();
                dummy = null;
            }
        }

        private Thing? FindDummy()
        {
            if (dummy != null && !dummy.Destroyed)
            {
                return dummy;
            }
            ThingDef? dummyDef = DummyDef;
            if (dummyDef != null && parent.Spawned)
            {
                return parent.Map.thingGrid.ThingAt(parent.Position, dummyDef);
            }
            return null;
        }

        private void SyncDummy()
        {
            if (concealed == null)
            {
                return;
            }
            bool wantDummy = !concealed.Submerged;
            Thing? existing = FindDummy();

            // already in the desired state -> nothing to do
            if (wantDummy == (existing != null))
            {
                return;
            }
            Map map = parent.Map;
            if (wantDummy)
            {
                ThingDef? dummyDef = DummyDef;
                if (parent.Spawned && dummyDef != null)
                {
                    Thing made = ThingMaker.MakeThing(dummyDef);
                    dummy = GenSpawn.Spawn(made, parent.Position, map, parent.Rotation);
                    map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
                }
            }
            else
            {
                if (existing != null)
                {
                    existing.Destroy();
                    map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
                }
                dummy = null;
            }
        }
    }
}
#nullable restore
