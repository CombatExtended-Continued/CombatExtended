using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public static class AmmoUtility
    {
        /// <summary>
        /// Generates a readout text for a projectile with the damage amount, type, secondary explosion and other CE stats for display in info-box
        /// </summary>
        /// <param name="projectileDef">The projectile's ThingDef</param>
        /// <returns>Formatted string listing projectile stats</returns>
        public static string GetProjectileReadout(this ThingDef projectileDef)
        {
            // Append ammo stats
            ProjectilePropertiesCE props = projectileDef?.projectile as ProjectilePropertiesCE;
            if (props == null)
            {
                Log.Error("CE tried getting projectile readout with null props");
                return "";
            }

            StringBuilder stringBuilder = new StringBuilder();

            // Damage type/amount
            string dmgList = "CE_DescDamage".Translate() + ": ";
            if (!props.secondaryDamage.NullOrEmpty())
            {
                // If we have multiple damage types, put every one in its own line
                stringBuilder.AppendLine(dmgList);
                stringBuilder.AppendLine("   " + GenText.ToStringByStyle(props.damageAmountBase, ToStringStyle.Integer) + " (" + props.damageDef.LabelCap + ")");
                foreach (SecondaryDamage sec in props.secondaryDamage)
                {
                    stringBuilder.AppendLine("   " + GenText.ToStringByStyle(sec.amount, ToStringStyle.Integer) + " (" + sec.def.LabelCap + ")");
                }
            }
            else
            {
                stringBuilder.AppendLine(dmgList + GenText.ToStringByStyle(props.damageAmountBase, ToStringStyle.Integer) + " (" + props.damageDef.LabelCap + ")");
            }
            // Explosion radius
            if (props.explosionRadius > 0)
                stringBuilder.AppendLine("CE_DescExplosionRadius".Translate() + ": " + GenText.ToStringByStyle(props.explosionRadius, ToStringStyle.FloatOne));

            // Secondary explosion
            CompProperties_ExplosiveCE secExpProps = projectileDef.GetCompProperties<CompProperties_ExplosiveCE>();
            if (secExpProps != null)
            {
                if (secExpProps.explosionRadius > 0)
                {
                    stringBuilder.AppendLine("CE_DescSecondaryExplosion".Translate() + ":");
                    stringBuilder.AppendLine("   " + "CE_DescExplosionRadius".Translate() + ": " + GenText.ToStringByStyle(secExpProps.explosionRadius, ToStringStyle.FloatOne));
                    stringBuilder.AppendLine("   " + "CE_DescDamage".Translate() + ": " +
                        GenText.ToStringByStyle(secExpProps.explosionDamage, ToStringStyle.Integer) + " (" + secExpProps.explosionDamageDef.LabelCap + ")");
                }
                if (secExpProps.fragRange > 0)
                    stringBuilder.AppendLine("CE_DescFragRange".Translate() + ": " + GenText.ToStringByStyle(secExpProps.fragRange, ToStringStyle.FloatTwo));
            }

            // CE stats
            stringBuilder.AppendLine("CE_DescArmorPenetration".Translate() + ": " + GenText.ToStringByStyle(props.armorPenetration, ToStringStyle.PercentOne));
            if (props.pelletCount > 1)
                stringBuilder.AppendLine("CE_DescPelletCount".Translate() + ": " + GenText.ToStringByStyle(props.pelletCount, ToStringStyle.Integer));
            if (props.spreadMult != 1)
                stringBuilder.AppendLine("CE_DescSpreadMult".Translate() + ": " + GenText.ToStringByStyle(props.spreadMult, ToStringStyle.PercentZero));

            return stringBuilder.ToString();
        }
    }
}
