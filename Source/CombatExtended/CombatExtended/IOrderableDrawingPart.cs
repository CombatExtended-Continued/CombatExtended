using System;
using UnityEngine;

namespace CombatExtended
{
    public struct IOrderableDrawingPart : IComparable<IOrderableDrawingPart>
    {
        public Mesh mesh;
        public Material mat;
        public readonly int priority;

        public IOrderableDrawingPart(Mesh mesh, Material mat, int priority)
        {
            this.mesh = mesh;
            this.mat = mat;
            this.priority = priority;
        }

        public int CompareTo(IOrderableDrawingPart other)
        {
            return priority.CompareTo(other.priority);
        }        
    }
}

