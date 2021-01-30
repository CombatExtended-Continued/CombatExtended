using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CombatExtended.Lasers
{
    interface IDrawnWeaponWithRotation
    {
        float RotationOffset
        {
            get;
            set;
        }
    }
}
