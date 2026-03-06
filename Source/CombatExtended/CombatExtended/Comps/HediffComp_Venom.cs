using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;
public class HediffComp_Venom : HediffComp
{
    private float _venomPerTick;
    private int _lifetime;
    private bool? _isPermanent;

    public HediffCompProperties_Venom Props => props as HediffCompProperties_Venom;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        var toxicSensitivity = Mathf.Max(1f - parent.pawn.GetStatValue(StatDefOf.ToxicResistance), 0f);
        _venomPerTick = Props.VenomPerSeverity * parent.Severity / parent.pawn.BodySize * toxicSensitivity;
        _lifetime = Rand.Range(Props.MinTicks, Props.MaxTicks);
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        _isPermanent ??= parent.IsPermanent();

        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (parent.ageTicks < _lifetime && !_isPermanent.Value)
        {
            HealthUtility.AdjustSeverity(parent.pawn, CE_HediffDefOf.VenomBuildup, _venomPerTick * delta);
        }
    }

    public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
    {
        base.CompTended(quality, maxQuality, batchPosition);
        _venomPerTick *= 1 - quality;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref _venomPerTick, "venomPerTick");
        Scribe_Values.Look(ref _lifetime, "lifetime");
    }
}
