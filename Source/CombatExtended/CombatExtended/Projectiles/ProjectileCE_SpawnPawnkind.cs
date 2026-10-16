using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended;
public class ProjectileCE_SpawnPawnkind : ProjectileCE
{
    public override bool AnimalsFleeImpact => false;

    public override void Impact(Thing hitThing)
    {
        Map map = this.Map;
        base.Impact(hitThing);

        if (this.def.projectile is not ProjectilePropertiesCE props)
        {
            return;
        }

        PawnKindDef spawnsPawnKind = props.spawnsPawnKind;

        if (spawnsPawnKind == null)
        {
            Log.ErrorOnce($"Projectile {this.def.defName} missing spawnsPawnKind definition", this.def.GetHashCode());
            return;
        }

        IntVec3 loc = this.Position;
        if (this.def.projectile.tryAdjacentFreeSpaces && this.Position.GetFirstBuilding(map) != null)
        {
            foreach (IntVec3 c in GenAdjFast.AdjacentCells8Way(this.Position))
            {
                if (c.GetFirstBuilding(map) == null && c.Standable(map))
                {
                    loc = c;
                    break;
                }
            }
        }
        // Fallback to launcher's faction if no configured faction or faction not found
        Faction faction = null;

        if (props.factionDef != null)
        {
            faction = Find.FactionManager.FirstFactionOfDef(props.factionDef);
            if (faction == null)
            {
                Log.Warning($"Could not find faction {props.factionDef.defName} for projectile {this.def.defName}");
            }
        }

        faction ??= this.launcher?.Faction;

        PlanetTile? tile = null;
        float? fixedBiologicalAge = props.fixedBiologicalAge;
        float? fixedChronologicalAge = props.fixedChronologicalAge;
        Gender? fixedGender = null;
        FloatRange? excludeBiologicalAgeRange = props.excludeBiologicalAgeRange;
        FloatRange? biologicalAgeRange = props.biologicalAgeRange;

        PawnGenerationRequest request = new PawnGenerationRequest(
            kind: spawnsPawnKind,
            faction: faction,
            tile: tile,
            fixedBiologicalAge: fixedBiologicalAge,
            fixedChronologicalAge: fixedChronologicalAge,
            fixedGender: fixedGender,
            excludeBiologicalAgeRange: excludeBiologicalAgeRange,
            biologicalAgeRange: biologicalAgeRange
        );

        Pawn pawn = PawnGenerator.GeneratePawn(request);
        if (pawn == null)
        {
            Log.ErrorOnce($"Failed to generate pawn of kind {spawnsPawnKind.defName}", Gen.HashCombine(this.def.GetHashCode(), "PawnGenFailed"));
            return;
        }

        MentalStateDef mentalStateDef = props.mentalStateDef;
        GenSpawn.Spawn(pawn, loc, map);
        if (props.forceMentalState && mentalStateDef != null && pawn.mindState != null)
        {
            pawn.mindState.mentalStateHandler.TryStartMentalState(mentalStateDef);
        }

    }
}
