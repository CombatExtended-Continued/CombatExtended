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
        private Thing? dummy;
        private bool loaded;

        public bool Submerged => concealed?.Submerged ?? false;

        public Graphic? SubmergedGraphic => concealed?.Props.submergedGraphic?.Graphic;

        private ThingDef? DummyDef => (props as CompProperties_ConcealedCE)?.dummyDef;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            concealed = parent.GetComp<CompConcealed>();
            // On load, defer the reconcile to the first tick so any saved dummy
            // has already been instantiated (avoids spawning a duplicate).
            loaded = !respawningAfterLoad;
            if (!respawningAfterLoad)
                SyncDummy();
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
                return;
            }
            SyncDummy();
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
                dummy.Destroy();
            dummy = null;
        }

        private Thing? FindDummy()
        {
            if (dummy != null && !dummy.Destroyed)
                return dummy;
            if (DummyDef != null && parent.Spawned)
            {
                foreach (Thing t in parent.Map.thingGrid.ThingsListAt(parent.Position))
                {
                    if (t.def == DummyDef)
                        return t;
                }
            }
            return null;
        }

        private void SyncDummy()
        {
            if (concealed == null)
                return;

            bool wantDummy = !concealed.Submerged;
            Thing? existing = FindDummy();

            // already in the desired state -> nothing to do
            if (wantDummy == (existing != null))
                return;

            if (wantDummy)
            {
                if (parent.Spawned && DummyDef != null)
                {
                    Thing made = ThingMaker.MakeThing(DummyDef);
                    dummy = GenSpawn.Spawn(made, parent.Position, parent.Map, parent.Rotation, WipeMode.Vanish);
                    parent.Map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
                }
            }
            else
            {
                if (existing != null)
                {
                    existing.Destroy();
                    parent.Map.pathing.RecalculatePerceivedPathCostAt(parent.Position);
                }
                dummy = null;
            }
        }
    }
}
#nullable restore
