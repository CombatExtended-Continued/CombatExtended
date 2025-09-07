using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI;
public enum CombatRole : byte
{
    Undefined,
    Melee,
    Assault,
    Sniper,
    Suppressor,
    Grenadier,
    Rocketeer
}
