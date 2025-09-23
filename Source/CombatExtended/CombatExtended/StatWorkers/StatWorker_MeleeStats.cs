using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_MeleeStats : StatWorker
{
    public override bool IsDisabledFor(Thing thing)
    {
        return thing?.def?.building?.IsTurret ?? base.IsDisabledFor(thing);
    }
}
