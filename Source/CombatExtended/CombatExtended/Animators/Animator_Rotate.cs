using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class Animator_Rotate : IAnimator
    {                
        /// <summary>
        /// Angle in degrees
        /// </summary>
        protected float angle;
        protected float angleIncrement;

        public Animator_Rotate(Thing thing, AnimatedPart part) : base(thing, part)
        {            
            angleIncrement = 360f / (60 / part.speed);
        }

        public override void DrawAt(Vector3 drawPos)
        {            
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(drawPos + part.offset, Quaternion.Euler(0, angle, 0), part.scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, part.graphicData.Graphic.MatSingle, 0);
        }

        public override void Tick()
        {
            angle += angleIncrement;
            if (angleIncrement >= 360f)
            {
                angleIncrement -= 360f;
            }
        }
    }
}

