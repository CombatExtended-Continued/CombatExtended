using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended
{
    public class ShellingResponseDef : Def
    {
        public float defaultShellingPropability = 0.0f;
        public float defaultRaidPropability = 0.0f;
        public float defaultRaidMTBDays = 0.0f;        

        /// <summary>
        /// The list of projectiles that can be used in response when shelled
        /// </summary>
        public List<ShellingResponsePart_Projectile> projectiles = new List<ShellingResponsePart_Projectile>();
        /// <summary>
        /// The list of worldobjects and how the faction should response when they are attacked
        /// </summary>
        public List<ShellingResponsePart_WorldObject> worldObjects = new List<ShellingResponsePart_WorldObject>();

        public override void PostLoad()
        {
            if (projectiles == null)            
                projectiles = new List<ShellingResponsePart_Projectile>();            
            if (worldObjects == null)           
                worldObjects = new List<ShellingResponsePart_WorldObject>();                
            base.PostLoad();
        }

        /// <summary>
        /// Contains data related to how a faction response when a world object of the faction is attacked
        /// </summary>
        public class ShellingResponsePart_WorldObject
        {
            public WorldObjectDef worldObject;
            
            public float shellingPropability;                        
            public float raidPropability;
            public float raidMTBDays;
        }

        /// <summary>
        /// Contains data related to a projectile that the faction can use in response to an attack
        /// </summary>
        public class ShellingResponsePart_Projectile
        {
            /// <summary>
            /// Projectile def
            /// </summary>
            public ThingDef projectile;
            /// <summary>
            /// The equivalent in raid points for this projectile
            /// </summary>
            public float points;
            /// <summary>
            /// A weight used when selecting projectiles for counter shelling
            /// </summary>
            public float weight = 0.1f;
        }
    }
}

