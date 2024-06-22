using CombatExtended.AI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_CEFlee : JobGiver_FleeForDistance
    {
        public JobGiver_CEFlee() : base() { }
        public bool meleeOnly = false;
        public List<string> weaponTags;
        public List<string> weaponTagsOfDanger;
        public bool doNotFleeIfCantPenetrate = true;
        public bool doNotFleeIfFaster = true;
        public bool mechs = true;
        public bool humanlike = true;
        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.RaceProps.IsMechanoid ^ mechs && pawn.RaceProps.Humanlike ^ humanlike)
            {
                return null;
            }
            var equipment = pawn.equipment.PrimaryEq;
            if (equipment?.PrimaryVerb.IsMeleeAttack ?? true)
            {
                return null; // Melee fighters do not flee
            }
            if (!weaponTags.NullOrEmpty() && !equipment.parent.def.weaponTags.Any(x => weaponTags.Contains(x)))
            {
                return null; //If weaponsTags specified, check if pawn equipment has one of weapon tags
            }
            if (equipment.PrimaryVerb.EffectiveRange < this.fleeDistRange.min)
            {
                return null;
            }
            if (GenAI.EnemyIsNear(pawn, this.enemyDistToFleeRange.min, out _, meleeOnly, true))
            {
                return null;
            }
            foreach (var danger in AI_Utility.EnemiesNear(pawn, this.enemyDistToFleeRange.max, meleeOnly, true))
            {
                if (danger is Pawn dangerPawn)
                {
                    var dangerWeapon = dangerPawn.equipment.PrimaryEq;
                    if (!weaponTagsOfDanger.NullOrEmpty() && (!dangerWeapon?.parent.def.weaponTags.Any(x => weaponTagsOfDanger.Contains(x)) ?? true))
                    {
                        continue; //If weaponTagsOfDanger specified, check if dangerous pawn equipment has one of weapon tags
                    }
                    if (doNotFleeIfCantPenetrate)
                    {
                        float sharpOfPawn = ArmorUtilityCE.AverageArmor(pawn, StatDefOf.ArmorRating_Sharp), bluntOfPawn = ArmorUtilityCE.AverageArmor(pawn, StatDefOf.ArmorRating_Blunt);
                        float sharpOfAttacker = 0, bluntOfAttacker = 0;
                        if (dangerWeapon == null || dangerWeapon.PrimaryVerb.IsMeleeAttack || !dangerWeapon.parent.TryGetMaxPenetration(out sharpOfAttacker, out bluntOfAttacker))
                        {
                            var verbs = dangerPawn.meleeVerbs.GetUpdatedAvailableVerbsList(false).Select(x => x.verb).OfType<Verb_MeleeAttackCE>().ToArray();
                            if (verbs.Length > 0)
                            {
                                sharpOfAttacker = verbs.Max(x => x.ArmorPenetrationSharp);
                                bluntOfAttacker = verbs.Max(x => x.ArmorPenetrationBlunt);
                            }
                        }

                        if (sharpOfAttacker < sharpOfPawn && bluntOfAttacker < bluntOfPawn)
                        {
                            continue;
                        }
                    }

                    if (doNotFleeIfFaster && dangerPawn.GetMoveSpeed() > pawn.GetMoveSpeed() * 1.3f)
                    {
                        return null; // Do not run if ANY of dangers faster than you
                    }
                    return FleeUtility.FleeJob(pawn, danger, Mathf.CeilToInt(new FloatRange(fleeDistRange.min, Mathf.Max(equipment.PrimaryVerb.EffectiveRange, fleeDistRange.max)).RandomInRange));
                }
            }
            return null;
        }
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var copy = (JobGiver_CEFlee)base.DeepCopy(resolve);
            copy.meleeOnly = meleeOnly;
            copy.weaponTags = weaponTags?.ToList();
            copy.weaponTagsOfDanger = weaponTagsOfDanger?.ToList();
            copy.doNotFleeIfCantPenetrate = doNotFleeIfCantPenetrate;
            copy.doNotFleeIfFaster = doNotFleeIfFaster;
            copy.mechs = mechs;
            copy.humanlike = humanlike;
            return copy;
        }
    }
}
