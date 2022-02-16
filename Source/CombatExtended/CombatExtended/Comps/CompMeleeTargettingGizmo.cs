using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Multiplayer.API;
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

        public Thing primaryWeapon
        {
            get
            {
                return PawnParent?.equipment.Primary;
            }
        }

        public BodyPartHeight finalHeight(Pawn target)
        {
            if (PawnParent.Faction == Faction.OfPlayer && heightInt != BodyPartHeight.Undefined)
            {
                return heightInt;
            }

            float maxWeaponPen = 1f;

            if (primaryWeapon != null)
            {
                maxWeaponPen = primaryWeapon.def.tools.Select(x => x as ToolCE).Max(x => x.armorPenetrationSharp) * primaryWeapon.GetStatValue(CE_StatDefOf.MeleePenetrationFactor);
            }

            if (PawnParent.skills.GetSkill(SkillDefOf.Melee).Level < 16 && PawnParent.skills.GetSkill(SkillDefOf.Melee).Level > 7)
            {
                var torso = target.health.hediffSet.GetNotMissingParts().Where(x => x.def == BodyPartDefOf.Torso).FirstOrFallback();

                //just in case of attacking some weird creature
                if (torso != null)
                {
                    var torsoApparel = target.apparel?.WornApparel?.FindAll(x => x.def.apparel.CoversBodyPart(torso));

                    float overallRHA = target.GetStatValue(StatDefOf.ArmorRating_Sharp);

                    if (!torsoApparel.NullOrEmpty())
                    {
                        foreach (Apparel apparel in torsoApparel)
                        {
                            if (apparel != null)
                            {
                                overallRHA += apparel.GetStatValue(StatDefOf.ArmorRating_Sharp);
                            }
                        }
                    }
                    if (maxWeaponPen < overallRHA)
                    {
                        return BodyPartHeight.Top;
                    }
                }

                return BodyPartHeight.Middle;
            }

            if (PawnParent.skills.GetSkill(SkillDefOf.Melee).Level >= 16)
            {
                targetBodyPart = BodyPartDefOf.Neck;

                var neck = target.health.hediffSet.GetNotMissingParts(BodyPartHeight.Top).Where(y => y.def == BodyPartDefOf.Neck).FirstOrFallback();

                if (neck != null)
                {
                    var neckApparel = target.apparel?.WornApparel?.Find(x => x.def.apparel.CoversBodyPart(neck));

                    if (neckApparel != null && maxWeaponPen < neckApparel.GetStatValue(StatDefOf.ArmorRating_Sharp))
                    {
                        targetBodyPart = null;
                        return BodyPartHeight.Bottom;
                    }
                    else
                    {
                        targetBodyPart = neck.def;
                    }
                }

                return BodyPartHeight.Top;
            }

            return BodyPartHeight.Undefined;
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
                                if(posTarget.depth == BodyPartDepth.Outside
                                && posTarget.height == heightInt
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
                                       ChangeCurrentPart(def);
                                   }
                                   ));
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

        [SyncMethod]
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

        [SyncMethod]
        private void ChangeCurrentPart(BodyPartDef def)
        {
            targetBodyPart = def;
        }

        #endregion
    }
}
