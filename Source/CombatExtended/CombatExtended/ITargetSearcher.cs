using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public interface ITargetSearcher
    {
        bool TryFindNewTarget(out LocalTargetInfo target);
    }
}
