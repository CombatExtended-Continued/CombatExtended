using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoThing : ThingWithComps
    {
        AmmoDef ammoDef { get { return def as AmmoDef; } }

        public override string GetDescription()
        {
            if(ammoDef != null && ammoDef.ammoClass != null && ammoDef.linkedProjectile != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(base.GetDescription());

                // Append ammo class description
                if (!string.IsNullOrEmpty(ammoDef.ammoClass.description))
                    stringBuilder.AppendLine("\n" + 
                        (string.IsNullOrEmpty(ammoDef.ammoClass.LabelCap) ? "" : ammoDef.ammoClass.LabelCap + ":\n") + 
                        ammoDef.ammoClass.description);

                // Append ammo stats
                ProjectilePropertiesCE props = ammoDef.linkedProjectile.projectile as ProjectilePropertiesCE;
                if (props != null)
                {
                    // Damage type/amount
                    stringBuilder.AppendLine("\n" + "CE_DescDamage".Translate() + ": ");
                    stringBuilder.AppendLine("   " + GenText.ToStringByStyle(props.damageAmountBase, ToStringStyle.Integer) + " (" + props.damageDef.LabelCap + ")");
                    if (!props.secondaryDamage.NullOrEmpty())
                    {
                        foreach(SecondaryDamage sec in props.secondaryDamage)
                        {
                            stringBuilder.AppendLine("   " + GenText.ToStringByStyle(sec.amount, ToStringStyle.Integer) + " (" + sec.def.LabelCap + ")");
                        }
                    }
                    // Explosion radius
                    if (props.explosionRadius > 0)
                        stringBuilder.AppendLine("CE_DescExplosionRadius".Translate() + ": " + GenText.ToStringByStyle(props.explosionRadius, ToStringStyle.FloatOne));

                    // Secondary explosion
                    CompProperties_ExplosiveCE secExpProps = ammoDef.linkedProjectile.GetCompProperties<CompProperties_ExplosiveCE>();
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
                }

                return stringBuilder.ToString();
            }
            return base.GetDescription();
        }
    }
}
