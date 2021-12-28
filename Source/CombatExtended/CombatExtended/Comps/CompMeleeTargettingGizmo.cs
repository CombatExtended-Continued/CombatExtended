using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class MeleeTargettingAdd
    {
        static MeleeTargettingAdd()
        {
            foreach(ThingDef humanlike in DefDatabase<ThingDef>.AllDefs.Where(y => y.race != null && y.race.Humanlike))
            {
                if (humanlike.comps == null)
                {
                    humanlike.comps = new List<CompProperties>();
                }

                humanlike.comps.Add(new CompProperties { compClass = typeof(CompMeleeTargettingGizmo) });
            }
        }
    }
    public class CompMeleeTargettingGizmo : ThingComp
    {
        #region Fields
        public Pawn PawnParent => (Pawn)this.parent;

        public BodyPartHeight heightInt = BodyPartHeight.Undefined;

        public BodyPartDef targetBodyPart = BodyPartDefOf.Torso;

        #endregion

        #region Properties

        public BodyPartHeight finalHeight
        {
            get
            {
                if (PawnParent.Faction == Faction.OfPlayer)
                {
                    return heightInt;
                }

                if (PawnParent.skills.GetSkill(SkillDefOf.Melee).Level < 16)
                {
                    return BodyPartHeight.Middle;
                }

                if (PawnParent.skills.GetSkill(SkillDefOf.Melee).Level > 16)
                {
                    targetBodyPart = BodyPartDefOf.Neck;
                    return BodyPartHeight.Top;
                }

                return BodyPartHeight.Undefined;
            }
        }



        public bool SkillReqA
        {
            get
            {
                return PawnParent.skills.GetSkill(SkillDefOf.Melee).Level >= 5;
            }
        }
        public bool SkillReqBP
        {
            get
            {
                return PawnParent.skills.GetSkill(SkillDefOf.Melee).Level > 15;
            }
        }

        public float SkillBodyPartAttackChance
        {
            get
            {
                var result = 0.10f;

                result *= ((PawnParent.skills.GetSkill(SkillDefOf.Melee).Level * 1f) - 15f);

                return result;
            }
        }
        #endregion


        #region Methods
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (SkillReqA)
            {
                yield return new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/TargettingMelee/" + finalHeight.ToString()),
                    defaultLabel = "Melee target height: " + finalHeight.ToString(),
                    action = delegate 
                    {
                        switch (heightInt)
                        {
                            case BodyPartHeight.Bottom:
                                heightInt = BodyPartHeight.Middle;
                                break;
                            case BodyPartHeight.Middle:
                                heightInt = BodyPartHeight.Top;
                                break;
                            case BodyPartHeight.Top:
                                heightInt = BodyPartHeight.Undefined;
                                break;
                            case BodyPartHeight.Undefined:
                                heightInt = BodyPartHeight.Bottom;
                                break;
                        }
                    }
                };
            }
            if (SkillReqBP)
            {
                yield return new Command_Action
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/TargettingMelee/Undefined"),
                    defaultLabel = "Targeted bodypart " + targetBodyPart,
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        List<BodyPartDef> parts = new List<BodyPartDef>();
                        foreach (BodyPartRecord posTarget in BodyDefOf.Human.AllParts)
                        {
                            if (!parts.Contains(posTarget.def))
                            {
                                if(posTarget.depth == BodyPartDepth.Outside
                                && posTarget.height == finalHeight
                                && !posTarget.def.label.Contains("toe")
                                && !posTarget.def.label.Contains("finger")
                                && !posTarget.def.label.Contains("utility")
                                )
                                {
                                    parts.Add(posTarget.def);
                                }
                            }

                            

                           
                        }

                        foreach (BodyPartDef def in parts)
                        {
                            options.Add(new FloatMenuOption(def.label,
                                   delegate
                                   {
                                       targetBodyPart = def;
                                   }
                                   ));
                        }

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

        #endregion
    }
}
