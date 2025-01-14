using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompFireArc : ThingComp
    {
        public CompProperties_FireArc Props => props as CompProperties_FireArc;

        public float CenterAngle => Editing ? NewCenterAngle : CurrentCenterAngle;

        public float Span => Editing ? NewSpan : CurrentSpan;

        public float LineLength => Editing ? EditingLineLength : Props.lineLength;


        float NewCenterAngle;
        float NewSpan;
        float EditingLineLength;
        float EditingSpanLineLength;

        bool Editing = false;
        bool EditingSpan = false;

        float cachedDiagonal;

        float DiagonalLength
        {
            get
            {
                if (cachedDiagonal == 0)
                {
                    cachedDiagonal = parent.def.size.x * parent.def.size.z / 2;
                }
                return cachedDiagonal;
            }
        }


        public float CurrentCenterAngle = 0;

        public float CurrentSpan = -1;

        Vector3 MousePos => MousePosYto0 + Altitide;
        Vector3 MousePosYto0 => UI.MouseMapPosition().Yto0();

        Vector3 Altitide => Vector3.up * 10;

        float effectiveLeftSpan => Mathf.Max(CenterAngle - Span / 2, maxLeftSpan);
        float effectiveRightSpan => Mathf.Min(CenterAngle + Span / 2, maxRightSpan);

        float maxLeftSpan => -Props.maxSpanDeviation;
        float maxRightSpan => Props.maxSpanDeviation;

        public Vector3 Left => Vector3.forward.RotatedBy(effectiveLeftSpan + parent.Rotation.AsAngle);
        public Vector3 Right => Vector3.forward.RotatedBy(effectiveRightSpan + parent.Rotation.AsAngle);

        public bool WithinFireArc(LocalTargetInfo tgt)
        {
            var angle = Vector3.SignedAngle(Vector3.forward.RotatedBy(parent.Rotation.AsAngle), (tgt.CenterVector3 - parent.DrawPos).Yto0(), Vector3.up);
            return angle > effectiveLeftSpan && angle < effectiveRightSpan;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            var drawPos = parent.DrawPos.Yto0();
            if (Editing)
            {
                EditingLineLength = Vector3.Distance(drawPos, MousePosYto0);
                NewCenterAngle = Vector3.SignedAngle(Vector3.forward, (MousePosYto0 - drawPos).RotatedBy(-parent.Rotation.AsAngle), Vector3.up);
                if (Props.maxSpanDeviation < 180) { Mathf.Clamp(NewCenterAngle, maxLeftSpan, maxRightSpan); }
                else { Mathf.Clamp(NewCenterAngle, -180, 180); }

                drawPos += Altitide;

                GenDraw.DrawLineBetween(drawPos, MousePos);

                if (Input.GetMouseButtonDown(1))
                {
                    Editing = false;
                    EditingSpan = false;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    CurrentCenterAngle = NewCenterAngle;
                    CurrentSpan = NewSpan;
                    Editing = false;
                    EditingSpan = false;
                    if (parent is Building_TurretGunCE fa)
                    {
                        fa.PostAdjustFireArc();
                    }
                }

                if (Input.GetMouseButtonDown(2))
                {
                    EditingSpan = !EditingSpan;
                    EditingSpanLineLength = Props.spanRange.Span / (NewSpan - Props.spanRange.min) * (EditingLineLength - DiagonalLength);
                }
                if (EditingSpan)
                {
                    var l = (EditingLineLength - DiagonalLength) / EditingSpanLineLength;
                    NewSpan = Mathf.Lerp(Props.spanRange.min, Props.spanRange.max, l);

                    var norm = Vector3.right.RotatedBy(NewCenterAngle);
                    var ext = Vector3.forward.RotatedBy(CenterAngle);

                    var ext2 = ext * DiagonalLength;
                    GenDraw.DrawLineBetween(drawPos + ext2 - norm, drawPos + ext2 + norm, color: SimpleColor.Red);
                    ext *= EditingSpanLineLength + DiagonalLength;
                    GenDraw.DrawLineBetween(drawPos + ext - norm, drawPos + ext + norm, color: SimpleColor.Red);
                    GenDraw.DrawLineBetween(drawPos, drawPos + ext);
                }
                if (Props.maxSpanDeviation < 180)
                {
                    var leftBound = Vector3.forward.RotatedBy(maxLeftSpan + parent.Rotation.AsAngle) * (LineLength + 1);
                    var rightBound = Vector3.forward.RotatedBy(maxRightSpan + parent.Rotation.AsAngle) * (LineLength + 1);
                    GenDraw.DrawLineBetween(drawPos, drawPos + leftBound, color: SimpleColor.Red);
                    GenDraw.DrawLineBetween(drawPos, drawPos + rightBound, color: SimpleColor.Red);
                    DrawCircleOutline(drawPos, LineLength + 1, maxLeftSpan, maxRightSpan, 0, SimpleColor.Red);
                }
            }
            else
            {
                drawPos += Altitide;
            }
            GenDraw.DrawLineBetween(drawPos, drawPos + Left * LineLength);
            GenDraw.DrawLineBetween(drawPos, drawPos + Right * LineLength);

            DrawCircleOutline(drawPos, LineLength, effectiveLeftSpan, effectiveRightSpan, CenterAngle);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action Edit = new Command_Action();
            Edit.defaultLabel = "L";
            Edit.defaultDesc = "Right click to save change\nLeft click to cancel change\nMiddle click to edit span";
            Edit.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
            Edit.action = delegate
            {
                Editing = true;
                NewSpan = CurrentSpan;
            };
            yield return Edit;
        }

        public override string CompInspectStringExtra()
        {
            return $"Arc of fire: {(int)effectiveLeftSpan}~{(int)effectiveRightSpan}";
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref CurrentCenterAngle, "CurrentCenterAngle");
            Scribe_Values.Look(ref CurrentSpan, "CurrentSpan");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (CurrentSpan < 0) { CurrentSpan = Props.spanRange.max; }
        }

        public void DrawCircleOutline(Vector3 center, float radius, float spanL, float spanR, float startingAngle = 0, SimpleColor color = SimpleColor.White)
        {
            startingAngle = -spanR + 90 - parent.Rotation.AsAngle;
            startingAngle *= Mathf.Deg2Rad;

            float spanf = (spanR - spanL) / 360;
            int stepCount = Mathf.Clamp(Mathf.RoundToInt(24f * radius * spanf), 12, 48);
            float step = (float)Math.PI * 2f * spanf / stepCount;

            Vector3 vector = center;
            Vector3 a = center;
            for (int i = 0; i < stepCount + 2; i++)
            {
                if (i >= 2)
                {
                    GenDraw.DrawLineBetween(a, vector, color);
                }

                a = vector;
                vector = center;
                vector.x += Mathf.Cos(startingAngle) * radius;
                vector.z += Mathf.Sin(startingAngle) * radius;
                startingAngle += step;
            }
        }
    }



    public class CompProperties_FireArc : CompProperties
    {
        public CompProperties_FireArc()
        {
            compClass = typeof(CompFireArc);
        }
        public FloatRange spanRange = new FloatRange(0, 360);

        public float maxSpanDeviation = 180;

        public float lineLength = 3;
    }
}
