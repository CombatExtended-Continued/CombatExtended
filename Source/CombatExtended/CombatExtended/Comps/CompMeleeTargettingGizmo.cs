using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended;

[StaticConstructorOnStartup]
public class MeleeTargettingAdd
{
    static MeleeTargettingAdd()
    {
        foreach (ThingDef humanlike in DefDatabase<ThingDef>.AllDefs.Where(y => y.race != null && y.race.Humanlike))
        {
            if (humanlike.comps == null)
            {
                humanlike.comps = new List<CompProperties>();
            }

            humanlike.comps.Add(new CompProperties { compClass = typeof(CompMeleeTargettingGizmo) });
        }
    }
}

[StaticConstructorOnStartup]
public class CompMeleeTargettingGizmo : ThingComp
{
    static CompMeleeTargettingGizmo()
    {
        priorityList = new List<BodyPartDef>() {
            CE_BodyPartDefOf.Neck,
            BodyPartDefOf.Eye,
            BodyPartDefOf.Head
        };
    }
    private static List<BodyPartDef> priorityList = null;
    #region Fields
    public Pawn PawnParent => (Pawn)this.parent;

    public BodyPartHeight heightInt = BodyPartHeight.Undefined;

    public BodyPartDef targetBodyPart = BodyPartDefOf.Torso;

    #endregion

    #region Properties

    public Thing primaryWeapon
    {
        get
        {
            return PawnParent?.equipment.Primary;
        }
    }

    private List<Tool> PawnTools
    {
        get
        {
            return PawnParent.Tools;
        }
    }

    public BodyPartHeight finalHeight(Pawn target, bool hitChanceIsGuaranteed = false)
    {
        if (PawnParent.Faction == Faction.OfPlayer && heightInt != BodyPartHeight.Undefined)
        {
            return heightInt;
        }

        float maxWeaponPen = 0f;
        float maxWeaponPenBlunt = 0f;

        if (!primaryWeapon?.def.tools.NullOrEmpty() ?? false)
        {
            float maxPen = 0f;
            float maxPenBlunt = 0f;
            foreach (Tool tool in primaryWeapon.def.tools)
            {
                if (tool is ToolCE toolCe)
                {
                    maxPen = Mathf.Max(maxPen, toolCe.armorPenetrationSharp);
                    maxPenBlunt = Mathf.Max(maxPenBlunt, toolCe.armorPenetrationBlunt);
                }
            }

            float penetrationFactor = primaryWeapon.GetStatValue(CE_StatDefOf.MeleePenetrationFactor);
            maxWeaponPen = maxPen * penetrationFactor;
            maxWeaponPenBlunt = maxPenBlunt * penetrationFactor;
        }
        if (!PawnTools.NullOrEmpty())
        {
            foreach (Tool tool in PawnTools)
            {
                if (tool is ToolCE toolCe)
                {
                    maxWeaponPen = Mathf.Max(maxWeaponPen, toolCe.armorPenetrationSharp);
                    maxWeaponPenBlunt = Mathf.Max(maxWeaponPenBlunt, toolCe.armorPenetrationBlunt);
                }
            }
        }

        int skillLevel = PawnParent.skills.GetSkill(SkillDefOf.Melee).Level;
        if (skillLevel < 8)
        {
            return BodyPartHeight.Undefined;
        }

        // Aim at the area where the most damage gets through the armor, using whichever attack of the weapon does best (sharp or blunt).
        // Areas within ArmorTieMargin of the best one count as equally armored, the order of the list then decides: head, legs, torso.
        BodyPartHeight[] areas = skillLevel >= 16 ? AutoAimAreasWithBottom : AutoAimAreas;
        var candidates = new List<(BodyPartHeight height, float fraction)>();
        foreach (BodyPartHeight area in areas)
        {
            if (!hitChanceIsGuaranteed && !AimedHitChanceIsEnough(target, area))
            {
                continue;
            }
            float? fraction = DamageFractionThroughArmor(target, area, maxWeaponPen, maxWeaponPenBlunt);
            if (fraction.HasValue)
            {
                candidates.Add((area, fraction.Value));
            }
        }
        if (candidates.Count == 0)
        {
            targetBodyPart = null;
            return BodyPartHeight.Undefined;
        }
        float bestFraction = candidates.Max(c => c.fraction);
        BodyPartHeight chosenHeight = candidates.First(c => c.fraction >= bestFraction - ArmorTieMargin).height;

        if (skillLevel >= 16)
        {
            // Specific part targeting: the least armored part of the area
            targetBodyPart = LeastArmoredPart(target, chosenHeight, maxWeaponPen, maxWeaponPenBlunt)?.def;
        }
        return chosenHeight;
    }

