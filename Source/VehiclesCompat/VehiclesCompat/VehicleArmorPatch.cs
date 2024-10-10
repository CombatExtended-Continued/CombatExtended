using System;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using Vehicles;
using UnityEngine;
using CombatExtended;


namespace CombatExtended.Compatibility.VehiclesCompat
{
    [HarmonyPatch(typeof(VehicleStatHandler),
                  nameof(VehicleStatHandler.ApplyDamage))]
    public static class VehicleArmorPatch
    {
        public static bool Prefix(VehicleStatHandler __instance, ref DamageInfo dinfo, IntVec2 hitCell)
        {
            if (!Controller.settings.patchArmorDamage)
            {
                return true;
            }
            StringBuilder report = VehicleMod.settings.debug.debugLogging ? new StringBuilder() : null;
            __instance.ApplyDamageCE(ref dinfo, hitCell, report);
            return false;
        }

        /*
          Posible States:
          Projectile Outside, hitting outer layer, deflected -- end here, apply secondary damage as necessary (sharp -> blunt)
          Projectile Outside, hitting outer layer, penetrated -- hit inner layer on same cell, projectile is now inside
          Projectile Outside, there is no outer layer --  projectile is now inside
          Projectile Inside, hitting inner layer, deflected -- hit outer layer on same cell, projectile is still inside
          Projectile Inside, hitting inner layer, penetrated -- hit inner layer on next cell, projectile is still inside
          Projectile Inside, hitting outer layer, deflected -- hit inner layer on /next/ cell, projectile is still inside
          Projectile Inside, hitting outer layer, penetrated -- projectile exits the vehicle and damage stops
          Projectile Inside, is no inner component -- hit outer component on same cell
          Projectile Inside, is no inner component, is no outer component -- hit outer component on previous cell, direction reversed
          TODO:  Allow non-linear deflections.
         */
        public static void ApplyDamageCE(this VehicleStatHandler stats, ref DamageInfo dinfo, IntVec2 hitCell, StringBuilder report)
        {
            DamageDef def = dinfo.Def;
            VehiclePawn vehicle = stats.vehicle;
            float damage = dinfo.Amount;
            if (!def.harmsHealth)
            {
                damage = 0; //Don't apply damage to vehicles if the damage def isn't supposed to harm
            }
            report?.AppendLine("-- DAMAGE REPORT --");
            report?.AppendLine($"Base Damage: {damage}");
            report?.AppendLine($"DamageDef: {dinfo.Def}");
            report?.AppendLine($"HitCell: {hitCell}");

            damage = AdjustDamage(damage, dinfo, report);

            if (damage <= 0)
            {
                report?.AppendLine($"Final Damage = {damage}. Exiting.");
                return;
            }

            dinfo.SetAmount(damage);
            float maxLength = vehicle.VehicleDef.Size.x + vehicle.VehicleDef.Size.z;
            Vector3 direction = new Vector3(Mathf.Sin(Mathf.Deg2Rad * dinfo.Angle) * maxLength, 0, Mathf.Cos(Mathf.Deg2Rad * dinfo.Angle) * maxLength);
            if (vehicle.Rotation == Rot4.East)
            {
                direction = new Vector3(-direction.z, 0, -direction.x);
            }
            if (vehicle.Rotation == Rot4.West)
            {
                direction = new Vector3(direction.z, 0, direction.x);
            }
            IntVec3 hc3 = new IntVec3(hitCell.x, 0, hitCell.z);
            Vector3 position = hc3.ToVector3();
            IntVec3 maxPosition = (position + direction).ToIntVec3();
            var cells = GenSight.PointsOnLineOfSight(hc3, maxPosition)
                .Union(new[] { hc3, maxPosition }).Distinct().OrderBy(x => (x.ToVector3Shifted() - position).MagnitudeHorizontalSquared()).ToList();


            report?.AppendLine($"Cells: {string.Join(",", cells)}, count: {cells.Count}");
            var isSharp = dinfo.Def.armorCategory.armorRatingStat == StatDefOf.ArmorRating_Sharp;
            bool inside = false;
            int cdirection = 1;
            int stop = cells.Count;
            int cidx = 0;
            VehicleComponent.Penetration penetration = VehicleComponent.Penetration.Deflected;
            VehicleComponent.VehiclePartDepth hitDepth = VehicleComponent.VehiclePartDepth.External;
            int hcount = 0;

            while (cidx < stop && cidx > -1)
            {
                if (++hcount > 100)
                {
                    report?.AppendLine($"Terminating damage due to excessive hits");
                    break;
                }
                if (dinfo.Amount <= 0)
                {
                    report?.AppendLine("Done");
                    break;
                }
                var cell = cells[cidx];
                var cell2 = new IntVec2(cell.x, cell.z);
                report?.AppendLine($"Damaging = {cell2}");
                if (VehicleMod.settings.debug.debugDrawHitbox)
                {
                    IntVec2 renderCell = cell2;
                    if (vehicle.Rotation != Rot4.North)
                    {
                        renderCell = renderCell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size, reverseRotate: true);
                    }
                    stats.debugCellHighlight.Add(new Pair<IntVec2, int>(renderCell, VehicleStatHandler.TicksHighlighted));
                }
                if (stats.componentLocations.TryGetValue(cell2, out List<VehicleComponent> components))
                {
                    bool penetrated = TryPenetrateComponents(stats, ref dinfo, components, hitDepth, report);
                    report?.AppendLine($"penetrated? {penetrated}");
                    if (!inside) // hit outside
                    {
                        report?.AppendLine($"Outside");
                        if (penetrated)
                        {
                            report?.AppendLine($"Punching through");
                            inside = true;
                            hitDepth = VehicleComponent.VehiclePartDepth.Internal;
                            continue; // Hit same cell again, this time on the inside
                        }
                        else
                        {
                            // If we're sharp, switch to blunt, otherwise we're done.
                            if (isSharp)
                            {
                                report?.AppendLine($"Switching to blunt");
                                float bluntPen = 0;
                                if (dinfo.Weapon?.projectile is ProjectilePropertiesCE projectile)
                                {
                                    bluntPen = projectile.armorPenetrationBlunt;
                                }
                                dinfo.armorPenetrationInt = bluntPen;
                                dinfo.Def = DamageDefOf.Blunt;
                                continue; // Hit same cell again, as blunt
                            }
                            report?.AppendLine($"Bouncing Off");
                            break; // Notify impact, recalculate health, and return;
                        }
                    }
                    else // we're already inside
                    {
                        if (penetrated)
                        {
                            if (hitDepth == VehicleComponent.VehiclePartDepth.External) // Blew a hole out, we're done
                            {
                                report?.AppendLine($"Exiting vehicle");
                                break;
                            }
                            else // Hit any pawns and then move on to the next cell
                            {
                                if (stats.HitPawn(dinfo, hitDepth, cell2, stats.DirectionFromAngle(dinfo.Angle), out Pawn hitPawn))
                                {
                                    report?.AppendLine($"Hit {hitPawn} for {dinfo.Amount}. Impact site = {hitCell}");
                                }
                                report?.AppendLine($"Moving to next cell");
                                cidx += cdirection;
                                continue;
                            }
                        }
                        else // We bounced off
                        {
                            if (hitDepth == VehicleComponent.VehiclePartDepth.External) // Bounced off the inside of the armor, move to the next cell, inside
                            {
                                report?.AppendLine($"Bouncing off inside of armor");
                                hitDepth = VehicleComponent.VehiclePartDepth.Internal;
                                cidx += cdirection;
                                continue;
                            }
                            else // bounced off an internal component such as an engine block, hit the inside of the armor in *this* cell.
                            {
                                report?.AppendLine($"Bouncing off internal components");
                                hitDepth = VehicleComponent.VehiclePartDepth.External;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (inside)
                    {
                        report?.AppendLine($"No more components, reversing direction and hitting outside");
                        hitDepth = VehicleComponent.VehiclePartDepth.External;
                        cdirection *= -1;
                        cidx += cdirection;
                        continue;
                    }
                }
            }

            vehicle.Notify_DamageImpact(new VehicleComponent.DamageResult()
            {
                penetration = penetration,
                damageInfo = dinfo,
                cell = hitCell
            });

            stats.RecalculateHealthPercent();

        }

        public static bool TryPenetrateComponents(VehicleStatHandler stats, ref DamageInfo dinfo, List<VehicleComponent> components, VehicleComponent.VehiclePartDepth hitDepth, StringBuilder report)
        {
            var componentsAtHitDepth = components.Where(comp => comp.Depth == hitDepth && comp.HealthPercent > 0).OrderBy(x => Rand.Value * x.props.hitWeight);
            report?.AppendLine($"components=({string.Join(",", components.Select(c => c.props.label))})");
            report?.AppendLine($"hitDepth = {hitDepth}");
            report?.AppendLine($"components at hitDepth {hitDepth}: ({string.Join(",", componentsAtHitDepth.Select(comp => comp.props.label))})");
            foreach (var component in componentsAtHitDepth)
            {
                report?.AppendLine($"Hitting Component {component.props.label}");
                if (HitComponent(component, ref dinfo, report))
                {
                    report?.AppendLine($"Component did not stop projectile");
                    return false;
                }
                if (hitDepth == VehicleComponent.VehiclePartDepth.Internal) // only hit one component per cell internally.
                {
                    break;
                }
            }
            return true;
        }

        public static float DamageComponent(VehicleComponent component, float damageAmount, DamageInfo dinfo, VehicleComponent.Penetration pen)
        {
            float overdamage = 0f;
            if (damageAmount > component.health)
            {
                overdamage = damageAmount - component.health;
                component.health = 0f;
            }
            else
            {
                component.health -= damageAmount;
            }

            if (!component.props.reactors.NullOrEmpty())
            {
                foreach (Reactor reactor in component.props.reactors)
                {
                    reactor.Hit(component.vehicle, component, ref dinfo, pen);
                }
            }

            component.vehicle.EventRegistry[VehicleEventDefOf.DamageTaken].ExecuteEvents();
            if (component.vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) <= 0.1f)
            {
                component.vehicle.ignition.Drafted = false;
            }
            if (component.vehicle.Spawned && component.vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity) < 0.01f)
            {
                component.vehicle.Kill(dinfo);
            }
            return overdamage;
        }

