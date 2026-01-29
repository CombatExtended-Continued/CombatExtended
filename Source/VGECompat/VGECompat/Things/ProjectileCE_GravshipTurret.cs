using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CombatExtended.Compatibility.VGECompat;

public class ProjectileCE_GravshipTurret : ProjectileCE_Explosive
{
    public override Quaternion DrawRotation => ExactRotation;

    //public override Quaternion DrawRotation
    //{
    //    get
    //    {
    //        return Quaternion.LookRotation(ExactPosition - LastPos); ;
    //    }
    //}
}