    // Order of the areas for automatic aiming, which also decides ties. Legs are only an option together with specific part targeting.
    private static readonly BodyPartHeight[] AutoAimAreas = { BodyPartHeight.Top, BodyPartHeight.Middle };
    private static readonly BodyPartHeight[] AutoAimAreasWithBottom = { BodyPartHeight.Top, BodyPartHeight.Bottom, BodyPartHeight.Middle };

    // Aiming at the top or bottom lowers the hit chance. Pawns whose final hit chance there (called shot penalty, gear, injuries and so on together)
    // would be below this attack without aiming at it instead, which is the hit chance of a pawn with no net melee skill.
    private const float MinAutoAimHitChance = 0.6f;

    private bool AimedHitChanceIsEnough(Pawn target, BodyPartHeight area)
    {
        // The middle has no penalty
        return area == BodyPartHeight.Middle || Verb_MeleeAttackCE.GetMeleeHitChance(PawnParent, target, area) >= MinAutoAimHitChance;
    }

    // Areas and parts that let through damage within this fraction of each other count as equally armored
    private const float ArmorTieMargin = 0.05f;

    /// <summary>
    /// Parts that can be aimed at, the same ones the specific part picker offers.
    /// </summary>
    private static bool IsTargetablePart(BodyPartRecord part)
    {
        return part.depth == BodyPartDepth.Outside
               && !part.def.label.Contains("toe")
               && !part.def.label.Contains("finger")
               && !part.def.label.Contains("utility");
    }

