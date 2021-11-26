using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class IAnimator
    {
        public Thing thing;
        public AnimatedPart part;

        public IAnimator(Thing thing, AnimatedPart part)
        {
            this.thing = thing;
            this.part = part;
        }

        public abstract void Tick();
        public abstract void DrawAt(Vector3 drawPos);
    }
}

