using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobCE : Job
    {
        public bool flagA;
        public bool flagB;
        public bool flagC;

        public int intA;
        public int intB;
        public int intC;

        private List<ThingDef> _targetDefs;
        public List<ThingDef> targetDefs
        {
            get
            {
                if (_targetDefs == null)
                    _targetDefs = new List<ThingDef>();
                return _targetDefs;
            }
        }

        public void PostExposeData()
        {
            Scribe_Values.Look(ref flagA, "flagA");
            Scribe_Values.Look(ref flagB, "flagB");
            Scribe_Values.Look(ref flagC, "flagC");

            Scribe_Values.Look(ref intA, "intA");
            Scribe_Values.Look(ref intB, "intB");
            Scribe_Values.Look(ref intC, "intC");

            Scribe_Collections.Look(ref _targetDefs, "TargetDef", LookMode.Def);
            if (_targetDefs == null)
                _targetDefs = new List<ThingDef>();
        }
    }
}
