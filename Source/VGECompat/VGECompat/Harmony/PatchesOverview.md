# Overview of VGE patches

## Not patched (not required)

### [AvoidGrid_PrintAvoidGridAroundTurret_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_Active_Patch.cs):
Not used — Building_TurretGun is not used in CE, so the patched method is never call anyway. (And CE already handles this)

### [Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch.cs):
No override required; our implementation works.

### [CastSourceReplacer](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/CastSourceReplacer.cs):
Graphical purpose : add an offset to casting position - Not adapted.

### [GenDraw_DrawAimPie_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/GenDraw_DrawAimPie_Patch.cs):
Graphical purpose : add an offset to aiming pie (warm up) - Not adapted.

### [ShotReport_HitFactorFromShooter_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/ShotReport_HitFactorFromShooter_Patch.cs):
Not applicable — we don't use ShotReport the same way.

### [Building_TurretGun_TryStartShootSomething_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_TryStartShootSomething_Patch.cs):
### [TurretTop_DrawTurret_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/TurretTop_DrawTurret_Patch.cs):
### [TurretTop_TurretTopTick_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/TurretTop_TurretTopTick_Patch.cs):
Not adapted — turret rotation is handled by our code.

### [Verb_LaunchProjectile_ForcedMissRadius_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Verb_LaunchProjectile_ForcedMissRadius_Patch.cs):
### [Verb_LaunchProjectile_GetForcedMissTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Verb_LaunchProjectile_GetForcedMissTarget_Patch.cs):
Not applicable — forced miss is not used in CE.

### [DamageWorker_ExplosionDamageTerrain_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/DamageWorker_ExplosionDamageTerrain_Patch.cs):
It works as it is - No need to patch anything on our side. We just need to add this to our own projectiles:
```xml
<modExtensions>
    <li Class="VanillaGravshipExpanded.SubstructureDamageExtension">
        <substructureDamageRadius>1.9</substructureDamageRadius>
    </li>
</modExtensions>
```

### [VerbUtility_ProjectileFliesOverhead_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/VerbUtility_ProjectileFliesOverhead_Patch.cs):
Unknown purpose; not adapted.
It works jointly with `Building_TurretGun_TryStartShootSomething_Patch`, making `VerbUtility.ProjectileFliesOverhead` to return `false` when VGETurret with `!CanAutoAttack` calls `TryFindNewTarget()`.
I asked Taranchunk about it, he said he doesn't remember the purpose of this patch, and it may be useless now `¯\_(ツ)_/¯`.

## Patched

### [DynamicDrawManager_DrawDynamicThings_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/DynamicDrawManager_DrawDynamicThings_Patch.cs):
Implemented at Harmony/Verse/DynamicDrawManager_DrawDynamicThings_Patch

### [WorldComponent_GravshipController_LandingEnded_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch.cs):
Implemented at Harmony/VGE/WorldComponent_GravshipController_LandingEnded_Patch_AbortEnemyTurretFiringStates_Patch

## Fixed directly in CE classes

### [Building_TurretGun_OrderAttack_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_OrderAttack_Patch.cs):
### [Building_TurretGun_ResetForcedTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_ResetForcedTarget_Patch.cs):
We don't use VGE CompWorldArtillery, and the reset part of the patches are included in Building_GravshipTurretCE's code

### [Building_TurretGun_Active_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_Active_Patch.cs):
`bool Active()` is overridden in `Building_TurretGunCE`, so no patch required.

### [Building_TurretGun_TryFindNewTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Building_TurretGun_TryFindNewTarget_Patch.cs):
`LocalTargetInfo TryFindNewTarget() `is overridden in `Building_GravshipTurretCE`, so no patch required.

### [Verb_LaunchProjectile_TryCastShot_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/gravship2/Source/HarmonyPatches/Verb_LaunchProjectile_TryCastShot_Patch.cs):
This patch edit the value of caster to return the pawn manning the terminal - Adapted in `Verb_ShootWithVGETargeting` with the override of `Pawn CasterPawn`
