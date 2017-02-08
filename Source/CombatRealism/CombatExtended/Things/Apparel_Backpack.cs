using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_Backpack : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return 0.1f;
            }
            float offset = 0.0375f;
            bool hasOnSkin = false;
            bool hasMiddle = false;
            bool hasShell = false;
            for (int i = 0; i < wearer.apparel.WornApparel.Count && !(hasOnSkin && hasMiddle && hasShell); i++)
            {
                switch (wearer.apparel.WornApparel[i].def.apparel.LastLayer)
                {
                    case ApparelLayer.OnSkin:
                        if (!hasOnSkin)
                        {
                            offset += 0.005f;
                            hasOnSkin = true;
                        }
                        break;
                    case ApparelLayer.Middle:
                        if (!hasMiddle)
                        {
                            offset += 0.005f;
                            hasMiddle = true;
                        }
                        break;
                    case ApparelLayer.Shell:
                        if (!hasShell)
                        {
                            offset += 0.005f;
                            hasShell = true;
                        }
                        break;
                }
            }
            return offset;
        }
    }
}
