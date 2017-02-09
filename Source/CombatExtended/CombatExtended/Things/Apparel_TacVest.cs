using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_TacVest : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            float offset = 0.035f;
            bool hasOnSkin = false;
            bool hasMiddle = false;
            bool hasShell = false;
            for(int i = 0; i < wearer.apparel.WornApparel.Count && !(hasOnSkin && hasMiddle && hasShell); i++)
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
            if (rotation == Rot4.North)
            {
                offset += hasShell ? 0.03f : -0.015f;
            }
            return offset;
        }
    }
}
