using RimWorld;
using Verse;

namespace CombatExtended
{
    public class HediffComp_Venom : HediffComp
    {
        private float _venomPerTick;
        private int _lifetime;

        public HediffCompProperties_Venom Props => props as HediffCompProperties_Venom;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            _venomPerTick = Props.VenomPerSeverity * parent.Severity / parent.pawn.BodySize * parent.pawn.GetStatValue(StatDefOf.ToxicSensitivity);
            _lifetime = Rand.Range(Props.MinTicks, Props.MaxTicks);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (parent.ageTicks < _lifetime)
                HealthUtility.AdjustSeverity(parent.pawn, CE_HediffDefOf.VenomBuildup, _venomPerTick);
        }

        public override void CompTended_NewTemp(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended_NewTemp(quality, maxQuality, batchPosition);
            _venomPerTick *= 1 - quality;
        }
    }
}