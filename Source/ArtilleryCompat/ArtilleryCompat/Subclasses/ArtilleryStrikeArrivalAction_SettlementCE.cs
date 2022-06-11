using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;
using VFESecurity;
using System.Collections.Generic;

namespace CombatExtended.Compatibility.Artillery
{
    public class ArtilleryStrikeArrivalAction_SettlementCE : ArtilleryStrikeArrivalAction_Settlement
    {
	public ArtilleryStrikeArrivalAction_SettlementCE()
        {
        }

	public ArtilleryStrikeArrivalAction_SettlementCE(WorldObject worldObject)
        {
            this.worldObject = worldObject;
        }

	public override void Arrived(List<ActiveArtilleryStrike> artilleryStrikes, int tile)
	{
	    this.ArrivedCE(artilleryStrikes, tile);
	}

	public override void StrikeAction(ActiveArtilleryStrike strike, CellRect mapRect, CellRect baseRect, ref bool destroyed)
        {
            var radialCells = GenRadial.RadialCellsAround(mapRect.RandomCell, strike.shellDef.GetProjectileProperties().explosionRadius, true);
            int cellsInRect = radialCells.Count(c => baseRect.Contains(c));

            // Destroy settlement
            if (cellsInRect > 0 && Rand.Chance(cellsInRect * DestroyChancePerCellInRect))
            {
                Find.LetterStack.ReceiveLetter("LetterLabelFactionBaseDefeated".Translate(), "VFESecurity.LetterFactionBaseDefeatedStrike".Translate(Settlement.Label), LetterDefOf.PositiveEvent,
                    new GlobalTargetInfo(Settlement.Tile), Settlement.Faction, null);
                var destroyedSettlement = (DestroyedSettlement)WorldObjectMaker.MakeWorldObject(RimWorld.WorldObjectDefOf.DestroyedSettlement);
                destroyedSettlement.Tile = Settlement.Tile;
                Find.WorldObjects.Add(destroyedSettlement);
                Find.WorldObjects.Remove(Settlement);
                destroyed = true;
            }
        }
	public override void PostStrikeAction(bool destroyed)
        {
            if (!destroyed)
            {
                // Otherwise artillery retaliation
                var artilleryComp = Settlement.GetComponent<ArtilleryComp>();
                if (artilleryComp != null)
                    artilleryComp.TryStartBombardment();
            }
        }

    }
}
