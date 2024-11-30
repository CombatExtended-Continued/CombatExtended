using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class VerbPropertiesCE : VerbProperties
    {
        public RecoilPattern recoilPattern = RecoilPattern.None;
        public int ammoConsumedPerShotCount = 1;
        public float recoilAmount = 0;
        public float indirectFirePenalty = 0;
        public float circularError = 0;
        public int ticksToTruePosition = 5;
        public bool ejectsCasings = true;
        public bool ignorePartialLoSBlocker = false;
        public bool interruptibleBurst = true;
    }
}
