using UnityEngine;
using Verse;

namespace CombatExtended.Detours
{
    class InitializerBehaviour : MonoBehaviour
    {
        protected bool reinjectNeeded;
        protected float reinjectTime;

        public void OnLevelWasLoaded(int level)
        {
            reinjectNeeded = true;
            reinjectTime = level >= 0 ? 1 : 0;
        }

        public void FixedUpdate()
        {
            if (reinjectNeeded)
            {
                reinjectTime -= Time.fixedDeltaTime;

                if (reinjectTime <= 0)
                {
                    reinjectNeeded = false;
                    reinjectTime = 0;
                }
            }

        }

        public void Start()
        {
            OnLevelWasLoaded(-1);
        }
    }
}
