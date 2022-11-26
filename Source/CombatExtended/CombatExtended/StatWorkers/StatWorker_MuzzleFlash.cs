﻿using System;
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
                var count = 0;
                var scale = 0f;
                foreach (VerbProperties prop in def.verbs)
                {
                    if (!prop.IsMeleeAttack && prop.muzzleFlashScale > 1e-2f)
                    {
                        scale += prop.muzzleFlashScale;
                        count++;
                    }
                }
                return scale / count;
            }
            return 0f;
        }
    }
}
