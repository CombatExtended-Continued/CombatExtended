using RimWorld;
using Verse;

namespace CombatExtended
{
    public class LetterTracker : MapComponent
    {
        private static bool _sentMechWarning;

        public LetterTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (!_sentMechWarning && GenDate.DaysPassed >= Faction.OfMechanoids.def.earliestRaidDays * 0.75f)
            {
                var suggestingPawn = Find.AnyPlayerHomeMap.mapPawns.FreeColonistsSpawnedCount != 0 
                    ? Find.AnyPlayerHomeMap.mapPawns.FreeColonistsSpawned.RandomElement() 
                    : Find.AnyPlayerHomeMap.mapPawns.FreeColonists.RandomElement();

                var label = "CE_MechWarningLabel".Translate();
                var text = "CE_MechWarningText".Translate(suggestingPawn.LabelShort, CE_ThingDefOf.Mech_Centipede.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp));

                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent);
                _sentMechWarning = true;
            }
        }
    }
}