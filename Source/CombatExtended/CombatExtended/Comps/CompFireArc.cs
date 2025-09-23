using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Noise;
using System.Linq;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class CompFireArc : ThingComp
{
    static CompFireArc()
    {
        _greenMat = DebugMatsSpectrum.Mat(50, false);
        _greenMat.color = _greenMat.color.ToTransparent(0.1f);
    }

    public CompProperties_FireArc Props => props as CompProperties_FireArc;

    public float CenterAngle => Editing ? NewCenterAngle : CurrentCenterAngle;

    public float Span => Editing ? NewSpan : CurrentSpan;

    public float LineLength => Editing ? EditingLineLength : Props.lineLength;

    bool functionEnabled => Controller.settings.EnableArcOfFire && (turnedOn || !canTurnOff);

    bool canTurnOff => Props.maxSpanDeviation >= 180 && Props.spanRange.max >= 180;

    bool turnedOn = true;


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

    FloatRange? supplementarySpan;

    FloatRange? SupplementarySpan
    {
        get
        {
            if (Editing)
            {
                if (effectiveLeftSpan < -180)
                {
                    supplementarySpan = new FloatRange(effectiveLeftSpan + 360, 180);
                }
                else
                {
                    supplementarySpan = effectiveRightSpan > 180 ? new FloatRange(-180, effectiveRightSpan - 360) : (FloatRange?)null;
                }
            }
            return supplementarySpan;
        }
    }

    public Vector3 Left => Vector3.forward.RotatedBy(effectiveLeftSpan + parent.Rotation.AsAngle);
    public Vector3 Right => Vector3.forward.RotatedBy(effectiveRightSpan + parent.Rotation.AsAngle);

    List<IntVec3> _cellCache = new List<IntVec3>();
    static readonly Material _greenMat;

    public bool WithinFireArc(LocalTargetInfo tgt)
    {
        if (!functionEnabled)
        {
            return true;
        }
        var angle = Vector3.SignedAngle(Vector3.forward.RotatedBy(parent.Rotation.AsAngle), (tgt.CenterVector3 - parent.DrawPos).Yto0(), Vector3.up);
        return (angle >= effectiveLeftSpan && angle <= effectiveRightSpan) || (SupplementarySpan.HasValue && angle >= SupplementarySpan.Value.min && angle <= SupplementarySpan.Value.max);
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        if (!functionEnabled)
        {
            return;
        }

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

                var norm = Vector3.right.RotatedBy(NewCenterAngle + parent.Rotation.AsAngle);
                var ext = Vector3.forward.RotatedBy(CenterAngle + parent.Rotation.AsAngle);

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


        if (KeyBindingDefOf.ShowEyedropper.IsDown)
        {
            _cellCache.Clear();
            foreach (VerbProperties verbProperties in parent.def.building.turretGunDef.Verbs)
            {
                var cells = GenRadial.RadialCellsAround(parent.Position, verbProperties.minRange, verbProperties.range).ToList();
                foreach (var cell in cells)
                {
                    if (GenSight.LineOfSightToThing(cell, parent, parent.Map) && WithinFireArc(cell))
                    {
                        _cellCache.Add(cell);
                    }
                }
            }
            if (_cellCache.Any())
            {
                GenDraw.DrawFieldEdges(_cellCache);
                foreach (var cell in _cellCache)
                {
                    CellRenderer.RenderCell(cell, _greenMat);
                }
            }
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Controller.settings.EnableArcOfFire)
        {
            Command_Action Edit = new Command_Action
            {
                defaultLabel = "CE_ArcOfFireAdjLabel".Translate(),
                defaultDesc = "CE_ArcOfFireAdjDesc".Translate(KeyBindingDefOf.ShowEyedropper.MainKeyLabel) + "\n\n" + (canTurnOff ? "CE_ArcOfFireToggle" : "CE_ArcOfFireReset").Translate(KeyBindingDefOf.ShowEyedropper.MainKeyLabel),
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/AdjustFireArc", true),
                action = delegate
                {
                    if (KeyBindingDefOf.ShowEyedropper.IsDown)
                    {
                        if (canTurnOff)
                        {
                            turnedOn = !turnedOn;
                        }
                        else
                        {
                            CurrentCenterAngle = 0;
                            CurrentSpan = Props.spanRange.max;
                        }
                    }
                    else
                    {
                        Editing = true;
                        NewSpan = CurrentSpan;
                        turnedOn = true;
                    }
                }
            };
            yield return Edit;
        }

        if (functionEnabled)
        {


            Command_Action Copy = new Command_Action();
            Copy.defaultLabel = "CE_ArcOfFireCopyLabel".Translate();
            Copy.defaultDesc = "CE_ArcOfFireCopyDesc".Translate();
            Copy.icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings", true);
            Copy.action = delegate
            {
                FireArcCopyPaster.CopyFrom(this);
            };
            yield return Copy;

            if (FireArcCopyPaster.HasData)
            {
                Command_Action Paste = new Command_Action();
                Paste.defaultLabel = "CE_ArcOfFirePasteLabel".Translate();
                Paste.defaultDesc = "CE_ArcOfFirePasteDesc".Translate();
                Paste.icon = ContentFinder<Texture2D>.Get("UI/Commands/PasteSettings", true);
                Paste.action = delegate
                {
                    FireArcCopyPaster.PasteTo(this);
                };
                yield return Paste;
            }
        }

    }

    List<FloatMenuOption> Toggle()
    {
        return null;
    }

    public override string CompInspectStringExtra()
    {
        if (functionEnabled)
        {
            return $"CE_ArcOfFire".Translate((int)effectiveLeftSpan, (int)effectiveRightSpan);
        }
        return null;
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

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (functionEnabled)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "CE_MaxArcOfFireSpan".Translate(), Props.spanRange.ToString(), "CE_MaxArcOfFireSpanDesc".Translate(), 0);
            if (Props.maxSpanDeviation < 180)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "CE_MaxArcOfFireDeviation".Translate(), Props.maxSpanDeviation.ToString(), "CE_MaxArcOfFireDeviationDesc".Translate(), 0);
            }
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

    public float maxSpanDeviation = 360;

    public float lineLength = 3;

    public override void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        if (maxSpanDeviation >= 180) { maxSpanDeviation = 360; }
    }
}

public static class FireArcCopyPaster
{
    public static float CenterAngle = 0;

    public static float Span = -1;

    public static bool HasData => Span > 0;

    public static void CopyFrom(CompFireArc comp)
    {
        CenterAngle = comp.CenterAngle;
        Span = comp.Span;
    }
    public static void PasteTo(CompFireArc comp)
    {
        comp.CurrentSpan = Mathf.Clamp(Span, comp.Props.spanRange.min, comp.Props.spanRange.max);
        comp.CurrentCenterAngle = Mathf.Clamp(CenterAngle, -comp.Props.maxSpanDeviation, comp.Props.maxSpanDeviation);
    }
}
