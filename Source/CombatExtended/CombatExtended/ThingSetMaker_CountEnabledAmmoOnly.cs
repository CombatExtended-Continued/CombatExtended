using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class ThingSetMaker_CountEnabledAmmoOnly : ThingSetMaker_Count
    {
        bool basic = true;

        bool advanced = false;

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            parms.validator = d => CE_ThingSetMakerUtility.CanGenerate(d, basic, advanced);
            base.Generate(parms, outThings);
        }
    }
    public class ThingSetMaker_StackCountEnabledAmmoOnly : ThingSetMaker_StackCount
    {
        bool basic = true;

        bool advanced = false;

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            parms.validator = d => CE_ThingSetMakerUtility.CanGenerate(d, basic, advanced);
            base.Generate(parms, outThings);
        }
    }
    public class ThingSetMaker_CountWithAmmo : ThingSetMaker_Count
    {
        IntRange magCount = new IntRange(2, 5);

        bool random = false;

        bool canGenerateAdvanced = false;

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            base.Generate(parms, outThings);
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(outThings, random, canGenerateAdvanced, magCount);
        }
    }

    public class ThingSetMaker_MarketValueWithAmmo : ThingSetMaker_MarketValue
    {
        IntRange magCount = new IntRange(2, 5);

        bool random = false;

        bool canGenerateAdvanced = false;

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            base.Generate(parms, outThings);
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(outThings, random, canGenerateAdvanced, magCount);
        }
    }
}
