using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public static class NonSnapAttackTargetFinder
    {
        private const float FriendlyFireScoreOffsetPerHumanlikeOrMechanoid = 18f;

        private const float FriendlyFireScoreOffsetPerAnimal = 7f;

        private const float FriendlyFireScoreOffsetPerNonPawn = 10f;

        private const float FriendlyFireScoreOffsetSelf = 40f;

        private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>(128);

        private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();

        private static List<float> tmpTargetScores = new List<float>();

        private static List<bool> tmpCanShootAtTarget = new List<bool>();

        public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, TargetScanFlags flags, Vector3 angle, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
        {
            Verb verb = searcher.CurrentEffectiveVerb;
            if (verb == null)
            {
                Log.Error("BestShootTargetFromCurrentPosition with " + searcher.ToStringSafe() + " who has no attack verb.");
                return null;
            }
            return BestAttackTarget(searcher, flags, angle, validator, Mathf.Max(minDistance, verb.verbProps.minRange), Mathf.Min(maxDistance, verb.verbProps.range));
        }

        public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Vector3 angle, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f)
        {
            Thing searcherThing = searcher.Thing;
            Verb verb = searcher.CurrentEffectiveVerb;
            if (verb == null)
            {
                Log.Error("BestAttackTarget with " + searcher.ToStringSafe() + " who has no attack verb.");
                return null;
            }
            bool onlyTargetMachines = verb.IsEMP();
            float minDistSquared = minDist * minDist;
            float num = verb.verbProps.range;
            float maxLocusDistSquared = num * num;
            Func<IntVec3, bool> losValidator = null;
            if ((flags & TargetScanFlags.LOSBlockableByGas) != 0)
            {
                losValidator = (IntVec3 vec3) => !vec3.AnyGas(searcherThing.Map, GasType.BlindSmoke);
            }



            tmpTargets.Clear();
            tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
            tmpTargets.RemoveAll((IAttackTarget t) => ShouldIgnoreNoncombatant(searcherThing, t, flags));
            bool flag = false;
            for (int i = 0; i < tmpTargets.Count; i++)
            {
                IAttackTarget attackTarget = tmpTargets[i];
                if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
                {
                    flag = true;
                    break;
                }
            }
            IAttackTarget result;
            if (flag)
            {
                tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
                result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb, angle);
            }
            else
            {
                bool num2 = (flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != 0;
                bool flag2 = (flags & TargetScanFlags.NeedReachable) != 0;
                result = (IAttackTarget)GenClosest.ClosestThing_Global(validator: (!num2 || flag2) ? (Thing t) => innerValidator((IAttackTarget)t) : ((Predicate<Thing>)((Thing t) => innerValidator((IAttackTarget)t) && CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb))), center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);
            }
            tmpTargets.Clear();
            return result;

            bool innerValidator(IAttackTarget t)
            {
                Thing thing = t.Thing;
                if (t == searcher)
                {
                    return false;
                }

                if (minDistSquared > 0f && (searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
                {
                    return false;
                }

                float num3 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
                if (num3 > 0f && (searcherThing.Position - thing.Position).LengthHorizontalSquared < num3 * num3)
                {
                    return false;
                }

                if (!searcherThing.HostileTo(thing))
                {
                    return false;
                }

                if (validator != null && !validator(thing))
                {
                    return false;
                }

                if ((flags & TargetScanFlags.NeedNotUnderThickRoof) != 0)
                {
                    RoofDef roof = thing.Position.GetRoof(thing.Map);
                    if (roof != null && roof.isThickRoof)
                    {
                        return false;
                    }
                }
                if ((flags & TargetScanFlags.NeedLOSToAll) != 0)
                {
                    if (losValidator != null && (!losValidator(searcherThing.Position) || !losValidator(thing.Position)))
                    {
                        return false;
                    }

                    if (!searcherThing.CanSee(thing, losValidator))
                    {
                        if (t is Pawn)
                        {
                            if ((flags & TargetScanFlags.NeedLOSToPawns) != 0)
                            {
                                return false;
                            }
                        }
                        else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != 0)
                        {
                            return false;
                        }
                    }
                }
                if (((flags & TargetScanFlags.NeedThreat) != 0 || (flags & TargetScanFlags.NeedAutoTargetable) != 0) && t.ThreatDisabled(searcher))
                {
                    return false;
                }

                if ((flags & TargetScanFlags.NeedAutoTargetable) != 0 && !Verse.AI.AttackTargetFinder.IsAutoTargetable(t))
                {
                    return false;
                }

                if ((flags & TargetScanFlags.NeedActiveThreat) != 0 && !GenHostility.IsActiveThreatTo(t, searcher.Thing.Faction))
                {
                    return false;
                }

                Pawn pawn = t as Pawn;
                if (onlyTargetMachines && pawn != null && pawn.RaceProps.IsFlesh)
                {
                    return false;
                }

                if ((flags & TargetScanFlags.NeedNonBurning) != 0 && thing.IsBurning())
                {
                    return false;
                }

                if (searcherThing.def.race != null && (int)searcherThing.def.race.intelligence >= 2)
                {
                    CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
                    if (compExplosive != null && compExplosive.wickStarted)
                    {
                        return false;
                    }
                }
                if (thing.def.size.x == 1 && thing.def.size.z == 1)
                {
                    if (thing.Position.Fogged(thing.Map))
                    {
                        return false;
                    }
                }
                else
                {
                    bool flag3 = false;
                    foreach (IntVec3 item in thing.OccupiedRect())
                    {
                        if (!item.Fogged(thing.Map))
                        {
                            flag3 = true;
                            break;
                        }
                    }
                    if (!flag3)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static bool ShouldIgnoreNoncombatant(Thing searcherThing, IAttackTarget t, TargetScanFlags flags)
        {
            if (!(t is Pawn pawn))
            {
                return false;
            }

            if (pawn.IsCombatant())
            {
                return false;
            }

            if ((flags & TargetScanFlags.IgnoreNonCombatants) != 0)
            {
                return true;
            }

            if (GenSight.LineOfSightToThing(searcherThing.Position, pawn, searcherThing.Map))
            {
                return false;
            }

            return true;
        }

        private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            return verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false;
        }

        private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
        {
            if (GetAvailableShootingTargetsByScore(targets, searcher, verb, angle).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out var result))
            {
                return result.First;
            }

            return null;
        }

        private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
        {
            availableShootingTargets.Clear();
            if (rawTargets.Count == 0)
            {
                return availableShootingTargets;
            }
            tmpTargetScores.Clear();
            tmpCanShootAtTarget.Clear();
            float num = 0f;
            IAttackTarget attackTarget = null;

            for (int i = 0; i < rawTargets.Count; i++)
            {
                tmpTargetScores.Add(float.MinValue);
                tmpCanShootAtTarget.Add(item: false);
                if (rawTargets[i] == searcher)
                {
                    continue;
                }
                bool flag = CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
                tmpCanShootAtTarget[i] = flag;
                if (flag)
                {
                    float shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher, verb, angle);
                    tmpTargetScores[i] = shootingTargetScore;
                    if (attackTarget == null || shootingTargetScore > num)
                    {
                        attackTarget = rawTargets[i];
                        num = shootingTargetScore;
                    }
                }
            }
            for (int j = 0; j < rawTargets.Count; j++)
            {
                if (rawTargets[j] != searcher && tmpCanShootAtTarget[j])
                {
                    availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], tmpTargetScores[j]));
                }
            }
            return availableShootingTargets;
        }

        private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, Vector3 angle)
        {
            float num = 60f;
            num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, FriendlyFireScoreOffsetSelf);
            if (target.TargetCurrentlyAimingAt == searcher.Thing)
            {
                num += FriendlyFireScoreOffsetPerNonPawn;
            }
            if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
            {
                num += FriendlyFireScoreOffsetSelf;
            }
            num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
            if (target is Pawn pawn)
            {
                num -= NonCombatantScore(pawn);
                if (verb.verbProps.ai_TargetHasRangedAttackScoreOffset != 0f && pawn.CurrentEffectiveVerb != null && pawn.CurrentEffectiveVerb.verbProps.Ranged)
                {
                    num += verb.verbProps.ai_TargetHasRangedAttackScoreOffset;
                }
                if (pawn.Downed)
                {
                    num -= 50f;
                }
            }
            num += FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
            num += FriendlyFireConeTargetScoreOffset(target, searcher, verb);

            var ext = searcher.Thing.def.GetModExtension<NonSnapTurretExtension>();
            var anglef = Vector3.Angle(angle, (target.Thing.DrawPos - searcher.Thing.DrawPos).Yto0());
            if (ext == null)
            {
                if (anglef < 0.1f)
                {
                    anglef = 0.1f;
                }

                return num / anglef;
            }
            return ext.TweakWeight(num * target.TargetPriorityFactor, anglef);
        }

        private static float NonCombatantScore(Thing target)
        {
            if (!(target is Pawn pawn))
            {
                return 0f;
            }
            if (!pawn.IsCombatant())
            {
                return 50f;
            }
            if (pawn.DevelopmentalStage.Juvenile())
            {
                return 25f;
            }
            return 0f;
        }

        private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
            {
                return 0f;
            }
            Map map = target.Thing.Map;
            IntVec3 position = target.Thing.Position;
            int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = position + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map))
                {
                    continue;
                }
                bool flag = true;
                List<Thing> thingList = intVec.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (!(thingList[j] is IAttackTarget) || thingList[j] == target)
                    {
                        continue;
                    }
                    if (flag)
                    {
                        if (!GenSight.LineOfSight(position, intVec, map, skipFirstCell: true))
                        {
                            break;
                        }
                        flag = false;
                    }
                    float num3 = ((thingList[j] == searcher) ? FriendlyFireScoreOffsetSelf : ((!(thingList[j] is Pawn)) ? 10f : (thingList[j].def.race.Animal ? FriendlyFireScoreOffsetPerAnimal : FriendlyFireScoreOffsetPerHumanlikeOrMechanoid)));
                    num2 = ((!searcher.Thing.HostileTo(thingList[j])) ? (num2 - num3) : (num2 + num3 * 0.6f));
                }
            }
            return num2;
        }

        private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
        {
            if (!(searcher.Thing is Pawn pawn))
            {
                return 0f;
            }
            if ((int)pawn.RaceProps.intelligence < 1)
            {
                return 0f;
            }
            if (pawn.RaceProps.IsMechanoid)
            {
                return 0f;
            }
            if (!(verb is Verb_Shoot verb_Shoot))
            {
                return 0f;
            }
            ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
            if (defaultProjectile == null)
            {
                return 0f;
            }
            if (defaultProjectile.projectile.flyOverhead)
            {
                return 0f;
            }
            Map map = pawn.Map;
            ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
            float radius = Mathf.Max(VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.ForcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source), 1.5f);
            IEnumerable<IntVec3> enumerable = (from dest in GenRadial.RadialCellsAround(report.ShootLine.Dest, radius, useCenter: true)
                                               where dest.InBounds(map)
                                               select new ShootLine(report.ShootLine.Source, dest)).SelectMany((ShootLine line) => line.Points().Concat(line.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map))).Distinct();
            float num = 0f;
            foreach (IntVec3 item in enumerable)
            {
                float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), item);
                if (num2 <= 0f)
                {
                    continue;
                }
                List<Thing> thingList = item.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing is IAttackTarget && thing != target)
                    {
                        float num3 = ((thing == searcher) ? FriendlyFireScoreOffsetSelf : ((!(thing is Pawn)) ? 10f : (thing.def.race.Animal ? FriendlyFireScoreOffsetPerAnimal : FriendlyFireScoreOffsetPerHumanlikeOrMechanoid)));
                        num3 *= num2;
                        num3 = ((!searcher.Thing.HostileTo(thing)) ? (num3 * -1f) : (num3 * 0.6f));
                        num += num3;
                    }
                }
            }
            return num;
        }
    }
}
