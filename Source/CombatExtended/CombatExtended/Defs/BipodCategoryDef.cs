using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class BipodCategoryDef : Def
    {
        public string bipod_id = "Bipod_Undefined";

        public int ad_Range = 0;

        public int setuptime = 240;

        public float recoil_mult_setup = 1f;

        public float recoil_mult_NOT_setup = 1f;

        public float warmup_mult_setup = 1f;

        public float warmup_mult_NOT_setup = 1f;

        public Color log_color = Color.blue;
    }
}
