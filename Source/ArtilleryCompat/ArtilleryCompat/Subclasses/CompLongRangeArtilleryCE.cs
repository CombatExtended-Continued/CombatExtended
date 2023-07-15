using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using CombatExtended;
using VFESecurity;
using System;

namespace CombatExtended.Compatibility.Artillery
{
    //[Obsolete]
    public class CompLongRangeArtilleryCE : CompLongRangeArtillery
    {
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Log.Warning($"Used an obsolete CompLongRangeArtilleryCE into {parent.def.defName}");
        }
        public override void CompTick()
        {
            
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        
    }
}
