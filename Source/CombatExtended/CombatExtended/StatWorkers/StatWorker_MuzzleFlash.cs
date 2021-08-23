using System;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class StatWorker_MuzzleFlash : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            ThingDef def = req.Thing?.def;
            if (def != null && def.verbs != null && def.IsRangedWeapon && def.verbs.Count > 0)
            {
                float val = def.verbs[0].muzzleFlashScale;               
                return val < 0 ? 0 : val;
            }
            return 0f;
        }        
    }
}
