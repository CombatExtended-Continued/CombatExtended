using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    [DefOf]
    public static class CE_JobDefOf
    {
        public static JobDef TakeFromOther;
        public static JobDef ReloadWeapon;
        public static JobDef ReloadTurret;
        public static JobDef HunkerDown;
        public static JobDef RunForCover;
        public static JobDef Stabilize;
        public static JobDef WaitKnockdown;
        public static JobDef EquipFromInventory;
        public static JobDef OpportunisticAttack;
        public static JobDef TendSelf;
        public static JobDef ModifyWeapon;
        public static JobDef GiveAmmo;
    }
}
