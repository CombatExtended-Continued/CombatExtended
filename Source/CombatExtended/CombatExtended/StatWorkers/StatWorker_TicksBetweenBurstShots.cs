using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class StatWorker_TicksBetweenBurstShots : StatWorker
    {
        public StatWorker_TicksBetweenBurstShots()
        {
        }        

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            val = Mathf.Ceil(60 / val);
            return base.ValueToString(val, finalized, numberSense);
        }
    }
}
