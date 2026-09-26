# Overview of VGE patches

## Not patched (not required)

### [AvoidGrid_PrintAvoidGridAroundTurret_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_Active_Patch.cs):
Not used — Building_TurretGun is not used in CE, so the patched method is never call anyway. (And CE already handles this)

### [Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch.cs):
No override required; our implementation works.

### [Building_TurretGun_TryStartShootSomething_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_TryStartShootSomething_Patch.cs):
Not adapted - our CIWS implementation works by itself

### [TurretTop_DrawTurret_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/TurretTop_DrawTurret_Patch.cs):
### [TurretTop_TurretTopTick_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/TurretTop_TurretTopTick_Patch.cs):
Not adapted — turret rotation is handled by our code.

### [CastSourceReplacer](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/CastSourceReplacer.cs):
Not adapted - Used CombatExtended.MultiBarrelExtension in PatchOperationMakeVGEBarrelsCompatible for VGE barrels

### [DamageWorker_ExplosionDamageTerrain_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/DamageWorker_ExplosionDamageTerrain_Patch.cs):
It works as it is - No need to patch anything on our side. We just need to add this to our own projectiles:
```xml
<modExtensions>
    <li Class="VanillaGravshipExpanded.SubstructureDamageExtension">
        <substructureDamageRadius>1.9</substructureDamageRadius>
    </li>
</modExtensions>
```

### [VerbUtility_ProjectileFliesOverhead_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/VerbUtility_ProjectileFliesOverhead_Patch.cs):
Unknown purpose; not adapted.
It works jointly with `Building_TurretGun_TryStartShootSomething_Patch`, making `VerbUtility.ProjectileFliesOverhead` to return `false` when VGETurret with `!CanAutoAttack` calls `TryFindNewTarget()`.
I asked Taranchunk about it, he said he doesn't remember the purpose of this patch, and it may be useless now `¯\_(ツ)_/¯`.

## Patched

### [DynamicDrawManager_DrawDynamicThings_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/DynamicDrawManager_DrawDynamicThings_Patch.cs):
Implemented at Harmony/Verse/DynamicDrawManager_DrawDynamicThings_Patch

### [WorldComponent_GravshipController_LandingEnded_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch.cs):
Implemented at Harmony/VGE/WorldComponent_GravshipController_LandingEnded_Patch_AbortEnemyTurretFiringStates_Patch

### [ShotReport_HitReportFor_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/ShotReport_HitFactorFromShooter_Patch.cs):
Implemented at Harmony/Verse/ShotReport_HitReportFor_Patch

### [Verb_LaunchProjectile_ForcedMissRadius_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Verb_LaunchProjectile_ForcedMissRadius_Patch.cs):
The GetAdjustedForcedMissRadius used to call HighlightFieldRadiusAroundTarget is patch in Harmony/VGE/Verb_LaunchProjectile_ForcedMissRadius_Patch_GetAdjustedForcedMissRadius_Patch
See also in Verb_ShootWithVGETargeting

### [GenDraw_DrawAimPie_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/GenDraw_DrawAimPie_Patch.cs):
Graphical purpose - patched in Harmony/Verse/Building_GravshipTurret_GetCastSource_Patch

## Fixed directly in CE classes

### [Building_TurretGun_OrderAttack_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_OrderAttack_Patch.cs):
### [Building_TurretGun_ResetForcedTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_ResetForcedTarget_Patch.cs):
We don't use VGE CompWorldArtillery, and the reset part of the patches are included in Building_GravshipTurretCE's code

### [Building_TurretGun_Active_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_Active_Patch.cs):
`bool Active()` is overridden in `Building_TurretGunCE`, so no patch required.

### [Building_TurretGun_TryFindNewTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Building_TurretGun_TryFindNewTarget_Patch.cs):
`LocalTargetInfo TryFindNewTarget() `is overridden in `Building_GravshipTurretCE`, so no patch required.

### [Verb_LaunchProjectile_TryCastShot_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Verb_LaunchProjectile_TryCastShot_Patch.cs):
This patch edit the value of caster to return the pawn manning the terminal - Adapted in `Verb_ShootWithVGETargeting` with the override of `Pawn CasterPawn`

### [ShotReport_HitFactorFromShooter_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/ShotReport_HitFactorFromShooter_Patch.cs):
### [Verb_LaunchProjectile_GetForcedMissTarget_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Verb_LaunchProjectile_GetForcedMissTarget_Patch.cs):
### [Verb_LaunchProjectile_ForcedMissRadius_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Verb_LaunchProjectile_ForcedMissRadius_Patch.cs):
Implemented in Verb_ShootWithVGETargeting
