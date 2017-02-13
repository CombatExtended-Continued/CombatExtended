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
        private const float YOffsetInterval_Clothes = 0.005f; // copy-pasted from PawnRenderer

        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            float offset = 0.006f;
            /*
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
                            offset += 0.006f;
                            hasMiddle = true;
                        }
                        break;
                    case ApparelLayer.Shell:
                        if (!hasShell)
                        {
                            offset += 0.0f;
                            hasShell = true;
                        }
                        break;
                }
            }
            if (hasShell)
            {
                offset += rotation == Rot4.North ? 0.018f : 0.014f;
            }*/
            List<Material> matList = wearer.Drawer.renderer.graphics.MatsBodyBaseAt(rotation);
            foreach(Material mat in matList)
            {
                offset += YOffsetInterval_Clothes;
            }
            Log.Message("CE :: Increased offset for " + wearer.ToString() + " by " + offset.ToString());
            return offset;
        }
    }
}
