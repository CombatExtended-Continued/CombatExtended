using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended
{
    public class AmmoCategoryDef : Def
    {
        public bool advanced = false;                

        public bool isArmorPiercing = false;

        public bool isHollowPoint = false;

        public bool isFullMetalJacket = false;

        [Unsaved(false)]
        public bool projectileOfUnknownType = false;

        public string labelShort;

        public string LabelCapShort => labelShort.CapitalizeFirst();

        public override void PostLoad()
        {
            base.PostLoad();
            if(description != null && !isArmorPiercing && !isHollowPoint && !isFullMetalJacket)
            {                
                string desc = description.ToLower() + " " + label.ToLower() + " " + labelShort;

                isArmorPiercing = (desc.Contains("armor") && desc.Contains("piercing")) || desc.Contains("sabot") || desc.Contains("concentrated") || desc.Contains("AP");
                if (isArmorPiercing)
                {
                    Log.Warning($"CE: {this.defName} has isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo all set to false. CE will guess the ammo category field values for isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo fields. please set one of these to to be as close as to your ammo types using it. if your ammo doesn't match any of the first 3 set isAbnormalAmmo to true");
                    return;
                }
                isHollowPoint = (desc.Contains("hollow") && desc.Contains("point")) || desc.Contains("HP");
                if (isHollowPoint)
                {
                    Log.Warning($"CE: {this.defName} has isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo all set to false. CE will guess the ammo category field values for isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo fields. please set one of these to to be as close as to your ammo types using it. if your ammo doesn't match any of the first 3 set isAbnormalAmmo to true");
                    return;
                }
                isFullMetalJacket = (desc.Contains("metal") && desc.Contains("jacket")) || desc.Contains("balance") || desc.Contains("FMJ");
                if (isFullMetalJacket)
                {
                    Log.Warning($"CE: {this.defName} has isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo all set to false. CE will guess the ammo category field values for isArmorPiercing, isHollowPoint, isFullMetalJacket, isAbnormalAmmo fields. please set one of these to to be as close as to your ammo types using it. if your ammo doesn't match any of the first 3 set isAbnormalAmmo to true");
                    return;
                }                
                projectileOfUnknownType = true;
            }
        }        
    }
}
