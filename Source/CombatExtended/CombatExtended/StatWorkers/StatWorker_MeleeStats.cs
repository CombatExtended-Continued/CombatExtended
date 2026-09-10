using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_MeleeStats : StatWorker
{
    public override bool IsDisabledFor(Thing thing)
    {
        return thing?.def?.building?.IsTurret ?? base.IsDisabledFor(thing);
    }

    public override bool ShouldShowFor(StatRequest req)
    {
        if (base.ShouldShowFor(req))
        {
            return true;
        }

        // Show melee stats for artificial body parts that add melee tools.
        return stat.category == StatCategoryDefOf.Weapon &&
               req.Def is ThingDef { isTechHediff: true } thingDef && AllMeleeVerbPropsWithSource(thingDef).Any();
    }

    /// <summary>
    /// Get all melee verbs provided by a given weapon or tech hediff (e.g. artificial body part).
    /// </summary>
    /// <param name="thingDef">The def to fetch verbs for.</param>
    /// <returns>Available melee VerbProperties + Tool + Maneuver combinations for the given def.</returns>
    protected IEnumerable<VerbUtility.VerbPropertiesWithSource> AllMeleeVerbPropsWithSource(ThingDef thingDef)
    {
        var verbs = thingDef.Verbs;
        var tools = thingDef.tools;

        // For tech hediffs like artificial body parts, lookup whether they add any verbs when installed
        // and return them if so.
        if (thingDef.isTechHediff)
        {
            var props = DefDatabase<RecipeDef>.AllDefsListForReading
                .Where(recipe => recipe.IsIngredient(thingDef))
                .Select(recipe => recipe.addsHediff.CompProps<HediffCompProperties_VerbGiver>())
                .FirstOrDefault(props => props != null);

            if (props?.tools.All(tool => tool is ToolCE) ?? false)
            {
                verbs = props.verbs;
                tools = props.tools;
            }
        }

        return VerbUtility
            .GetAllVerbProperties(verbs, tools)
            .Where(vps => vps.verbProps.IsMeleeAttack);
    }

    /// <summary>
    /// Get the selection weight fir a single VerbProperties + Tool combination of a weapon,
    /// adjusted according to the wielder's (if any) stats/capacities and multipliers from the weapon's stuff.
    ///
    /// Note that this is only correct for calculating weights for a single weapon, since a pawn will likely
    /// have "natural" melee verbs provided by body parts, which may influence final weighting.
    /// </summary>
    /// <param name="vps">Verb data (VerbProperties + Tool + Maneuver)</param>
    /// <param name="pawn">The wielder of the weapon, or null if the stat request is for an unwielded weapon.</param>
    /// <param name="req">Stat request for a weapon or weapon def</param>
    /// <returns>Selection weight adjusted for the wielder and weapon.</returns>

    protected float AdjustedMeleeSelectionWeight(VerbUtility.VerbPropertiesWithSource vps, Pawn pawn, StatRequest req)
    {
        return req.HasThing ?
            vps.verbProps.AdjustedMeleeSelectionWeight(vps.tool, pawn, req.Thing, hediffCompSource: null, comesFromPawnNativeVerbs: false) :
            vps.verbProps.AdjustedMeleeSelectionWeight(vps.tool, pawn, req.Def as ThingDef, req.StuffDef, hediffCompSource: null, comesFromPawnNativeVerbs: false);
    }

    protected Pawn GetCurrentWielder(StatRequest req) => req.Thing as Pawn ?? (req.Thing?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
}