        public static bool HitComponent(VehicleComponent component, ref DamageInfo dinfo, StringBuilder report)
        {
            report?.AppendLine($"Applying Damage = {dinfo.Amount} to {component.props.key}");

            DamageArmorCategoryDef armorCategoryDef = dinfo.Def.armorCategory;
            float armorAmount = component.Efficiency * component.ArmorRating(armorCategoryDef, out _);
            float penAmount = dinfo.ArmorPenetrationInt;
            var dmgAmount = dinfo.Amount;
            var isSharp = dinfo.Def.armorCategory.armorRatingStat == StatDefOf.ArmorRating_Sharp;
            float armorDamage = 0;

            bool deflected = armorAmount > 0 && DamageArmor(isSharp, armorAmount, ref penAmount, ref dmgAmount, out armorDamage);
            report?.AppendLine($"deflected by component armor? {deflected}");
            report?.AppendLine($"Armor Damage: {armorDamage}");
            dmgAmount += DamageComponent(component, armorDamage, dinfo, deflected ? VehicleComponent.Penetration.Diminished : VehicleComponent.Penetration.Penetrated);

            if (!deflected)
            {
                report?.AppendLine($"component health? {component.health}");
                deflected = DamageArmor(isSharp, component.health / 50, ref penAmount, ref dmgAmount, out armorDamage);
                report?.AppendLine($"deflected by component bulk? {deflected}");
                report?.AppendLine($"Component Damage: {armorDamage}");
                dmgAmount += DamageComponent(component, armorDamage, dinfo, VehicleComponent.Penetration.Penetrated);
            }
            dinfo.SetAmount(dmgAmount);
            dinfo.armorPenetrationInt = penAmount;
            report?.AppendLine($"Fallthrough Damage = {dinfo.Amount}");
            return deflected;
        }