    /// <summary>
    /// The part of an area that lets through the most damage, ties go to the order of the priority list and then to the part that is easiest to hit.
    /// </summary>
    private static BodyPartRecord LeastArmoredPart(Pawn target, BodyPartHeight height, float sharpPen, float bluntPen)
    {
        var candidates = target.health.hediffSet.GetNotMissingParts(height, BodyPartDepth.Outside)
                         .Where(part => part.coverageAbs > 0f && IsTargetablePart(part))
                         .Select(part => (part, fraction: DamageFractionThroughArmor(target, part, sharpPen, bluntPen)))
                         .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }
        float bestFraction = candidates.Max(c => c.fraction);
        return candidates.Where(c => c.fraction >= bestFraction - ArmorTieMargin)
               .OrderBy(c => PartPriority(c.part.def))
               .ThenByDescending(c => c.part.coverageAbs)
               .First().part;
    }

    private static int PartPriority(BodyPartDef def)
    {
        int index = priorityList.IndexOf(def);
        return index < 0 ? int.MaxValue : index;
    }

    /// <summary>
    /// Roughly the fraction of the damage that gets through the armor of a body part, using the weapon's best attack (CE reduces damage by (penetration - armor) / penetration).
    /// </summary>
    private static float DamageFractionThroughArmor(Pawn target, BodyPartRecord part, float sharpPen, float bluntPen)
    {
        float sharpArmor = target.PartialStat(StatDefOf.ArmorRating_Sharp, part);
        float bluntArmor = target.PartialStat(StatDefOf.ArmorRating_Blunt, part);
        if (target.apparel != null)
        {
            foreach (Apparel apparel in target.apparel.WornApparel)
            {
                sharpArmor += apparel.PartialStat(StatDefOf.ArmorRating_Sharp, part);
                bluntArmor += apparel.PartialStat(StatDefOf.ArmorRating_Blunt, part);
            }
        }
        return Mathf.Max(DamageFractionThroughArmor(sharpPen, sharpArmor), DamageFractionThroughArmor(bluntPen, bluntArmor));
    }

    /// <summary>
    /// Same as above, averaged over the parts of an area by how likely they are to be hit. Null if nothing can be aimed at in the area.
    /// </summary>
    private static float? DamageFractionThroughArmor(Pawn target, BodyPartHeight height, float sharpPen, float bluntPen)
    {
        float total = 0f;
        float weight = 0f;
        foreach (BodyPartRecord part in target.health.hediffSet.GetNotMissingParts(height, BodyPartDepth.Outside))
        {
            if (part.coverageAbs > 0f && IsTargetablePart(part))
            {
                total += part.coverageAbs * DamageFractionThroughArmor(target, part, sharpPen, bluntPen);
                weight += part.coverageAbs;
            }
        }
        return weight > 0f ? total / weight : null;
    }

    private static float DamageFractionThroughArmor(float penetration, float armor)
    {
        if (armor <= 0f)
        {
            return 1f;
        }
        return penetration <= armor ? 0f : (penetration - armor) / penetration;
    }

    public bool SkillReqA
    {
        get
        {
            return PawnParent.skills.GetSkill(SkillDefOf.Melee).Level >= 8;
        }
    }
    public bool SkillReqBP
    {
        get
        {
            return PawnParent.skills.GetSkill(SkillDefOf.Melee).Level > 15;
        }
    }

    public float SkillBodyPartAttackChance(BodyPartRecord PartToHit)
    {
        if (PartToHit == null)
        {
            return 0f;
        }

        var result = PartToHit.coverage;

        result *= ((PawnParent.skills.GetSkill(SkillDefOf.Melee).Level) - 15f) * PawnParent.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);

        return result;
    }

    private string heightString
    {
        get
        {
            var result = heightInt.ToString();

            if (result.ToLower() == "undefined")
            {
                result = "Automatic";
            }

            return result;
        }
    }

    #endregion


    #region Methods
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        // Don't let people control melee targeting for non-colonist pawns or colonists in a mental state
        // Corpses are also auto-assigned this comp, but clearly they are not typeof(Pawn)
        if (parent is Corpse || !PawnParent.IsColonist || PawnParent.InAggroMentalState)
        {
            yield break;
        }

        if (SkillReqA)
        {
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/TargettingMelee/" + heightInt.ToString()),
                action = ChangeCurrentHeight,
                defaultLabel = "CE_MeleeTargetting_CurHeight".Translate() + " " + heightString,
            };
        }
        if (SkillReqBP && heightInt != BodyPartHeight.Undefined)
        {
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/TargettingMelee/Undefined"),
                defaultLabel = "CE_MeleeTargetting_CurPart".Translate() + " " + (targetBodyPart?.label ?? "CE_NoBP".Translate()),
                action = delegate
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    List<BodyPartDef> parts = new List<BodyPartDef>();
                    foreach (BodyPartRecord posTarget in BodyDefOf.Human.AllParts)
                    {
                        if (!parts.Contains(posTarget.def))
                        {
                            if (IsTargetablePart(posTarget) && posTarget.height == heightInt)
                            {
                                parts.Add(posTarget.def);
                            }
                        }




                    }

                    foreach (BodyPartDef def in parts)
                    {
                        options.Add(new FloatMenuOption(def.label, delegate { ChangeCurrentPart(def); }));
                    }

                    options.Add(new FloatMenuOption("CE_NoBP".Translate(), delegate { ChangeCurrentPart(null); }));

                    Find.WindowStack.Add(new FloatMenu(options));
                }

            };
        }
    }

    public override void PostExposeData()
    {
        Scribe_Defs.Look(ref targetBodyPart, "TargetBodyPart");

        Scribe_Values.Look(ref heightInt, "heightInt");
    }

    [Compatibility.Multiplayer.SyncMethod]
    private void ChangeCurrentHeight()
    {
        switch (heightInt)
        {
            case BodyPartHeight.Bottom:
                heightInt = BodyPartHeight.Middle;
                targetBodyPart = null;
                break;
            case BodyPartHeight.Middle:
                heightInt = BodyPartHeight.Top;
                targetBodyPart = null;
                break;
            case BodyPartHeight.Top:
                heightInt = BodyPartHeight.Undefined;
                targetBodyPart = null;
                break;
            case BodyPartHeight.Undefined:
                heightInt = BodyPartHeight.Bottom;
                targetBodyPart = null;
                break;
        }
    }

    [Compatibility.Multiplayer.SyncMethod]
    private void ChangeCurrentPart(BodyPartDef def)
    {
        targetBodyPart = def;
    }

    #endregion
}
