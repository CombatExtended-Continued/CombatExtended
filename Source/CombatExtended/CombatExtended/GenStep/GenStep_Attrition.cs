using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class GenStep_Attrition : GenStep
    {       
        public override int SeedPart => 1158116095;        

        public GenStep_Attrition()
        {            
        }

        public override void Generate(Map map, GenStepParams parms)
        {
            if (!map.IsPlayerHome)
            {
                AttritionUtility.TryApplyAttrition(map);
            }
        }
    }
}

