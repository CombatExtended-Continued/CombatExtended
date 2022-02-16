using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;
using Multiplayer.API;

namespace CombatExtended
{
    public class ExplosionCE : Verse.Explosion
    {
        // New properties
        public float height;
        public bool radiusChange = false;
        public bool toBeMerged = false;
        private const int DamageAtEdge = 2;      // Synch these with spreadsheet
        private const float PenAtEdge = 0.6f;
        private const float PressurePerDamage = 0.3f;
        private const float MaxMergeTicks = 3f;
        public const float MaxMergeRange = 3f;           //merge within 3 tiles
        public const bool MergeExplosions = false;

        // New method
        public virtual bool MergeWith(ExplosionCE other, out ExplosionCE merged, out ExplosionCE nonMerged)
        {
            merged = null;
            nonMerged = null;

            //Log.Message("This: "+this.ThingID+", Other: "+other.ThingID);

            //Sanity check
            if (other == null)
                return false;

            var startDiff = (other.startTick == 0 ? Find.TickManager.TicksGame : other.startTick)
                - (startTick == 0 ? Find.TickManager.TicksGame : startTick);

            //Log.Message("StartDiff: " + startDiff);

            if (Mathf.Abs(startDiff) > MaxMergeTicks)
                return false;

            //Log.Message("DF: " + damageFalloff + ", " + other.damageFalloff);

            if (other.damageFalloff != damageFalloff)
                return false;

            //Log.Message("DT: " + damType.defName + ", " + other.damType.defName);

            if (other.damType != damType)
                return false;

            //Log.Message("Sign: " + Mathf.Sign(height - CollisionVertical.WallCollisionHeight) + ", " + Mathf.Sign(other.height - CollisionVertical.WallCollisionHeight));

            if (Mathf.Sign(other.height - CollisionVertical.WallCollisionHeight) != Mathf.Sign(height - CollisionVertical.WallCollisionHeight))
                return false;

            //Log.Message("PreExp: " + (preExplosionSpawnThingDef != null ? preExplosionSpawnThingDef.defName : "null") + ", " + (other.preExplosionSpawnThingDef != null ? other.preExplosionSpawnThingDef.defName : "null"));

            if (preExplosionSpawnThingDef != null && other.preExplosionSpawnThingDef != null && other.preExplosionSpawnThingDef != preExplosionSpawnThingDef)
                return false;

            //Log.Message("PostExp: " + (postExplosionSpawnThingDef != null ? postExplosionSpawnThingDef.defName : "null") + ", " + (other.postExplosionSpawnThingDef != null ? other.postExplosionSpawnThingDef.defName : "null"));

            if (postExplosionSpawnThingDef != null && other.postExplosionSpawnThingDef != null && other.postExplosionSpawnThingDef != postExplosionSpawnThingDef)
                return false;

            Thing newInstigator = null;

            //Log.Message("Instig: " + (instigator != null ? instigator.ThingID : "null") + ", " + (other.instigator != null ? other.instigator.ThingID : "null"));

            //Instigator "equal-enough"
            if (other.instigator != null && instigator != null)
            {
                //If both were hostile action..
                if (other.intendedTarget != null && intendedTarget != null
                    && other.intendedTarget.HostileTo(other.instigator)
                    && intendedTarget.HostileTo(instigator))
                {
                    //If both instigators had different factions, the explosions had different intentions -- cannot merge
                    if (instigator.Faction != null && other.instigator.Faction != null && instigator.Faction != other.instigator.Faction)
                        return false;
                }
                else
                {
                    if (other.instigator != instigator)
                    {
                        //Impossible to distinguish for a combat log which pawn initiated the explosion
                        if (instigator is Pawn && other.instigator is Pawn)
                            return false;
                        else
                        {
                            if (instigator is Pawn)
                                newInstigator = instigator;
                            else if (other.instigator is Pawn)
                                newInstigator = other.instigator;

                            if (instigator is AmmoThing || other.instigator is AmmoThing)
                                newInstigator = instigator is AmmoThing ? other.instigator : instigator;
                            else
                                newInstigator = Rand.Value < 0.5f ? instigator : other.instigator;
                        }
                    }
                }
            }

            //Log.Message("Wpn: " + (weapon != null ? weapon.defName : "null") + ", " + (other.weapon != null ? other.weapon.defName : "null"));

            //Might be problematic
            if (other.weapon != null && weapon != null && other.weapon != weapon)
                return false;

            //Log.Message("Prj: " + (projectile != null ? projectile.defName : "null") + ", " + (other.projectile != null ? other.projectile.defName : "null"));

            //Might be problematic
            if (other.projectile != null && projectile != null && other.projectile != projectile)
                return false;

            //Log.Message("LOS1: " + needLOSToCell1.ToString() + ", " + other.needLOSToCell1.ToString());

            if (other.needLOSToCell1 != needLOSToCell1)
                return false;

            //Log.Message("LOS2: " + needLOSToCell2.ToString() + ", " + other.needLOSToCell2.ToString());

            if (other.needLOSToCell2 != needLOSToCell2)
                return false;

            //Crucial matches
            merged = startDiff <= 0 ? this : other;
            nonMerged = startDiff <= 0 ? other : this;

            //Log.Message("this: "+this.ThingID + " merged: "+merged.ThingID + " other: "+other.ThingID + " nonmerged: "+nonMerged.ThingID);

            merged.instigator = newInstigator;

            //Combine shared ignored things
            var newIgnoredThings = new HashSet<Thing>();
            if (ignoredThings != null && other.ignoredThings != null)
                newIgnoredThings.AddRange(ignoredThings.Where(x => other.ignoredThings.Contains(x)));

            //Add instigators if necessary
            if (ignoredThings != null && ignoredThings.Contains(instigator))
                newIgnoredThings.Add(instigator);
            if (other.ignoredThings != null && other.ignoredThings.Contains(other.instigator))
                newIgnoredThings.Add(other.instigator);

            merged.ignoredThings = newIgnoredThings.ToList();

            //Combine chances such that the same spread of things is observed, while the average is retained to the best of our ability using integer values only
            merged.chanceToStartFire = 1 - (1 - chanceToStartFire) * (1 - other.chanceToStartFire);
            merged.preExplosionSpawnThingCount = Mathf.RoundToInt((preExplosionSpawnThingCount * preExplosionSpawnChance
                + other.preExplosionSpawnThingCount * other.preExplosionSpawnChance) / (1 - (1 - preExplosionSpawnChance) * (1 - other.preExplosionSpawnChance)));
            merged.preExplosionSpawnChance = 1 - (1 - preExplosionSpawnChance) * (1 - other.preExplosionSpawnChance);
            merged.postExplosionSpawnThingCount = Mathf.RoundToInt((postExplosionSpawnThingCount * postExplosionSpawnChance
                + other.postExplosionSpawnThingCount * other.postExplosionSpawnChance) / (1 - (1 - postExplosionSpawnChance) * (1 - other.postExplosionSpawnChance)));
            merged.postExplosionSpawnChance = 1 - (1 - postExplosionSpawnChance) * (1 - other.postExplosionSpawnChance);

            //Linearly combine damage, since that's how it would be if both explosions ran
            merged.armorPenetration = Mathf.Max(damAmount * PressurePerDamage, armorPenetration)
                + Mathf.Max(other.damAmount * PressurePerDamage, other.armorPenetration);
            merged.damAmount = damAmount + other.damAmount;

            if (!merged.applyDamageToExplosionCellsNeighbors && nonMerged.applyDamageToExplosionCellsNeighbors)
            {
                merged.applyDamageToExplosionCellsNeighbors = true;
                merged.radiusChange = true;
            }

            if (radius != other.radius && merged.radius < nonMerged.radius)
            {
                merged.radius = Mathf.Max(radius, other.radius);
                merged.radiusChange = true;
            }

            return true;
        }

