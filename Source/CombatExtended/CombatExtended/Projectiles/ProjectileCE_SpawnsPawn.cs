using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended;
public class ProjectileCE_SpawnsPawn : ProjectileCE
{
    public override bool AnimalsFleeImpact => false;

    public override void Impact(Thing hitThing)
    {
        Map map = this.Map;
        base.Impact(hitThing);
        
        if (this.def?.projectile?.spawnsPawnKind == null)
        {
            Log.Warning($"Projectile {this.def?.defName} missing spawnsPawnKind definition");
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
        
        PawnKindDef spawnsPawnKind = this.def.projectile.spawnsPawnKind;
        Faction factionlauncher = this.launcher?.Faction;
        
        // Get CE projectile properties
        ProjectilePropertiesCE props = this.def.projectile as ProjectilePropertiesCE;
        
        // Use configured faction if specified, otherwise use launcher's faction
        Faction faction = null;
        if (props?.factionDef != null)
        {
            faction = Find.FactionManager.FirstFactionOfDef(props.factionDef);
            if (faction == null)
            {
                Log.Warning($"Could not find faction {props.factionDef.defName} for projectile {this.def.defName}");
            }
        }
        
        // Fallback to launcher's faction if no configured faction or faction not found
        if (faction == null)
        {
            faction = factionlauncher;
        }

        bool alwaysHostile = props?.alwaysHostile ?? false;
        if (alwaysHostile && faction == Faction.OfPlayer)
        {
            Faction hostileFaction = Find.FactionManager.AllFactions
                .Where(f => f != Faction.OfPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer))
                .OrderBy(f => f.PlayerGoodwill)
                .FirstOrDefault();
            
            if (hostileFaction != null)
            {
                faction = hostileFaction;
            }
        }
        
        PlanetTile? tile = new PlanetTile?();
        float? minChanceToRedressWorldPawn = new float?();
        float? fixedBiologicalAge = props?.fixedBiologicalAge;
        float? fixedChronologicalAge = props?.fixedChronologicalAge;
        Gender? fixedGender = new Gender?();
        FloatRange? excludeBiologicalAgeRange = props?.excludeBiologicalAgeRange;
        FloatRange? biologicalAgeRange = props?.biologicalAgeRange;
        
        PawnGenerationRequest request = new PawnGenerationRequest(
            kind: spawnsPawnKind,
            faction: faction,
            tile: tile,
            minChanceToRedressWorldPawn: minChanceToRedressWorldPawn,
            fixedBiologicalAge: fixedBiologicalAge,
            fixedChronologicalAge: fixedChronologicalAge,
            fixedGender: fixedGender,
            excludeBiologicalAgeRange: excludeBiologicalAgeRange,
            biologicalAgeRange: biologicalAgeRange
        );
        
        Pawn pawn = PawnGenerator.GeneratePawn(request);
        
        if (pawn == null)
        {
            Log.Warning($"Failed to generate pawn of kind {spawnsPawnKind.defName}");
            return;
        }
        
        if (alwaysHostile)
        {
            pawn.mindState.enemyTarget = this.launcher;
        }
        
        GenSpawn.Spawn((Thing)pawn, loc, map);
        
        
        if (pawn.Faction != faction && pawn.def.CanHaveFaction)
        {
            pawn.SetFaction(faction);
        }
    }
}
