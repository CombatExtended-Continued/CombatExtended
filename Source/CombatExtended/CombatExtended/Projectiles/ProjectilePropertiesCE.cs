using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class ProjectilePropertiesCE : ProjectileProperties
    {
        public TravelingShellProperties shellingProps;

        // public float armorPenetration = 0;
        public int pelletCount = 1;
        public float spreadMult = 1;
        public List<SecondaryDamage> secondaryDamage = new List<SecondaryDamage>();
        public bool damageAdjacentTiles = false;
        public bool dropsCasings = false;
        public string casingMoteDefname = "Fleck_EmptyCasing";
        public string casingFilthDefname = "Filth_RifleAmmoCasings";
        public float gravityFactor = 1;
        public bool isInstant = false;
        public bool damageFalloff = true; // Damage falloff for *instant* projectiles.
        public float armorPenetrationSharp;
        public float armorPenetrationBlunt;
        public bool castShadow = true;

        public float suppressionFactor = 1;
        public float airborneSuppressionFactor = 1;
        public float dangerFactor = 1;

        public FloatRange ballisticCoefficient = new FloatRange(1f, 1f);
        public FloatRange mass = new FloatRange(1f, 1f);
        public FloatRange diameter = new FloatRange(1f, 1f);

        public bool lerpPosition = true;
        public ThingDef detonateMoteDef;
        public FleckDef detonateFleckDef;
        public float detonateEffectsScaleOverride = -1;

        #region Bunker Buster fields
        /// <summary>
        /// Amount of tiles ProjectileCE_BunkerBuster will detonate after after penetrating an obstacle
        /// </summary>
        public int fuze_delay = 2;

        public bool HP_penetration = false;

        public float HP_penetration_ratio = 1f;

        #endregion

        public int armingDelay = 0;
        public float aimHeightOffset = 0;

        public float empShieldBreakChance = 1f;
        public float Gravity => CE_Utility.GravityConst * gravityFactor;
    }
}
