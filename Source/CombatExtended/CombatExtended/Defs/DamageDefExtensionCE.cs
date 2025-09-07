using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class DamageDefExtensionCE : DefModExtension
    {
        public bool noDamageOnDeflect = false;
        public bool harmOnlyOutsideLayers = false;
        public bool isAmbientDamage = false;
        public float worldDamageMultiplier = -1f;
    }
}
