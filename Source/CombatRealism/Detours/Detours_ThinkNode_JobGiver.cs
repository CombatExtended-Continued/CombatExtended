using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public abstract class Detours_ThinkNode_JobGiver : ThinkNode
    {
        protected abstract Job TryGiveJob(Pawn pawn);

        public static readonly String[] robotBodyList = { "AIRobot", "HumanoidTerminator" };


        public override ThinkResult TryIssueJobPackage(Pawn pawn)
        {
            Job job = this.TryGiveJob(pawn);
            ThinkResult result;
            if (job == null)
            {
                result = ThinkResult.NoJob;
            }
            else
            {
                if (pawn.Faction == Faction.OfPlayer && !robotBodyList.Contains(pawn.def.race.body.defName))
                {
                    if (job.def == JobDefOf.CutPlant || job.def == JobDefOf.Harvest)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.PlantWorkSpeed);
                    }
                    else
                    {
                        if (job.def == JobDefOf.Mine)
                        {
                            RightTools.EquipRigthTool(pawn, StatDefOf.MiningSpeed);
                        }
                        else
                        {
                            if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct)
                            {
                                RightTools.EquipRigthTool(pawn, StatDefOf.ConstructionSpeed);
                            }
                            else
                            {
                                if (job.def == JobDefOf.DoBill && job.bill.recipe.workSkill == SkillDefOf.Cooking)
                                {
                                    RightTools.EquipRigthTool(pawn, StatDef.Named("CookSpeed"));
                                }
                                else
                                {
                                    if (job.def == JobDefOf.TendPatient)
                                    {
                                        RightTools.EquipRigthTool(pawn, StatDef.Named("HealingQuality"));
                                    }
                                }
                            }
                        }
                    }
                }
                result = new ThinkResult(job, this);
            }
            return result;
        }
    }
}
