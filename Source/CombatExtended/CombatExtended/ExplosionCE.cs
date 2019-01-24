using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
	public class ExplosionCE : Explosion
	{
		public float height;

		private int startTick;
		private List<IntVec3> cellsToAffect;
		private List<Thing> damagedThings;
		private HashSet<IntVec3> addedCellsAffectedOnlyByDamage;
		private const float DamageFactorAtEdge = 0.2f;
		private static HashSet<IntVec3> tmpCells = new HashSet<IntVec3>();

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad) {
				this.cellsToAffect = SimplePool<List<IntVec3>>.Get();
				this.cellsToAffect.Clear();
				this.damagedThings = SimplePool<List<Thing>>.Get();
				this.damagedThings.Clear();
				this.addedCellsAffectedOnlyByDamage = SimplePool<HashSet<IntVec3>>.Get();
				this.addedCellsAffectedOnlyByDamage.Clear();
			}
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			this.cellsToAffect.Clear();
			SimplePool<List<IntVec3>>.Return(this.cellsToAffect);
			this.cellsToAffect = null;
			this.damagedThings.Clear();
			SimplePool<List<Thing>>.Return(this.damagedThings);
			this.damagedThings = null;
			this.addedCellsAffectedOnlyByDamage.Clear();
			SimplePool<HashSet<IntVec3>>.Return(this.addedCellsAffectedOnlyByDamage);
			this.addedCellsAffectedOnlyByDamage = null;
		}

		public virtual IEnumerable<IntVec3> ExplosionCellsToHit
		{
			get
			{
				bool roofed = Position.Roofed(Map);
				bool aboveRoofs = height >= CollisionVertical.WallCollisionHeight;
				
				var openCells = new List<IntVec3>();
				var adjWallCells = new List<IntVec3>();
				
				int num = GenRadial.NumCellsInRadius(radius);
				for (int i = 0; i < num; i++)
				{
					IntVec3 intVec = Position + GenRadial.RadialPattern[i];
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
						for (int k = 0; k < 4; k++) {
							IntVec3 intVec3 = intVec2 + GenAdj.CardinalDirections[k];
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
		
		public override void StartExplosion(SoundDef explosionSound)
		{
			if (!base.Spawned) {
				Log.Error("Called StartExplosion() on unspawned thing.");
				return;
			}
			this.startTick = Find.TickManager.TicksGame;
			this.cellsToAffect.Clear();
			this.damagedThings.Clear();
			this.addedCellsAffectedOnlyByDamage.Clear();
			//this.cellsToAffect.AddRange(this.damType.Worker.ExplosionCellsToHit(this));
			this.cellsToAffect.AddRange(this.ExplosionCellsToHit);
			if (this.applyDamageToExplosionCellsNeighbors) {
				this.AddCellsNeighbors(this.cellsToAffect);
			}
			this.damType.Worker.ExplosionStart(this, this.cellsToAffect);
			this.PlayExplosionSound(explosionSound);
			MoteMaker.MakeWaterSplash(base.Position.ToVector3Shifted(), base.Map, this.radius * 6f, 20f);
			this.cellsToAffect.Sort((IntVec3 a, IntVec3 b) => this.GetCellAffectTick(b).CompareTo(this.GetCellAffectTick(a)));
			RegionTraverser.BreadthFirstTraverse(base.Position, base.Map, (Region from, Region to) => true, delegate(Region x) {
				List<Thing> list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn);
				for (int i = list.Count - 1; i >= 0; i--) {
					((Pawn)list[i]).mindState.Notify_Explosion(this);
				}
				return false;
			}, 25, RegionType.Set_Passable);
		}

		public override void Tick()
		{
			int ticksGame = Find.TickManager.TicksGame;
			int count = this.cellsToAffect.Count;
			for (int i = count - 1; i >= 0; i--) {
				if (ticksGame < this.GetCellAffectTick(this.cellsToAffect[i])) {
					break;
				}
				try {
					this.AffectCell(this.cellsToAffect[i]);
				}
				catch (Exception ex) {
					Log.Error(string.Concat(new object[] {
						"Explosion could not affect cell ",
						this.cellsToAffect[i],
						": ",
						ex
					}));
				}
				this.cellsToAffect.RemoveAt(i);
			}
			if (!this.cellsToAffect.Any<IntVec3>()) {
				this.Destroy(DestroyMode.Vanish);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.startTick, "startTick", 0, false);
			Scribe_Collections.Look<IntVec3>(ref this.cellsToAffect, "cellsToAffect", LookMode.Value, new object[0]);
			Scribe_Collections.Look<Thing>(ref this.damagedThings, "damagedThings", LookMode.Reference, new object[0]);
			Scribe_Collections.Look<IntVec3>(ref this.addedCellsAffectedOnlyByDamage, "addedCellsAffectedOnlyByDamage", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				this.damagedThings.RemoveAll((Thing x) => x == null);
			}
		}

		private int GetCellAffectTick(IntVec3 cell)
		{
			return this.startTick + (int)((cell - base.Position).LengthHorizontal * 1.5f);
		}

		private void AffectCell(IntVec3 c)
		{
			bool flag = this.ShouldCellBeAffectedOnlyByDamage(c);
			if (!flag && Rand.Chance(this.preExplosionSpawnChance) && c.Walkable(base.Map)) {
				this.TrySpawnExplosionThing(this.preExplosionSpawnThingDef, c, this.preExplosionSpawnThingCount);
			}
			this.damType.Worker.ExplosionAffectCell(this, c, this.damagedThings, !flag);
			if (!flag && Rand.Chance(this.postExplosionSpawnChance) && c.Walkable(base.Map)) {
				this.TrySpawnExplosionThing(this.postExplosionSpawnThingDef, c, this.postExplosionSpawnThingCount);
			}
			float num = this.chanceToStartFire;
			if (this.damageFalloff) {
				num *= Mathf.Lerp(1f, 0.2f, c.DistanceTo(base.Position) / this.radius);
			}
			if (Rand.Chance(num)) {
				FireUtility.TryStartFireIn(c, base.Map, Rand.Range(0.1f, 0.925f));
			}
		}

		private void TrySpawnExplosionThing(ThingDef thingDef, IntVec3 c, int count)
		{
			if (thingDef == null) {
				return;
			}
			if (thingDef.IsFilth) {
				FilthMaker.MakeFilth(c, base.Map, thingDef, count);
			}
			else {
				Thing thing = ThingMaker.MakeThing(thingDef, null);
				thing.stackCount = count;
				GenSpawn.Spawn(thing, c, base.Map);
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
				explosionSound.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
			}
			else {
				this.damType.soundExplosion.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
			}
		}

		private void AddCellsNeighbors(List<IntVec3> cells)
		{
			ExplosionCE.tmpCells.Clear();
			this.addedCellsAffectedOnlyByDamage.Clear();
			for (int i = 0; i < cells.Count; i++) {
				ExplosionCE.tmpCells.Add(cells[i]);
			}
			for (int j = 0; j < cells.Count; j++) {
				if (cells[j].Walkable(base.Map)) {
					for (int k = 0; k < GenAdj.AdjacentCells.Length; k++) {
						IntVec3 intVec = cells[j] + GenAdj.AdjacentCells[k];
						if (intVec.InBounds(base.Map)) {
							bool flag = ExplosionCE.tmpCells.Add(intVec);
							if (flag) {
								this.addedCellsAffectedOnlyByDamage.Add(intVec);
							}
						}
					}
				}
			}
			cells.Clear();
			foreach (IntVec3 current in ExplosionCE.tmpCells) {
				cells.Add(current);
			}
			ExplosionCE.tmpCells.Clear();
		}

		private bool ShouldCellBeAffectedOnlyByDamage(IntVec3 c)
		{
			return this.applyDamageToExplosionCellsNeighbors && this.addedCellsAffectedOnlyByDamage.Contains(c);
		}
	}
}
