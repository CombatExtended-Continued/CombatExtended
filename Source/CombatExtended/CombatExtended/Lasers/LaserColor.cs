using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace CombatExtended.Lasers
{
    public class LaserColor
    {
        static LaserColor[] colors = {
            new LaserColor { index = 0, name = "RimlaserBeamBrown" },
            new LaserColor { index = 1, name = "RimlaserBeamOrange" },
            new LaserColor { index = 2, name = "RimlaserBeamRed" },
            new LaserColor { index = 3, name = "RimlaserBeamPink" },
            new LaserColor { index = 4, name = "RimlaserBeamBlue" },
            new LaserColor { index = 5, name = "RimlaserBeamTeal" },
            new LaserColor { index = 6, name = "RimlaserBeamRedBlack", allowed = false },
        };

        internal static int IndexBasedOnThingQuality(int index, Thing gun)
        {
            if (index != -1) return index;

            QualityCategory qc;
            if (gun.TryGetQuality(out qc))
            {
                switch (qc)
                {
                    case QualityCategory.Awful: return 0;
                    case QualityCategory.Poor: return 1;
                    case QualityCategory.Normal: return 2;
                    case QualityCategory.Good: return 3;
                    case QualityCategory.Excellent: return 4;
                    case QualityCategory.Masterwork: return 5;
                    case QualityCategory.Legendary: return 6;
                }
            }

            return 2;
        }

        public int index;
        public string name;
        public bool allowed = true;
        
        
    };
}