        public static bool DamageArmor(bool isSharp, float armorAmount, ref float penAmount, ref float dmgAmount, out float armorDamage)
        {
            var newPenAmount = penAmount - armorAmount;
            bool deflected = armorAmount > penAmount;
            var dmgMult = penAmount == 0 ? 1 : Mathf.Clamp01(newPenAmount / penAmount);
            if (dmgMult == 0)
            {
                deflected = true;
            }
            var newDmgAmount = dmgAmount * dmgMult;
            var blockedDamage = dmgAmount - newDmgAmount;
            if (!isSharp && (penAmount / armorAmount) < 0.5f)
            {
                armorDamage = 0f;
                deflected = true;
            }
            else
            {
                armorDamage = (blockedDamage) * Mathf.Min(1.0f, (penAmount * penAmount) / (armorAmount * armorAmount)) + newDmgAmount * Mathf.Clamp01(armorAmount / penAmount);
            }
            penAmount = newPenAmount;
            dmgAmount = newDmgAmount;
            return deflected;
        }

        public static float AdjustDamage(float damage, DamageInfo dinfo, StringBuilder report)
        {
            if (dinfo.Weapon?.GetModExtension<VehicleDamageMultiplierDefModExtension>() is VehicleDamageMultiplierDefModExtension weaponMultiplier)
            {
                damage *= weaponMultiplier.multiplier;
                report?.AppendLine($"ModExtension Multiplier: {weaponMultiplier.multiplier} Result: {damage}");
            }
            if (dinfo.Instigator?.def.GetModExtension<VehicleDamageMultiplierDefModExtension>() is VehicleDamageMultiplierDefModExtension defMultiplier)
            {
                damage *= defMultiplier.multiplier;
                report?.AppendLine($"ModExtension Multiplier: {defMultiplier.multiplier} Result: {damage}");
            }
            if (dinfo.Def.isRanged)
            {
                damage *= VehicleMod.settings.main.rangedDamageMultiplier;
                report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.rangedDamageMultiplier} Result: {damage}");
            }
            else if (dinfo.Def.isExplosive)
            {
                damage *= VehicleMod.settings.main.explosiveDamageMultiplier;
                report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.explosiveDamageMultiplier} Result: {damage}");
            }
            else
            {
                damage *= VehicleMod.settings.main.meleeDamageMultiplier;
                report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.meleeDamageMultiplier} Result: {damage}");
            }
            return damage;
        }
    }
}
