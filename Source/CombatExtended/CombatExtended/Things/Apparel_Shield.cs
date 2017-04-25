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
        private const string OneHandedTag = "CE_OneHandedWeapon";
        private bool drawShield => wearer.Drafted || (wearer.CurJob?.def.alwaysShowWeapon ?? false) || (wearer.mindState.duty?.def.alwaysShowWeapon ?? false);  // Copied from PawnRenderer.CarryWeaponOpenly(), we show the shield whenever weapons are drawn
        private bool isTall => def.apparel.tags.Contains(ArmorUtilityCE.BallisticShieldTag);

        public override bool AllowVerbCast(IntVec3 root, TargetInfo targ)
        {
            ThingWithComps primary = wearer.equipment?.Primary;
            return primary == null || (primary.def.weaponTags?.Contains(OneHandedTag) ?? false);
        }

        public override void DrawWornExtras()
        {
            if (wearer == null || !wearer.Spawned) return;
            if (!drawShield) return;

            float num = 0f;
            Vector3 vector = this.wearer.Drawer.DrawPos;
            vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
            Vector3 s = new Vector3(1f, 1f, 1f);
            if (this.wearer.Rotation == Rot4.North)
            {
                //vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                vector.x -= 0.1f;
                vector.z -= isTall ? -0.1f : 0.2f;
            }
            else
            {
                if (this.wearer.Rotation == Rot4.South)
                {
                    //vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                    vector.y += 0.0375f;
                    vector.x += 0.1f;
                    vector.z -= isTall ? -0.05f : 0.2f;
                }
                else
                {
                    if (this.wearer.Rotation == Rot4.East)
                    {
                        //vector.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                        if (isTall) vector.x += 0.1f;
                        vector.z -= isTall ? -0.05f : 0.2f;
                        num = 22.5f;
                    }
                    else
                    {
                        if (this.wearer.Rotation == Rot4.West)
                        {
                            //vector.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                            vector.y += 0.0425f;
                            if (isTall) vector.x -= 0.1f;
                            vector.z -= isTall ? -0.05f : 0.2f;
                            num = 337.5f;
                        }
                    }
                }
            }
            Material mat = Graphic.GetColoredVersion(ShaderDatabase.Cutout, DrawColor, DrawColorTwo).MatSingle;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(num, Vector3.up), s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}
