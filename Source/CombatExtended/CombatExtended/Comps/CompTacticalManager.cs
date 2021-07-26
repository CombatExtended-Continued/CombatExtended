using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using CombatExtended.AI;
using Verse;

namespace CombatExtended
{
    public class CompTacticalManager : ThingComp
    {
        private List<ICompTactics> _tacticalComps = null;
        public List<ICompTactics> TacticalComps
        {
            get
            {
                if (_tacticalComps == null)
                {
                    _tacticalComps = parent.comps?.Where(c => c is ICompTactics).Cast<ICompTactics>().ToList() ?? null;
                    _tacticalComps?.SortBy(c => -1 * c.Priority);
                }
                return _tacticalComps;
            }
        }

        public bool TryStartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            bool AllChecksPassed(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg, out ICompTactics failedComp)
            {
                foreach (ICompTactics comp in TacticalComps)
                {
                    if (!comp.StartCastChecks(verb, castTarg, destTarg))
                    {
                        failedComp = comp;
                        return false;
                    }
                }
                failedComp = null;
                return true;
            }
            if (AllChecksPassed(verb, castTarg, destTarg, out var failedComp))
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksSuccess(verb);
                return true;
            }
            else
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksFailed(failedComp);
                return false;
            }
        }
    }
}
