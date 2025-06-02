using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class MultiBarrelExtension : DefModExtension
    {
        public List<Vector2> offsets = new List<Vector2>();

        public Vector2 GetOffsetFor(int index)
        {
            if (offsets.NullOrEmpty())
            {
                return Vector2.zero;
            }
            int i = index % offsets.Count;
            return offsets[i];
        }
    }
}
