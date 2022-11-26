﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public abstract class StatPart_StatSelect : StatPart
    {
        public bool includeWeapons;

        public StatDef weaponStat = null;

        public StatDef apparelStat;

        public bool sumApparelsStat = false;

        public StatDef implantStat = null;

        public bool sumImplantsStat = true;

        private Dictionary<EquipmentStatKey, float> cachedStats = new Dictionary<EquipmentStatKey, float>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetEquipmentStat(ThingWithComps equipment, StatDef stat)
        {
            EquipmentStatKey key = new EquipmentStatKey(equipment);

            return cachedStats.TryGetValue(key, out float value) ?
                   value : cachedStats[key] = equipment.GetStatValue(stat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetAddedPartStat(Hediff_AddedPart addedPart)
        {
            return addedPart.def.spawnThingOnRemoved.GetStatValueAbstract(implantStat);
        }

        protected abstract float Select(float first, float second);

        public override void TransformValue(StatRequest req, ref float result)
        {
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                if (apparelStat != null && pawn.apparel != null)
                {
                    if (sumApparelsStat)
                    {
                        float value = 0;
                        ThingOwner<Apparel> wornApparel = pawn.apparel.wornApparel;

                        for (int i = 0; i < wornApparel.Count; i++)
                        {
                            value += GetEquipmentStat(wornApparel[i], apparelStat);
                        }

                        result = Select(value, result);
                    }
                    else
                    {
                        ThingOwner<Apparel> wornApparel = pawn.apparel.wornApparel;

                        for (int i = 0; i < wornApparel.Count; i++)
                        {
                            result = Select(result, GetEquipmentStat(wornApparel[i], apparelStat));
                        }
                    }
                }
                if (weaponStat != null && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    result = Select(result, GetEquipmentStat(pawn.equipment.Primary, weaponStat));
                }
                if (implantStat != null)
                {
                    if (sumImplantsStat)
                    {
                        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                        float value = 0f;
                        for (int i = 0; i < hediffs.Count; i++)
                        {
                            if (hediffs[i] is Hediff_AddedPart addedpart && addedpart.def.spawnThingOnRemoved != null)
                            {
                                value += GetAddedPartStat(addedpart);
                            }
                        }
                        result = Select(value, result);
                    }
                    else
                    {
                        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                        for (int i = 0; i < hediffs.Count; i++)
                        {
                            if (hediffs[i] is Hediff_AddedPart addedpart && addedpart.def.spawnThingOnRemoved != null)
                            {
                                result = Select(result, GetAddedPartStat(addedpart));
                            }
                        }
                    }
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing && req.Pawn != null)
            {
                return "";
            }
            return null;
        }
    }
}
