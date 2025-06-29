using RimWorld;
using Verse;

namespace CombatExtended.AI
{
    public class CompGasMask : ICompTactics
    {
        private const string GASMASK_TAG = "GasMask";

        private const int SMOKE_TICKS_OFFSET = 800;
        private const int LONG_GAS_TICKS_OFFSET = 3500; // Just over the check interval of 3451 in Vanilla

        private int lastSmokeTick = -1;

        private bool maskEquiped = false;

        public override int Priority => 50;

        private int ticks = 0;

        public override void TickRarer()
        {
            base.TickRarer();
            if (ticks % 2 == 0)
            {
                UpdateGasMask();
            }
            ticks++;
        }

        private void UpdateGasMask()
        {
            if (SelPawn.Faction.IsPlayerSafe())
            {
                return;
            }
            if (!SelPawn.Spawned)
            {
                return;
            }
            if (SelPawn.Downed || SelPawn.apparel?.wornApparel == null)
            {
                return;
            }
            if (ticks % 4 == 0)
            {
                CheckForMask();
            }
            if (lastSmokeTick < GenTicks.TicksGame && maskEquiped)
            {
                RemoveMask();
            }
        }

        public void Notify_ShouldEquipGasMask(bool longOffset = false)
        {
            if (SelPawn.Faction.IsPlayerSafe() || SelPawn.Downed || SelPawn.apparel?.wornApparel == null)
            {
                return;
            }
            if (lastSmokeTick < GenTicks.TicksGame && !maskEquiped)
            {
                WearMask();
            }
            if (maskEquiped)
            {
                int newTick = GenTicks.TicksGame + (longOffset ? LONG_GAS_TICKS_OFFSET : SMOKE_TICKS_OFFSET);
                if (longOffset || newTick > lastSmokeTick)
                {
                    lastSmokeTick = newTick;
                }
            }
            else
            {
                lastSmokeTick = -1;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastSmokeTick, "lastSmokeTick", -1);
            Scribe_Values.Look(ref maskEquiped, "maskEquiped", false);
        }

        private void WearMask()
        {
            foreach (Thing thing in CompInventory.container)
            {
                if (thing is Apparel apparel && (apparel.def.apparel.tags?.Contains(GASMASK_TAG) ?? false))
                {
                    SelPawn.inventory.innerContainer.Remove(apparel);
                    SelPawn.apparel.Wear(apparel);
                    maskEquiped = true;
                    return;
                }
            }
        }

        private void RemoveMask()
        {
            foreach (Apparel apparel in SelPawn.apparel.wornApparel)
            {
                if (apparel.def.apparel.tags?.Contains(GASMASK_TAG) ?? false)
                {
                    SelPawn.apparel.Remove(apparel);
                    SelPawn.inventory.innerContainer.TryAddOrTransfer(apparel);
                    break;
                }
            }
            maskEquiped = false;
        }

        private void CheckForMask()
        {
            foreach (Apparel apparel in SelPawn.apparel.wornApparel)
            {
                if (apparel.def.apparel.tags?.Contains(GASMASK_TAG) ?? false)
                {
                    maskEquiped = true;
                    return;
                }
            }
            maskEquiped = false;
        }
    }
}
