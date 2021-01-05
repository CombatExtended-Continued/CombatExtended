using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.TrailerProjectileExtension
    public class TrailerProjectileExtension : DefModExtension
    {
        public string trailMoteDef = "Mote_Smoke";
        public float trailMoteSize = 0.5f;
        public int trailerMoteInterval = 30;
        public int motesThrown = 1;
    }

}
