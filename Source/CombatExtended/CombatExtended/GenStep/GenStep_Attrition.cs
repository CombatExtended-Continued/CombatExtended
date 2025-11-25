using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.WorldObjects;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;
public class GenStep_Attrition : GenStep
{


    //private struct DamagedSite
    //{
    //    public int radius;
    //    public float damage;
    //    public IntVec3 location;
    //}

    private Map map;
    private HealthComp healthComp;


    public override int SeedPart => 1158116095;

    public GenStep_Attrition()
    {
    }

    public override void Generate(Map map, GenStepParams parms)
    {
        this.map = map;
        WorldObjectDamageWorker.BeginAttrition(map);
        healthComp = this.map.Parent?.GetComponent<HealthComp>() ?? null;
        if (healthComp != null && healthComp.Health < 0.98f)
        {
            try
            {
                foreach (var shell in healthComp.recentShells)
                {
                    shell.ShellDef.GetWorldObjectDamageWorker().ProcessShell(shell.ShellDef);
                }

            }
            catch (Exception er)
            {
                Log.Error($"CE: GenStep_Attrition Failed with error {er}");
            }
        }
        this.map = null;
        WorldObjectDamageWorker.EndAttrition();
        this.healthComp = null;
    }

}

