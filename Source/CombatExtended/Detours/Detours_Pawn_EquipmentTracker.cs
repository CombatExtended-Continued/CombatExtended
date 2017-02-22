using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_Pawn_EquipmentTracker
    {
        private const BindingFlags UniversalBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo pawnFieldInfo = typeof(Pawn_EquipmentTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo primaryIntFieldInfo = typeof(Pawn_EquipmentTracker).GetField("primaryInt", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo primaryPropertyInfo = typeof(Pawn_EquipmentTracker).GetProperty("Primary", UniversalBindingFlags);


        internal static void AddEquipment(this Pawn_EquipmentTracker _this, ThingWithComps newEq)
        {
            SlotGroupUtility.Notify_TakingThing(newEq);

            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            ThingWithComps primaryInt = (ThingWithComps)primaryIntFieldInfo.GetValue(_this);

            if ((from eq in _this.AllEquipment
                 where eq.def == newEq.def
                 select eq).Any<ThingWithComps>())
            {
                Log.Error(string.Concat(new object[]
                {
                    "Pawn ",
                    pawn.LabelCap,
                    " got equipment ",
                    newEq,
                    " while already having it."
                }));
                return;
            }
            if (newEq.def.equipmentType == EquipmentType.Primary && _this.Primary != null)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Pawn ",
                    pawn.LabelCap,
                    " got primaryInt equipment ",
                    newEq,
                    " while already having primaryInt equipment ",
                    _this.Primary
                }));
                return;
            }
            if (newEq.def.equipmentType == EquipmentType.Primary)
            {
                primaryPropertyInfo.SetValue(_this, newEq, null); // Changed assignment to SetValue() since we're fetching a private variable through reflection
            }
            else
            {
                Log.Error("Tried to equip " + newEq + " but it's not Primary. Secondary weapons are not supported.");
            }
            foreach (Verb current in newEq.GetComp<CompEquippable>().AllVerbs)
            {
                current.caster = pawn;
                current.Notify_PickedUp();
            }
            CE_Utility.TryUpdateInventory(pawn);   // Added equipment, update inventory
        }

        internal static void Notify_PrimaryDestroyed(this Pawn_EquipmentTracker _this)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            _this.Remove(_this.Primary);
            if (pawn.Spawned)
            {
                pawn.stances.CancelBusyStanceSoft();
            }

            CE_Utility.TryUpdateInventory(pawn);   // Equipment was destroyed, update inventory

            // Try switching to the next available weapon
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory != null)
                inventory.SwitchToNextViableWeapon(false);
        }

        internal static bool TryDropEquipment(this Pawn_EquipmentTracker _this, ThingWithComps eq, out ThingWithComps resultingEq, IntVec3 pos, bool forbid = true)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);

            if (!_this.AllEquipment.Contains(eq))
            {
                Log.Warning(pawn.LabelCap + " tried to drop equipment they didn't have: " + eq);
                resultingEq = null;
                return false;
            }
            if (!pos.IsValid)
            {
                Log.Error(string.Concat(new object[]
                {
                    pawn,
                    " tried to drop ",
                    eq,
                    " at invalid cell."
                }));
                resultingEq = null;
                return false;
            }
            _this.Remove(eq);
            Thing thing = null;
            bool result = GenThing.TryDropAndSetForbidden(eq, pos, pawn.MapHeld, ThingPlaceMode.Near, out thing, forbid);
            resultingEq = (thing as ThingWithComps);
            CE_Utility.TryUpdateInventory(pawn);       // Dropped equipment, update inventory

            // Cancel current job (use verb, etc.)
            if (pawn.Spawned)
                pawn.stances.CancelBusyStanceSoft();

            return result;
        }

        internal static bool TryTransferEquipmentToContainer(this Pawn_EquipmentTracker _this, ThingWithComps eq, ThingContainer container, out ThingWithComps resultingEq)
        {
            // Fetch private fields
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            if (!_this.AllEquipment.Contains(eq))
            {
                Log.Warning(pawn.LabelCap + " tried to transfer equipment he didn't have: " + eq);
                resultingEq = null;
                return false;
            }
            // Need to drop stacking equipment (grenades and such) as a workaround for vanilla bug
            ThingWithComps oldEq = eq;
            if(eq.def.stackLimit > 1)
            {
                _this.TryDropEquipment(eq, out oldEq, pawn.Position);
            }
            if (container.TryAdd(oldEq, true))
            {
                resultingEq = null;
            }
            else
            {
                resultingEq = eq;
            }
            _this.Remove(eq);
            CE_Utility.TryUpdateInventory(pawn);   // Equipment was stored away, update inventory

            // Cancel current job (use verb, etc.)
            if (pawn.Spawned)
                pawn.stances.CancelBusyStanceSoft();

            return resultingEq == null;
        }

        internal static bool TryStartAttack(this Pawn_EquipmentTracker _this, LocalTargetInfo targ)
        {
            Pawn pawn = (Pawn)pawnFieldInfo.GetValue(_this);
            if (pawn.stances.FullBodyBusy)
            {
                return false;
            }
            if (pawn.story != null && pawn.story.DisabledWorkTags.Contains(WorkTags.Violent))
            {
                return false;
            }
            bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = pawn.TryGetAttackVerb(allowManualCastWeapons);

            // Check for reload before attacking
            if (_this.PrimaryEq != null && verb != null && verb == _this.PrimaryEq.PrimaryVerb)
            {
                if (_this.Primary != null)
                {
                    CompAmmoUser compAmmo = _this.Primary.TryGetComp<CompAmmoUser>();
                    if (compAmmo != null)
                    {
                        if (!compAmmo.hasMagazine)
                        {
                            if (compAmmo.useAmmo && !compAmmo.hasAmmo)
                                return false;
                        }
                        else if (compAmmo.curMagCount <= 0)
                        {
                            compAmmo.TryStartReload();
                            return false;
                        }
                    }
                }
            }
            return verb != null && verb.TryStartCastOn(targ, false, true);
        }
    }
}