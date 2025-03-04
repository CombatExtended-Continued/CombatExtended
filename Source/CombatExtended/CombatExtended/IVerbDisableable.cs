using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CombatExtended
{
    public interface IVerbDisableable
    {
        bool HoldFire { get; set; }
        string HoldFireLabel { get; }
        string HoldFireDesc { get; }
        Texture2D HoldFireIcon { get; }
    }
}
