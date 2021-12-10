using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended
{
    public class PresetPatcherDef : Def
    {
        public string ID = "none";

        public float bulk;

        public float mass;

        public float Sight_Efficiency;

        public float BaseRecoil;

        public int FullAutoShotCount;

        public int Range;

        public float WarmupTime;

        public int shotinterval;

        public ThingDef DefaultProj;

        public int MagSize;

        public float ReloadTime;

        public int BurstCount;

        public float CooldownRanged;

        public float SwayAm;

        public float BaseSpread;


        //Will be later changed to SimilarTo system
        public AmmoSetDef ammoset;

    }
}
