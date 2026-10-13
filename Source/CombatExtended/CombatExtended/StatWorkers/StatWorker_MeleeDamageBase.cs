using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_MeleeDamageBase : StatWorker_MeleeStats
{
    #region Constants

    public const float damageVariationMin = 0.5f;
    public const float damageVariationMax = 1.5f;
    public const float damageVariationPerSkillLevel = 0.025f;

    #endregion

    #region Methods

    public static float GetDamageVariationMin(Pawn pawn)
    {
        float unskilledReturnValue = damageVariationMin;
        if (!ShouldUseSkillVariation(pawn, ref unskilledReturnValue))
        {
            return unskilledReturnValue;
        }
        return damageVariationMin + (damageVariationPerSkillLevel * pawn.skills.GetSkill(SkillDefOf.Melee).Level);
    }

    public static float GetDamageVariationMax(Pawn pawn)
    {
        float unskilledReturnValue = damageVariationMax;
        if (!ShouldUseSkillVariation(pawn, ref unskilledReturnValue))
        {
            return unskilledReturnValue;
        }
        return damageVariationMax - (damageVariationPerSkillLevel * (20 - pawn.skills.GetSkill(SkillDefOf.Melee).Level));
    }

    public static bool ShouldUseSkillVariation(Pawn pawn, ref float unskilledReturnValue)
    {
        if (pawn == null)       //Info windows for when weapon isn't equipped
        {
            return false;
        }
        if ((pawn?.skills?.GetSkill(SkillDefOf.Melee) ?? null) == null)     //Pawns that can equip weapons but don't use skill (mechanoids, custom races)
        {
            //No damage variation applied (same as animals for unarmed damage)
            unskilledReturnValue = 1.0f;
            return false;
        }
        return true;
    }


    /// <summary>
    /// Get the melee damaged dealt by a single VerbProperties + Tool combination of a weapon,
    /// adjusted according to the wielder's (if any) stats/capacities and multipliers from the weapon's stuff.
    /// </summary>
    /// <param name="vps">Verb data (VerbProperties + Tool + Maneuver)</param>
    /// <param name="pawn">The wielder of the weapon, or null if the stat request is for an unwielded weapon.</param>
    /// <param name="req">Stat request for a weapon or weapon def</param>
    /// <returns>Melee damage adjusted for the wielder and weapon.</returns>
    protected float AdjustedMeleeDamageAmount(VerbUtility.VerbPropertiesWithSource vps, Pawn pawn, StatRequest req)
    {
        return req.HasThing ?
            vps.verbProps.AdjustedMeleeDamageAmount(vps.tool, pawn, req.Thing, hediffCompSource: null) :
            vps.verbProps.AdjustedMeleeDamageAmount(vps.tool, pawn, req.Def as ThingDef, req.StuffDef, hediffCompSource: null);
    }

    /// <summary>
    /// Get the cooldown (in seconds) a single VerbProperties + Tool combination of a weapon,
    /// adjusted according to the wielder's (if any) stats/capacities and multipliers from the weapon's stuff.
    /// </summary>
    /// <param name="vps">Verb data (VerbProperties + Tool + Maneuver)</param>
    /// <param name="pawn">The wielder of the weapon, or null if the stat request is for an unwielded weapon.</param>
    /// <param name="req">Stat request for a weapon or weapon def</param>
    /// <returns>Cooldown time in seconds, adjusted for the wielder and weapon.</returns>
    protected float AdjustedCooldown(VerbUtility.VerbPropertiesWithSource vps, Pawn pawn, StatRequest req)
    {
        return req.HasThing ?
            vps.verbProps.AdjustedCooldown(vps.tool, pawn, req.Thing) :
            vps.verbProps.AdjustedCooldown(vps.tool, pawn, req.Def as ThingDef, req.StuffDef);
    }

    #endregion

}
