using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class PawnRenderNodeWorker_Drafted : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            var pawn = node.tree.pawn;
            return pawn != null && pawn.Spawned && (pawn.Drafted || (pawn.CurJob?.def.alwaysShowWeapon ?? false) || (pawn.mindState?.duty?.def.alwaysShowWeapon ?? false)) && base.CanDrawNow(node, parms);
        }
    }
}
