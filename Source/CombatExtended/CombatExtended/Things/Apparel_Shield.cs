using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_Shield : Apparel
    {
        // From PawnRenderer
        private const float YOffsetBehind = 0.00390625f;
        private const float YOffsetPostHead = 0.03515625f;
        private const float YOffsetPrimaryEquipmentUnder = 0f;
        private const float YOffsetPrimaryEquipmentOver = 0.0390625f;
        private const float YOffsetIntervalClothes = 0.00390625f;
        private const float YOffsetStatus = 0.04296875f;

        public const string OneHandedTag = "CE_OneHandedWeapon";
        private bool drawShield => this.def.apparel.renderNodeProperties.NullOrEmpty() && (Wearer.Drafted || (Wearer.CurJob?.def.alwaysShowWeapon ?? false) || (Wearer.mindState.duty?.def.alwaysShowWeapon ?? false));  // Copied from PawnRenderer.CarryWeaponOpenly(), we show the shield whenever weapons are drawn
        private bool IsTall => def.GetModExtension<ShieldDefExtension>()?.drawAsTall ?? false;

        public override void PostMake()
        {
            base.PostMake();
            if (this.def.apparel.renderNodeProperties.NullOrEmpty())
            {
                Log.WarningOnce($"{def.defName} using obsolete render system", def.defName.GetHashCodeSafe());
            }
        }

        public override bool AllowVerbCast(Verb verb)
        {
            ThingWithComps primary = Wearer.equipment?.Primary;
            return primary == null || (primary.def.weaponTags?.Contains(OneHandedTag) ?? false);
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry statDrawEntry in base.SpecialDisplayStats())
            {
                yield return statDrawEntry;
            }
            RoyalTitleDef royalTitleDef = (from t in DefDatabase<FactionDef>.AllDefsListForReading.SelectMany((FactionDef f) => f.RoyalTitlesAwardableInSeniorityOrderForReading)
                                           where t.requiredApparel != null && t.requiredApparel.Any((ApparelRequirement req) => req.ApparelMeetsRequirement(this.def, false))
                                           orderby t.seniority descending
                                           select t).FirstOrDefault<RoyalTitleDef>();
            if (royalTitleDef != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_Apparel_MaxSatisfiedTitle".Translate(), royalTitleDef.GetLabelCapForBothGenders(), "Stat_Thing_Apparel_MaxSatisfiedTitle_Desc".Translate(), 2752, null, new Dialog_InfoCard.Hyperlink[]
                {
                    new Dialog_InfoCard.Hyperlink(royalTitleDef, -1)
                }, false, false);
            }
            var shieldCoverage = this.def.GetModExtension<ShieldDefExtension>()?.shieldCoverage;
            if (shieldCoverage != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "CE_Shield_Coverage".Translate(), ShieldDefExtension.GetShieldProtectedAreas(BodyDefOf.Human, this.def), "CE_Shield_Coverage_Desc".Translate(), 800, null);
            }
            yield break;
        }
        public override void DrawWornExtras()
        {
            if (Wearer == null || !Wearer.Spawned)
            {
                return;
            }
            if (!drawShield)
            {
                return;
            }

            float num = 0f;
            Vector3 vector = this.Wearer.Drawer.DrawPos;
            vector.y = Wearer.Rotation == Rot4.West || Wearer.Rotation == Rot4.South ? AltitudeLayer.PawnUnused.AltitudeFor() : AltitudeLayer.Pawn.AltitudeFor();

            Vector3 s = new Vector3(1f, 1f, 1f);
            if (this.Wearer.Rotation == Rot4.North)
            {
                vector.x -= 0.1f;
                vector.z -= IsTall ? -0.1f : 0.2f;
            }
            else
            {
                if (this.Wearer.Rotation == Rot4.South)
                {
                    vector.x += 0.1f;
                    vector.z -= IsTall ? -0.05f : 0.2f;
                }
                else
                {
                    if (this.Wearer.Rotation == Rot4.East)
                    {
                        if (IsTall)
                        {
                            vector.x += 0.1f;
                        }
                        vector.z -= IsTall ? -0.05f : 0.2f;
                        num = 22.5f;
                    }
                    else
                    {
                        if (this.Wearer.Rotation == Rot4.West)
                        {
                            if (IsTall)
                            {
                                vector.x -= 0.1f;
                            }
                            vector.z -= IsTall ? -0.05f : 0.2f;
                            num = 337.5f;
                        }
                    }
                }
            }
            Material mat = Graphic.GetColoredVersion(ShaderDatabase.CutoutComplex, DrawColor, DrawColorTwo).MatSingle;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(num, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);

        }
    }
}
