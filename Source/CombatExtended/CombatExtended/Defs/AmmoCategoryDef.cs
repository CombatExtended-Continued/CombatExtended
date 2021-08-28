using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended
{
    public class AmmoCategoryDef : Def
    {
        public bool advanced = false;                
      
        public string labelShort;

        public string LabelCapShort => labelShort.CapitalizeFirst();      
    }
}
