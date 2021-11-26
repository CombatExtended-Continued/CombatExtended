using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class AnimatedPart
    {
        /// <summary>
        /// Graphic data of the animated part.
        /// </summary>
        public GraphicData graphicData;

        /// <summary>
        /// Offset for the animated part.
        /// </summary>
        public Vector3 offset = Vector3.zero;

        /// <summary>
        /// Scale for the animated part.
        /// </summary>
        public Vector3 scale = new Vector3(1, 1, 1);

        /// <summary>
        /// Animation speed.
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// Class of the animator.
        /// </summary>
        public Type animatorClass;
    }
}

