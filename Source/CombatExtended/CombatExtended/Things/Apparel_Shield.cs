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
        private bool drawShield => Wearer.Drafted || (Wearer.CurJob?.def.alwaysShowWeapon ?? false) || (Wearer.mindState.duty?.def.alwaysShowWeapon ?? false);  // Copied from PawnRenderer.CarryWeaponOpenly(), we show the shield whenever weapons are drawn
        private ShieldDefExtension _extension;
        private ShieldDefExtension extension
        {
            get
            {
                if (_extension == null)
                {
                    _extension = def.GetModExtension<ShieldDefExtension>() ?? new ShieldDefExtension();
                }
                return _extension;
            }
        }
        private GraphicData _graphicData;
        private GraphicData graphicData
        {
            get
            {
                if (_graphicData == null)
                {
                    _graphicData = extension.equippedGraphicData ?? def.graphicData;
                }
                return _graphicData;
            }
        }
        private bool IsTall => extension.drawAsTall;


        public override bool AllowVerbCast(Verb verb)
        {
            ThingWithComps primary = Wearer.equipment?.Primary;
            return primary == null || (primary.def.weaponTags?.Contains(OneHandedTag) ?? false);
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

            Rot4 rot = Wearer.Rotation;
            Graphic graphic = graphicData.Graphic;

            Vector3 vector = Wearer.Drawer.DrawPos + GetFacingVector(rot);
            vector.y = Wearer.Rotation == Rot4.West || Wearer.Rotation == Rot4.South ? AltitudeLayer.PawnUnused.AltitudeFor() : AltitudeLayer.Pawn.AltitudeFor();


            Material mat = GetFacingMaterial(graphic, rot);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.AngleAxis(GetDrawAngle(rot), Vector3.up), Vector3.one);
            Graphics.DrawMesh(graphicData.Graphic.MeshAt(Wearer.Rotation), matrix, mat, 0);
        }

        public Vector3 GetFacingVector(Rot4 rot)
        {
            if (rot == Rot4.North)
            {
                return extension.northDrawOffset + GetTallOffsetVector(rot);
            }
            else if (rot == Rot4.East)
            {
                return extension.eastDrawOffset + GetTallOffsetVector(rot);
            }
            else if (rot == Rot4.West)
            {
                return extension.westDrawOffset + GetTallOffsetVector(rot);
            }
            else //South as default
            {
                return extension.southDrawOffset + GetTallOffsetVector(rot);
            }
        }

        public Material GetFacingMaterial(Graphic graphic, Rot4 rot)
        {
            var coloredGraphic = graphic.GetColoredVersion(ShaderDatabase.CutoutComplex, DrawColor, DrawColorTwo);

            if (rot == Rot4.North)
            {
                return coloredGraphic.MatNorth;
            }
            else if (rot == Rot4.East)
            {
                return coloredGraphic.MatEast;
            }
            else if (rot == Rot4.West)
            {
                return coloredGraphic.MatWest;
            }
            else //South as default
            {
                return coloredGraphic.MatSouth;
            }

        }

        //This exists to maintain backwards compatibility for existing shields without custom offsets. New shields would be better served defining their own offsets. 
        public Vector3 GetTallOffsetVector(Rot4 rot)
        {
            if (!IsTall)
            {
                return Vector3.zero;
            }

            if (rot == Rot4.North)
            {
                return new Vector3(0, 0, 0.3f);
            }
            else //South, East, West use same offset
            {
                return new Vector3(0, 0, 0.25f);
            }
        }

        //Using a method call here will make it easier to open to xml in the future if needed
        public float GetDrawAngle(Rot4 rot)
        {
            if (rot == Rot4.East)
            {
                return 22.5f;
            }
            else if (rot == Rot4.West)
            {
                return 337.5f;
            }
            else
            {
                return 0;
            }
        }
    }
}
