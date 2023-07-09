using System;
using Verse;

namespace CombatExtended
{
    public class TravelingShellProperties
    {
        /// <summary>
        /// The number of world tiles traveled per tick
        /// </summary>
        public float tilesPerTick = 0.2f;
        /// <summary>
        /// The range in tiles
        /// </summary>
        public float range = 30;
        /// <summary>
        /// Damage done to tile
        /// </summary>
        public float damage;

        public Type workerClass = typeof(WorldObjectDamageWorker);
        public WorldObjectDamageWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (WorldObjectDamageWorker)Activator.CreateInstance(workerClass);
                }
                return workerInt;
            }
        }
        [Unsaved]
        private WorldObjectDamageWorker workerInt;
    }
}
