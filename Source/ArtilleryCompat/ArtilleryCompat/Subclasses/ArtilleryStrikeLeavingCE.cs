using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility.Artillery
{
    public class ArtilleryStrikeLeavingCE : ArtilleryStrikeLeaving
    {
        public AmmoDef shellDef = null;
        public override ThingDef ShellDef => shellDef;
    }
}
