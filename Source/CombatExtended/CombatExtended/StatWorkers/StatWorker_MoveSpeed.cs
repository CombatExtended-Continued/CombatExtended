using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class StatWorker_MoveSpeed : StatWorker
    {
        private const float CrouchWalkFactor = 0.67f;   // The factor to apply when crouch-walking

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetExplanationUnfinalized(req, numberSense));
            if (req.HasThing)
            {
                CompInventory inventoryComp = req.Thing.TryGetComp<CompInventory>();
                if (inventoryComp != null)
                {
                    stringBuilder.AppendLine();
                    if (this.stat.defName != "MeleeDodgeChance")
                    {
                        stringBuilder.AppendLine("CE_CarriedWeight".Translate() + ": x" + inventoryComp.moveSpeedFactor.ToStringPercent());
                    }
                    else
                    {
                        stringBuilder.AppendLine("CE_CarriedWeight".Translate() + ": x" + MassBulkUtility.DodgeWeightFactor(inventoryComp.currentWeight, inventoryComp.capacityWeight).ToStringPercent());
                    }
                    if (inventoryComp.encumberPenalty > 0)
                    {
                        stringBuilder.AppendLine("CE_Encumbered".Translate() + ": -" + inventoryComp.encumberPenalty.ToStringPercent());
                        stringBuilder.AppendLine("CE_FinalModifier".Translate() + ": x" + GetStatFactor(req.Thing).ToStringPercent());
                    }
                }

                var suppressComp = req.Thing.TryGetComp<CompSuppressable>();
                if (suppressComp != null && suppressComp.IsCrouchWalking)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine(
                        $"{"CE_CrouchWalking".Translate()}: x{CrouchWalkFactor.ToStringPercent()}");
                }
                if (this.stat.defName == "MeleeDodgeChance")
                {
                    stringBuilder.AppendLine("CE_BulkEffect".Translate() + " x" + (MassBulkUtility.DodgeChanceFactor(inventoryComp.currentBulk, inventoryComp.capacityBulk) * 100f).ToString() + "%");
                }
            }

            return stringBuilder.ToString();
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            float value = base.GetValueUnfinalized(req, applyPostProcess);
            if (req.HasThing)
            {
                value *= GetStatFactor(req.Thing);


                var inventory = req.Thing.TryGetComp<CompInventory>();
                if (this.stat.defName == "MeleeDodgeChance" && inventory != null)
                {
                    value *= MassBulkUtility.DodgeChanceFactor(inventory.currentBulk, inventory.capacityBulk);
                    value *= MassBulkUtility.DodgeWeightFactor(inventory.currentWeight, inventory.capacityWeight);
                }
            }
            return value;
        }

        private float GetStatFactor(Thing thing)
        {
            float factor = 1f;

            // Apply inventory penalties
            CompInventory inventory = thing.TryGetComp<CompInventory>();
            if (inventory != null)
            {
                factor = Mathf.Clamp(inventory.moveSpeedFactor - inventory.encumberPenalty, 0.5f, 1f);
            }

            // Apply crouch walk penalty
            var suppressComp = thing.TryGetComp<CompSuppressable>();
            if (suppressComp?.IsCrouchWalking ?? false)
            {
                factor *= CrouchWalkFactor;
            }

            return factor;
        }
    }
}
