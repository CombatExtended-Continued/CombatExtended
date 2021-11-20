using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class GenStep_Attrition : GenStep
    {
        private const float WORLD_SHELLDAMAGE = 0.015f;
        private const float MAP_SHELLMINDAMAGE = 100;
        private const float MAP_SHELLMAXDAMAGE = 300;
        private const int MAP_MINSITECOUNT = 5;
        private const int MAP_GENLIMITER = 50;

        private int siteCount;
        private Map map;
        private WorldObjects.HealthComp healthComp;

        private readonly List<float> damageList = new List<float>();
        
        public override int SeedPart => 1158116095;        

        public GenStep_Attrition()
        {            
        }        

        public override void Generate(Map map, GenStepParams parms)
        {
            this.damageList.Clear();
            this.map = map;            
            healthComp = this.map.Parent?.GetComponent<WorldObjects.HealthComp>() ?? null;
            float health = healthComp.Health;
            if (healthComp != null && health < 0.999f)
            {
                try
                {
                    siteCount = (int)((1 - health) / WORLD_SHELLDAMAGE);
                    if (siteCount > 0)
                    {
                        siteCount = Mathf.Max(siteCount, MAP_MINSITECOUNT);
                        for (int i = 0; i < siteCount; i++)
                        {
                            damageList.Add(CE_Utility.RandomGaussian(MAP_SHELLMINDAMAGE, MAP_SHELLMAXDAMAGE));                           
                        }
                        int m = siteCount * MAP_GENLIMITER;
                        int n = 0;
                        while (m-- > 0 && n < siteCount)
                        {
                            IntVec3 cell = new IntVec3((int)CE_Utility.RandomGaussian(0, map.Size.x), 0, (int)CE_Utility.RandomGaussian(0, map.Size.z));
                            RoofDef roof = cell.GetRoof(map);
                            if(roof != RoofDefOf.RoofRockThick)
                            {
                                ApplyAttrition();
                            }                            
                        }
                    }
                }
                catch (Exception er)
                {
                    Log.Error($"CE: GenStep_Attrition Failed with error {er}");
                }
            }           
            this.map = null;
        }        

        private void ApplyAttrition()
        {

        }        
    }
}

