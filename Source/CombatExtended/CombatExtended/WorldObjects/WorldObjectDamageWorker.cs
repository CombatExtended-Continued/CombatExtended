using CombatExtended.WorldObjects;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WorldObjectDamageWorker
    {
        #region WorldObject
        public virtual float ApplyDamage(HealthComp healthComp, ThingDef shellDef)
        {
            var damage = CalculateDamage(shellDef.GetProjectile(), healthComp.parent.Faction) / healthComp.ArmorDamageMultiplier;
            healthComp.Health -= damage;
            return damage;
        }
        public virtual float CalculateDamage(ThingDef projectile, Faction faction)
        {
            var empModifier = 0.03f;
            if (faction != null)
            {
                if ((int)faction.def.techLevel > (int)TechLevel.Medieval)
                {
                    empModifier = Mathf.Pow(3.3f, (int)faction.def.techLevel) / 1800f / ((int)faction.def.techLevel - 2f);
                }
                if (faction.def.pawnGroupMakers.SelectMany(x => x.options).All(k => k.kind.RaceProps.IsMechanoid))
                {
                    empModifier = 1f;
                }
            }
            var result = FragmentsPotentialDamage(projectile) + FirePotentialDamage(projectile) + EMPPotentialDamage(projectile, empModifier) + OtherPotentialDamage(projectile);
            //Damage calculated as in-map damage, needs to be converted into world object damage. 3500f experimentally obtained
            result /= 3500f;
            //manual overwrite
            if (projectile.projectile is ProjectilePropertiesCE projectileProperties && projectileProperties.shellingProps.damage > 0f)
            {
                result = projectileProperties.shellingProps.damage;
            }
            //Crit/Miss imitation
            result *= Rand.Range(0.4f, 1.5f);
            return result;
        }
        protected const float fragDamageMultipler = 0.04f;
        protected virtual float FragmentsPotentialDamage(ThingDef projectile)
        {
            var result = 0f;
            var fragProp = projectile.GetCompProperties<CompProperties_Fragments>();
            if (projectile.projectile is ProjectilePropertiesCE props && fragProp != null)
            {
                foreach (var frag in fragProp.fragments)
                {
                    result += frag.count * frag.thingDef.projectile.damageAmountBase * fragDamageMultipler;
                }
            }
            return result;
        }
        protected virtual float EMPPotentialDamage(ThingDef projectile, float modifier = 0.03f)
        {
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props && props.damageDef == DamageDefOf.EMP)
            {
                result += props.damageAmountBase * modifier;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += modifier * DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
            }
            return result;
        }
        protected virtual float FirePotentialDamage(ThingDef projectile)
        {
            const float prometheumDamagePerCell = 3;
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props && props.damageDef == CE_DamageDefOf.PrometheumFlame)
            {
                result += props.damageAmountBase;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
                if (props.preExplosionSpawnThingDef == CE_ThingDefOf.FilthPrometheum)
                {
                    result += props.preExplosionSpawnChance * (Mathf.PI * props.explosionRadius * props.explosionRadius) * prometheumDamagePerCell; // damage per cell with preExplosionSpawn Chance
                }
            }
            return result;
        }
        protected virtual float OtherPotentialDamage(ThingDef projectile)
        {
            float result = 0f;
            if (projectile.projectile is ProjectilePropertiesCE props)
            {
                if (props.damageDef == DamageDefOf.EMP || props.damageDef == CE_DamageDefOf.PrometheumFlame)
                {
                    return 0f;
                }
                result += props.damageAmountBase;
                for (int i = 1; i < props.explosionRadius; i++)
                {
                    result += DamageAtRadius(projectile, i) * Mathf.Pow(2, i);
                }
                var extension = props.damageDef.GetModExtension<DamageDefExtensionCE>();
                if (extension != null && extension.worldDamageMultiplier >= 0.0f)
                {
                    result *= extension.worldDamageMultiplier;
                }
            }
            return result;
        }

        public static float DamageAtRadius(ThingDef projectile, int radius)
        {
            if (!projectile.projectile.explosionDamageFalloff)
            {
                return projectile.projectile.damageAmountBase;
            }
            float t = radius / projectile.projectile.explosionRadius;
            return Mathf.Max(GenMath.RoundRandom(Mathf.Lerp((float)projectile.projectile.damageAmountBase, (float)projectile.projectile.damageAmountBase * 0.2f, t)), 1);
        }
        #endregion
        #region Attrition
        protected static Map map = null;
        public static void BeginAttrition(Map map) { WorldObjectDamageWorker.map = map; }
        public static void EndAttrition() { map = null; }
        public virtual void OnExploded(IntVec3 cell, ThingDef shell) { }
        public virtual void ProcessShell(ThingDef shell)
        {
            int radius = (int)shell.projectile.explosionRadius;
            int dmg = shell.projectile.damageAmountBase;
            IntVec3 cell;
            RoofDef roof;
            int count = 0;
            do
            {
                cell = new IntVec3((int)CE_Utility.RandomGaussian(1, map.Size.x - 1), 0, (int)CE_Utility.RandomGaussian(1, map.Size.z - 1));
                roof = cell.GetRoof(map);
                count++;
            } while (count <= 7 && roof == RoofDefOf.RoofRockThick);
            if (roof != RoofDefOf.RoofRockThick && TryExplode(cell, shell))
            {
                OnExploded(cell, shell);
            }
        }

        protected virtual bool TryExplode(IntVec3 centerCell, ThingDef shellDef)
        {
            var radius = (int)shellDef.GetProjectile().projectile.explosionRadius;


            if (!centerCell.InBounds(map))
            {
                return false;
            }
            ProcessFragmentsComp(shellDef);
            DamageToPawns(shellDef);
            IEnumerable<IntVec3> cellsToAffect = ExplosionCellsToHit(centerCell, map, radius);


            foreach (var cellToAffect in cellsToAffect)
            {
                List<Thing> things = cellToAffect.GetThingList(map).Except(map.mapPawns.AllPawns).ToList();
                if (Controller.settings.DebugDisplayAttritionInfo)
                {
                    map.debugDrawer.FlashCell(cellToAffect, duration: 1000);
                }

                var filthMade = false;
                var damageCell = (int)DamageAtRadius(shellDef, (int)centerCell.DistanceTo(cellToAffect));
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (!thing.def.useHitPoints)
                    {
                        continue;
                    }
                    thing.hitPointsInt -= damageCell * (thing.IsPlant() ? 3 : 1);
                    if (thing.hitPointsInt > 0)
                    {
                        if (!filthMade && Rand.Chance(0.5f))
                        {
                            ScatterDebrisUtility.ScatterFilthAroundThing(thing, map, ThingDefOf.Filth_RubbleBuilding);
                            filthMade = true;
                        }
                        if (Rand.Chance(0.1f))
                        {
                            FireUtility.TryStartFireIn(cellToAffect, map, Rand.Range(0.5f, 1.5f), null);
                        }
                    }
                    else
                    {
                        thing.DeSpawn(DestroyMode.Vanish);
                        thing.Destroy(DestroyMode.Vanish);
                        //                        if (thing.def.category == ThingCategory.Plant && (thing.def.plant?.IsTree ?? false))
                        //                        {
                        //                            //Todo: 1.5 burned trees
                        //                            Thing burntTree = ThingMaker.MakeThing(ThingDefOf.BurnedTree);
                        //                            burntTree.positionInt = cellToAffect;
                        //                            burntTree.SpawnSetup(map, false);
                        //                            if (!filthMade && Rand.Chance(0.5f))
                        //                            {
                        //                                ScatterDebrisUtility.ScatterFilthAroundThing(burntTree, map, ThingDefOf.Filth_Ash);
                        //                                filthMade = true;
                        //                            }
                        //                        }
                        if (thing.def.MakeFog)
                        {
                            map.fogGrid.Notify_FogBlockerRemoved(thing);
                        }
                        ThingDef filth = thing.def.filthLeaving ?? (Rand.Chance(0.5f) ? ThingDefOf.Filth_Ash : ThingDefOf.Filth_RubbleBuilding);
                        if (!filthMade && FilthMaker.TryMakeFilth(cellToAffect, map, filth, Rand.Range(1, 3), FilthSourceFlags.Any))
                        {
                            filthMade = true;
                        }
                    }
                }
                map.snowGrid.SetDepth(cellToAffect, 0);
                map.roofGrid.SetRoof(cellToAffect, null);
                if (Rand.Chance(0.33f) && map.terrainGrid.CanRemoveTopLayerAt(cellToAffect))
                {
                    map.terrainGrid.RemoveTopLayer(cellToAffect, false);
                }
            }
            return true;
        }

        protected virtual void DamageToPawns(ThingDef shellDef)
        {
            var projDef = shellDef.GetProjectile();
            if (Rand.Chance(0.05f))
            {
                int countAffectedPawns = Rand.Range(1, Math.Min(map.mapPawns.AllPawnsSpawnedCount, (int)projDef.projectile.explosionRadius));
                for (int affectNum = 0; affectNum < countAffectedPawns; affectNum++)
                {
                    if (map.mapPawns.AllPawnsSpawned.Where(x => !x.Faction.IsPlayerSafe()).ToList().TryRandomElementByWeight((x => x.Faction.HostileTo(Faction.OfPlayer) ? 1f : 0.2f), out Pawn pawn))
                    {
                        DamagePawn(pawn, projDef);
                    }
                }
            }
        }
        protected virtual void DamagePawn(Pawn pawn, ThingDef projDef)
        {
            BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, CE_RulePackDefOf.DamageEvent_ShellingExplosion, null);
            Find.BattleLog.Add(battleLogEntry_DamageTaken);
            var num = WorldObjectDamageWorker.DamageAtRadius(projDef, Rand.Range(0, (int)projDef.projectile.explosionRadius));
            DamageInfo dinfo = new DamageInfo(projDef.projectile.damageDef, (float)num, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true);
            dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            pawn.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
            ResetVisualDamageEffects(pawn);
        }

        protected HashSet<IntVec3> addedCellsAffectedOnlyByDamage = new HashSet<IntVec3>();
        public virtual IEnumerable<IntVec3> ExplosionCellsToHit(IntVec3 center, Map map, float radius, IntVec3? needLOSToCell1 = null, IntVec3? needLOSToCell2 = null, FloatRange? affectedAngle = null)
        {
            var roofed = center.Roofed(map);
            var aboveRoofs = false;

            var openCells = new List<IntVec3>();
            var adjWallCells = new List<IntVec3>();

            var num = GenRadial.NumCellsInRadius(radius);
            for (var i = 0; i < num; i++)
            {
                var intVec = center + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map))
                {
                    //Added code
                    if (aboveRoofs)
                    {
                        if ((!roofed && GenSight.LineOfSight(center, intVec, map, false, null, 0, 0))
                                || !intVec.Roofed(map))
                        {
                            openCells.Add(intVec);
                        }
                    }
                    else if (GenSight.LineOfSight(center, intVec, map, true, null, 0, 0))
                    {
                        if (needLOSToCell1.HasValue || needLOSToCell2.HasValue)
                        {
                            bool flag = needLOSToCell1.HasValue && GenSight.LineOfSight(needLOSToCell1.Value, intVec, map, false, null, 0, 0);
                            bool flag2 = needLOSToCell2.HasValue && GenSight.LineOfSight(needLOSToCell2.Value, intVec, map, false, null, 0, 0);
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
                if (intVec2.Walkable(map))
                {
                    for (var k = 0; k < 4; k++)
                    {
                        var intVec3 = intVec2 + GenAdj.CardinalDirections[k];
                        if (intVec3.InHorDistOf(center, radius) && intVec3.InBounds(map) && !intVec3.Standable(map) && intVec3.GetEdifice(map) != null && !openCells.Contains(intVec3) && adjWallCells.Contains(intVec3))
                        {
                            adjWallCells.Add(intVec3);
                        }
                    }
                }
            }
            return openCells.Concat(adjWallCells);
        }
        void ProcessFragmentsComp(ThingDef shellDef)
        {
            var projDef = shellDef.GetProjectile();
            if (projDef.HasComp(typeof(CompFragments)))
            {
                var frags = projDef.GetCompProperties<CompProperties_Fragments>();
                if (Rand.Chance(0.33f))
                {
                    int countAffectedPawns = Rand.Range(1, Math.Min(map.mapPawns.AllPawnsSpawnedCount, 5));
                    for (int affectNum = 0; affectNum < countAffectedPawns; affectNum++)
                    {
                        if (map.mapPawns.AllPawnsSpawned.Where(x => !x.Faction.IsPlayerSafe()).ToList().TryRandomElementByWeight((x => x.Faction.HostileTo(Faction.OfPlayer) ? 1f : 0.2f), out Pawn pawn))
                        {
                            var hitsCount = Rand.Range(3, 9);
                            for (int i = 0; i < hitsCount; i++)
                            {
                                if (pawn.Map == null)
                                {
                                    break;
                                }
                                var frag = GenSpawn.Spawn(frags.fragments.RandomElementByWeight(x => x.count).thingDef, pawn.Position, pawn.Map) as ProjectileCE;
                                frag.Impact(pawn);
                            }
                            ResetVisualDamageEffects(pawn);
                        }
                    }
                }

            }
        }
        public static void ResetVisualDamageEffects(Pawn pawn)
        {
            var draw = pawn.drawer;
            (new Traverse(draw).Field("jitterer").GetValue() as JitterHandler)?.ProcessPostTickVisuals(10000);
            draw.renderer.graphics.flasher.lastDamageTick = -9999;
        }
        #endregion

    }
}
