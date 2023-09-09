using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class DutyDefPatcher
    {
        static DutyDefPatcher()
        {
            if (Controller.settings.AIEffectiveRange)
            {
                var node = DutyDefOf.AssaultColony.thinkNode.subNodes.Find(x => x is JobGiver_AIFightEnemies);
                DutyDefOf.AssaultColony.thinkNode.subNodes.Replace(node, new JobGiver_FightEnemies_EffectiveRange());
            }
        }
    }

    public class JobGiver_FightEnemies_EffectiveRange : JobGiver_AIFightEnemies
    {
        public override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
        {
            // copy of vanilla
            Thing enemyTarget = pawn.mindState.enemyTarget;
            bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, allowManualCastWeapons, allowTurrets);
            if (verb == null)
            {
                dest = IntVec3.Invalid;
                return false;
            }
            CastPositionRequest newReq = default(CastPositionRequest);
            newReq.caster = pawn;
            newReq.target = enemyTarget;
            newReq.verb = verb;
            float AIRange = verb.verbProps.range;
            //changes range based on weapon weapontags
            #region getting appropiate value
            var tags = verb.EquipmentSource.def.weaponTags;
            if ((tags.Contains("CE_AI_AR") && tags.Any(x => !x.ToLower().Contains("bipod"))))
            {
                AIRange = Mathf.Floor((AIRange * 0.7f));
            }
            else if (tags.Contains("CE_AI_LMG"))
            {
                AIRange = Mathf.Floor((AIRange * 0.85f));
            }
            #endregion
            newReq.maxRangeFromTarget = AIRange;
            newReq.wantCoverFromTarget = verb.verbProps.range > 5f;
            return CastPositionFinder.TryFindCastPosition(newReq, out dest);
        }
    }
}
