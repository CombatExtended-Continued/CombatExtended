using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class TurretDrawExtension : DefModExtension
    {
        public GraphicData TurretBottomTexPath;

        public GraphicData TurretTopTexPath;

        public List<TurretDrawExtension_BarrelOffsetPair> Barrels = new List<TurretDrawExtension_BarrelOffsetPair>();
    }

    public class TurretDrawExtension_BarrelOffsetPair
    {
        public Vector2 BarrelOffset;
        public Material BarrelMaterial;
        public GraphicData BarrelTexPath;
    }
}