        // Changed method to include "aboveRoofs" check
        public virtual IEnumerable<IntVec3> ExplosionCellsToHit
        {
            get
            {
                var roofed = Position.Roofed(Map);
                var aboveRoofs = height >= CollisionVertical.WallCollisionHeight;

                var openCells = new List<IntVec3>();
                var adjWallCells = new List<IntVec3>();

                var num = GenRadial.NumCellsInRadius(radius);
                for (var i = 0; i < num; i++)
                {
                    var intVec = Position + GenRadial.RadialPattern[i];
                    if (intVec.InBounds(Map))
                    {
                        //Added code
                        if (aboveRoofs)
                        {
                            if ((!roofed && GenSight.LineOfSight(Position, intVec, Map, false, null, 0, 0))
                               || !intVec.Roofed(Map))
                            {
                                openCells.Add(intVec);
                            }
                        }
                        else if (GenSight.LineOfSight(Position, intVec, Map, true, null, 0, 0))
                        {
                            if (needLOSToCell1.HasValue || needLOSToCell2.HasValue)
                            {
                                bool flag = needLOSToCell1.HasValue && GenSight.LineOfSight(needLOSToCell1.Value, intVec, Map, false, null, 0, 0);
                                bool flag2 = needLOSToCell2.HasValue && GenSight.LineOfSight(needLOSToCell2.Value, intVec, Map, false, null, 0, 0);
                                if (!flag && !flag2)
                                {
                                    continue;
                                }
                            }
                            openCells.Add(intVec);
                        }
                    }
                }
                foreach (var intVec2 in openCells)
                {
                    if (intVec2.Walkable(Map))
                    {
                        for (var k = 0; k < 4; k++)
                        {
                            var intVec3 = intVec2 + GenAdj.CardinalDirections[k];
                            if (intVec3.InHorDistOf(Position, radius) && intVec3.InBounds(Map) && !intVec3.Standable(Map) && intVec3.GetEdifice(Map) != null && !openCells.Contains(intVec3) && adjWallCells.Contains(intVec3))
                            {
                                adjWallCells.Add(intVec3);
                            }
                        }
                    }
                }
                return openCells.Concat(adjWallCells);
            }
        }

