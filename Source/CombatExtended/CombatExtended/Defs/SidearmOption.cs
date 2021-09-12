using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class SidearmOption
    {
        public FloatRange sidearmMoney = FloatRange.Zero;
        public FloatRange magazineCount = FloatRange.Zero;
        public List<string> weaponTags;
        public float generateChance = 1;
        public AttachmentOption attachments;
    }
}
