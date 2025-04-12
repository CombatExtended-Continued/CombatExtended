using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatPart_NonHumanParry : StatPart
    {
        public StatPart_NonHumanParry()
        {
        }

        public List<BodyPartGroupDef> bodyPartGroupList = [];
        public List<BodyPartRecord> notMissingPartList = [];
        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing || req.Thing is not Pawn pawn || pawn.RaceProps.Humanlike || pawn?.inventory?.innerContainer.Count > 0)
            {
                return;
            }
            var toolList = pawn.def.tools;
            if (toolList.Count == 0)
            {
                return;
            }
            bodyPartGroupList.Clear();
            for (int i = 0; i < toolList.Count; i++)
            {

                var bodyPart = toolList[i].linkedBodyPartsGroup;
                if (bodyPart != null && bodyPart != CE_BodyPartGroupDefOf.HeadAttackTool)
                {
                    if (!bodyPartGroupList.Contains(bodyPart))
                    {
                        bodyPartGroupList.Add(bodyPart);
                    }
                }
            }
            notMissingPartList.Clear();
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
            {
                notMissingPartList.Add(part);
            }
            int matchCount = 0;
            for (int i = 0; i < notMissingPartList.Count; i++)
            {
                for (int j = 0; j < bodyPartGroupList.Count; j++)
                {
                    if (notMissingPartList[i].groups.Contains(bodyPartGroupList[j]))
                    {
                        matchCount++;
                        break;
                    }
                }
            }
            val *= (1 + matchCount);
        }
    }
}
