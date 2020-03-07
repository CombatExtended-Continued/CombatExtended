using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace CombatExtended
{
	public class Explosion : Verse.Explosion
	{
		public float height;

		private int startTick;
		private List<IntVec3> cellsToAffect;
		private List<Thing> damagedThings;
		private HashSet<IntVec3> addedCellsAffectedOnlyByDamage;
		private const int DamageAtEdge = 2;      // Synch these with spreadsheet
        private const float PenAtEdge = 0.6f;
        private const float PressurePerDamage = 0.3f;
		private static HashSet<IntVec3> tmpCells = new HashSet<IntVec3>();
		private List<Thing> ignoredThings;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad) {
				cellsToAffect = SimplePool<List<IntVec3>>.Get();
				cellsToAffect.Clear();
				damagedThings = SimplePool<List<Thing>>.Get();
				damagedThings.Clear();
				addedCellsAffectedOnlyByDamage = SimplePool<HashSet<IntVec3>>.Get();
				addedCellsAffectedOnlyByDamage.Clear();
			}
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			cellsToAffect.Clear();
			SimplePool<List<IntVec3>>.Return(cellsToAffect);
			cellsToAffect = null;
			damagedThings.Clear();
			SimplePool<List<Thing>>.Return(damagedThings);
			damagedThings = null;
			addedCellsAffectedOnlyByDamage.Clear();
			SimplePool<HashSet<IntVec3>>.Return(addedCellsAffectedOnlyByDamage);
			addedCellsAffectedOnlyByDamage = null;
		}

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
							openCells.Add(intVec);
						}
					}
				}
				foreach (var intVec2 in openCells) {
					if (intVec2.Walkable(Map)) {
						for (var k = 0; k < 4; k++) {
							var intVec3 = intVec2 + GenAdj.CardinalDirections[k];
							if (intVec3.InHorDistOf(Position, radius)) {
								if (intVec3.InBounds(Map)) {
									if (!intVec3.Standable(Map)) {
										if (intVec3.GetEdifice(Map) != null) {
											if (!openCells.Contains(intVec3) && adjWallCells.Contains(intVec3)) {
												adjWallCells.Add(intVec3);
											}
										}
									}
								}
							}
						}
					}
				}
				return openCells.Concat(adjWallCells);
			}
		}
		
		public  void StartExplosion(SoundDef explosionSound)
		{
			if (!Spawned) {
				Log.Error("Called StartExplosion() on unspawned thing.");
				return;
			}
			startTick = Find.TickManager.TicksGame;
			cellsToAffect.Clear();
			damagedThings.Clear();
			addedCellsAffectedOnlyByDamage.Clear();
			//this.cellsToAffect.AddRange(this.damType.Worker.ExplosionCellsToHit(this));
			cellsToAffect.AddRange(ExplosionCellsToHit);
			if (applyDamageToExplosionCellsNeighbors) {
				AddCellsNeighbors(cellsToAffect);
			}
			damType.Worker.ExplosionStart(this, cellsToAffect);
			PlayExplosionSound(explosionSound);
			MoteMaker.MakeWaterSplash(Position.ToVector3Shifted(), Map, radius * 6f, 20f);
			cellsToAffect.Sort((IntVec3 a, IntVec3 b) => GetCellAffectTick(b).CompareTo(GetCellAffectTick(a)));
			RegionTraverser.BreadthFirstTraverse(Position, Map, (Region from, Region to) => true, delegate(Region x) {
				var list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
				for (var i = list.Count - 1; i >= 0; i--) {
					((Pawn)list[i]).mindState.Notify_Explosion(this);
				}
				return false;
			}, 25, RegionType.Set_Passable);
		}

		public override void Tick()
		{
			var ticksGame = Find.TickManager.TicksGame;
			var count = cellsToAffect.Count;
			for (var i = count - 1; i >= 0; i--) {
				if (ticksGame < GetCellAffectTick(cellsToAffect[i])) {
					break;
				}
				try {
					AffectCell(cellsToAffect[i]);
				}
				catch (Exception ex) {
					Log.Error(string.Concat(new object[] {
						"Explosion could not affect cell ",
						cellsToAffect[i],
						": ",
						ex
					}));
				}
				cellsToAffect.RemoveAt(i);
			}
			if (!cellsToAffect.Any<IntVec3>()) {
				Destroy(DestroyMode.Vanish);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref startTick, "startTick", 0, false);
			Scribe_Collections.Look<IntVec3>(ref cellsToAffect, "cellsToAffect", LookMode.Value, new object[0]);
			Scribe_Collections.Look<Thing>(ref damagedThings, "damagedThings", LookMode.Reference, new object[0]);
			Scribe_Collections.Look<IntVec3>(ref addedCellsAffectedOnlyByDamage, "addedCellsAffectedOnlyByDamage", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				damagedThings.RemoveAll((Thing x) => x == null);
			}
		}

		private int GetCellAffectTick(IntVec3 cell)
		{
			return startTick + (int)((cell - Position).LengthHorizontal * 1.5f);
		}

		private void AffectCell(IntVec3 c)
		{
			var flag = ShouldCellBeAffectedOnlyByDamage(c);
            if (!flag && Rand.Chance(preExplosionSpawnChance) && c.Walkable(Map)) {
				TrySpawnExplosionThing(preExplosionSpawnThingDef, c, preExplosionSpawnThingCount);
			}
			damType.Worker.ExplosionAffectCell(this, c, damagedThings, !flag);
			if (!flag && Rand.Chance(postExplosionSpawnChance) && c.Walkable(Map)) {
				TrySpawnExplosionThing(postExplosionSpawnThingDef, c, postExplosionSpawnThingCount);
			}
			var num = chanceToStartFire;
			if (damageFalloff) {
				num *= Mathf.Lerp(1f, (float)DamageAtEdge / damAmount, c.DistanceTo(Position) / radius);
			}
			if (Rand.Chance(num)) {
				FireUtility.TryStartFireIn(c, Map, Rand.Range(0.1f, 0.925f));
			}
		}

		private void TrySpawnExplosionThing(ThingDef thingDef, IntVec3 c, int count)
		{
			if (thingDef == null) {
				return;
			}
			if (thingDef.IsFilth) {
				FilthMaker.TryMakeFilth(c, Map, thingDef, count);
			}
			else {
				var thing = ThingMaker.MakeThing(thingDef, null);
				thing.stackCount = count;
				GenSpawn.Spawn(thing, c, Map);
			}
		}

		private void PlayExplosionSound(SoundDef explosionSound)
		{
			bool flag;
			if (Prefs.DevMode) {
				flag = (explosionSound != null);
			}
			else {
				flag = !explosionSound.NullOrUndefined();
			}
			if (flag) {
				explosionSound.PlayOneShot(new TargetInfo(Position, Map, false));
			}
			else {
				damType.soundExplosion.PlayOneShot(new TargetInfo(Position, Map, false));
			}
		}

		private void AddCellsNeighbors(List<IntVec3> cells)
		{
			tmpCells.Clear();
			addedCellsAffectedOnlyByDamage.Clear();
			for (var i = 0; i < cells.Count; i++) {
				tmpCells.Add(cells[i]);
			}
			for (var j = 0; j < cells.Count; j++) {
				if (cells[j].Walkable(Map)) {
					for (var k = 0; k < GenAdj.AdjacentCells.Length; k++) {
						var intVec = cells[j] + GenAdj.AdjacentCells[k];
						if (intVec.InBounds(Map)) {
							var flag = tmpCells.Add(intVec);
							if (flag) {
								addedCellsAffectedOnlyByDamage.Add(intVec);
							}
						}
					}
				}
			}
			cells.Clear();
			foreach (var current in tmpCells) {
				cells.Add(current);
			}
			tmpCells.Clear();
		}

		private bool ShouldCellBeAffectedOnlyByDamage(IntVec3 c)
		{
			return applyDamageToExplosionCellsNeighbors && addedCellsAffectedOnlyByDamage.Contains(c);
		}

        public int GetDamageAmountAtCE(IntVec3 c)
        {
            if (!damageFalloff)
            {
                return damAmount;
            }
            var t = c.DistanceTo(Position) / radius;
            t = Mathf.Pow(t, 1 / 3f);
            var a = GenMath.RoundRandom(Mathf.Lerp((float)damAmount, DamageAtEdge, t));

            return Mathf.Max(a, 1);
        }

        public float GetArmorPenetrationAtCE(IntVec3 c)
        {
            var basePen = damAmount * PressurePerDamage;
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