        public void RestartAfterMerge(SoundDef explosionSound)
        {
            //Hasn't been started yet or cellsToAffect has to be recalculated
            if (startTick == 0 || radiusChange)
            {
                StartExplosionCE(explosionSound, ignoredThings);
                radiusChange = false;
            }

            //If it has already been started and there's no radius change, continue as it was planning to
        }

        public void StartExplosionCE(SoundDef explosionSound, List<Thing> ignoredThings)    //Removed cellsToAffect by Worker
        {
            if (Controller.settings.MergeExplosions)
            {
                ExplosionCE existingExplosion = Position.GetThingList(Map).FirstOrDefault(x => x.def == CE_ThingDefOf.ExplosionCE && x != this && !(x as ExplosionCE).toBeMerged) as ExplosionCE;

                if (existingExplosion == null)
                {
                    var i = 1;
                    var num = GenRadial.NumCellsInRadius(MaxMergeRange);

                    while (i < num && existingExplosion == null)
                    {
                        IntVec3 c = Position + GenRadial.RadialPattern[i];
                        if (c.InBounds(Map))
                        {
                            existingExplosion = c.GetThingList(Map).FirstOrDefault(x => x.def == CE_ThingDefOf.ExplosionCE && x != this && !(x as ExplosionCE).toBeMerged) as ExplosionCE;
                        }
                        i++;
                    }
                }

                if (existingExplosion != null)
                {
                    if (MergeWith(existingExplosion, out var merged, out var nonMerged))
                    {
                        nonMerged.toBeMerged = true;
                        //Was possible to merge, destroy nonmerged (later) and start merged.
                        merged.RestartAfterMerge(explosionSound);
                        return;
                    }
                }
            }

            if (!Spawned)
            {
                Log.Error("Called StartExplosion() on unspawned thing.");
                return;
            }

            if (this.ignoredThings.NullOrEmpty())
                this.ignoredThings = ignoredThings;

            startTick = Find.TickManager.TicksGame;
            cellsToAffect.Clear();
            damagedThings.Clear();
            addedCellsAffectedOnlyByDamage.Clear();
            //this.cellsToAffect.AddRange(this.damType.Worker.ExplosionCellsToHit(this));
            cellsToAffect.AddRange(ExplosionCellsToHit);
            if (applyDamageToExplosionCellsNeighbors)
            {
                AddCellsNeighbors(cellsToAffect);
            }
            damType.Worker.ExplosionStart(this, cellsToAffect);
            PlayExplosionSound(explosionSound);
            if (MP.IsInMultiplayer) Rand.PushState();
            FleckMaker.WaterSplash(Position.ToVector3Shifted(), Map, radius * 6f, 20f);
            if (MP.IsInMultiplayer) Rand.PopState();
            cellsToAffect.Sort((IntVec3 a, IntVec3 b) => GetCellAffectTick(b).CompareTo(GetCellAffectTick(a)));
            RegionTraverser.BreadthFirstTraverse(Position, Map, (Region from, Region to) => true, delegate (Region x)
            {
                var list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    ((Pawn)list[i]).mindState.Notify_Explosion(this);
                }
                return false;
            }, 25, RegionType.Set_Passable);
        }

        public override void Tick()
        {
            int ticksGame = Find.TickManager.TicksGame;
            int num = this.cellsToAffect.Count - 1;
            while (!toBeMerged && num >= 0 && ticksGame >= this.GetCellAffectTick(this.cellsToAffect[num]))
            {
                try
                {
                    AffectCell(this.cellsToAffect[num]);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Explosion could not affect cell ",
                        this.cellsToAffect[num],
                        ": ",
                        ex
                    }));
                }
                this.cellsToAffect.RemoveAt(num);
                num--;
            }
            if (toBeMerged || !this.cellsToAffect.Any<IntVec3>())
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref startTick, "startTick", 0, false);
            Scribe_Collections.Look<IntVec3>(ref cellsToAffect, "cellsToAffect", LookMode.Value, new object[0]);
            Scribe_Collections.Look<Thing>(ref damagedThings, "damagedThings", LookMode.Reference, new object[0]);
            Scribe_Collections.Look<IntVec3>(ref addedCellsAffectedOnlyByDamage, "addedCellsAffectedOnlyByDamage", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                damagedThings.RemoveAll((Thing x) => x == null);
            }
        }

        //New methods
        public int GetDamageAmountAtCE(IntVec3 c)   //t => t^(0.333f)
        {
            if (!damageFalloff)
            {
                return damAmount;
            }
            var t = c.DistanceTo(Position) / radius;
            t = Mathf.Pow(t, 0.333f);
            return Mathf.Max(GenMath.RoundRandom(Mathf.Lerp((float)damAmount, DamageAtEdge, t)), 1);
        }

        public float GetArmorPenetrationAtCE(IntVec3 c) //t => t^(0.55f), penetrationAmount => damAmount * PressurePerDamage
        {
            var basePen = Mathf.Max(damAmount * PressurePerDamage, armorPenetration);
            if (!damageFalloff)
            {
                return basePen;
            }
            var t = c.DistanceTo(Position) / radius;
            t = Mathf.Pow(t, 0.55f);
            return Mathf.Lerp(basePen, PenAtEdge, t);
        }
    }
}
