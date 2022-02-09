using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended
{
    public class StatPart_Bulk : StatPart
    {
        public bool ValidReq(StatRequest req)
        {
            return req.HasThing && inv(req) != null;
        }

        public CompInventory inv(StatRequest req)
        {
            return req.Thing.TryGetComp<CompInventory>();
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (ValidReq(req))
            {
                return "CE_BulkEffect".Translate() + " x" + MassBulkUtility.HitChanceBulkFactor(inv(req).currentBulk, inv(req).capacityBulk);
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (ValidReq(req))
            {
                val *= MassBulkUtility.HitChanceBulkFactor(inv(req).currentBulk, inv(req).capacityBulk);
            }
        }
    }
}
