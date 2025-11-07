using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace CombatExtended;

public class CompAbilityEffect_ExplosionCE : CompAbilityEffect
{
    public new CompProperties_AbilityExplosionCE Props => (CompProperties_AbilityExplosionCE)props;
    public Pawn Pawn => parent.pawn;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        int damage = Props.damageAmount;
        float armorPen = Props.armorPenetration;
        List<Thing> ignoreList = new List<Thing>();
        if (damage == -1)
        {
            damage = Props.damageDef.defaultDamage;
        }

        if (Props.ignoreSelf)
        {
            ignoreList.Add(Pawn);
        }

        GenExplosionCE.DoExplosion(
            center: Pawn.Position,
            map: Pawn.MapHeld,
            radius: Props.explosionRadius,
            damType: Props.damageDef,
            instigator: Pawn,
            damAmount: damage,
            armorPenetration: armorPen,
            explosionSound: Props.soundExplode,
            weapon: parent.verb?.EquipmentSource?.def,
            postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
            postExplosionSpawnChance: Props.postExplosionSpawnChance,
            postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount,
            postExplosionGasType: Props.postExplosionGasType,
            applyDamageToExplosionCellsNeighbors: Props.applyDamageToExplosionCellsNeighbors,
            preExplosionSpawnThingDef: Props.preExplosionSpawnThingDef,
            preExplosionSpawnChance: Props.preExplosionSpawnChance,
            preExplosionSpawnThingCount: Props.preExplosionSpawnThingCount,
            chanceToStartFire: Props.explosionChanceToStartFire,
            damageFalloff: Props.explosionDamageFalloff,
            ignoredThings: Props.ignoreSelf ? ignoreList : null,
            doVisualEffects: Props.doExplosionVFX,
            propagationSpeed: Props.damageDef.expolosionPropagationSpeed,
            excludeRadius: Props.ignoreSelf ? 1f : 0f,
            postExplosionSpawnThingDefWater: Props.postExplosionSpawnThingDefWater,
            screenShakeFactor: Props.screenShakeFactor,
            postExplosionSpawnSingleThingDef: Props.postExplosionSpawnSingleThingDef,
            preExplosionSpawnSingleThingDef: Props.preExplosionSpawnSingleThingDef,
            falloffCurve: Props.falloffCurveOverride);
    }
}
