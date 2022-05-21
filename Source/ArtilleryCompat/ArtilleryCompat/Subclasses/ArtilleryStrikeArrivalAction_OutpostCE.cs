using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;
using VFESecurity;
using System.Collections.Generic;

namespace CombatExtended.Compatibility.Artillery
{
    public class ArtilleryStrikeArrivalAction_OutpostCE : ArtilleryStrikeArrivalAction_Outpost
    {
	public ArtilleryStrikeArrivalAction_OutpostCE()
        {
        }
	public ArtilleryStrikeArrivalAction_OutpostCE(WorldObject worldObject)
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

            // Destroy outpost and give reward
            if (cellsInRect > 0 && Rand.Chance(cellsInRect * DestroyChancePerCellInRect))
            {
                QuestUtility.SendQuestTargetSignals(Site.questTags, QuestUtility.QuestTargetSignalPart_AllEnemiesDefeated, Site.Named("SUBJECT"));
                NonPublicFields.Site_allEnemiesDefeatedSignalSent.SetValue(Site, true);
                Find.WorldObjects.Remove(worldObject);
                destroyed = true;
            }
        }
    }
}
