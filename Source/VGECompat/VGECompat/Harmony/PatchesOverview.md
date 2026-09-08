# Overview of VGE patches

## Not patched

- HarmonyPatches/AvoidGrid_PrintAvoidGridAroundTurret_Patch:
  Not used — Building_TurretGun is not used and CE already handles it.

- HarmonyPatches/Building_TurretGun_IsMortarOrProjectileFliesOverhead_Patch:
  No override required; our implementation works.

- HarmonyPatches/Building_TurretGun_TryStartShootSomething_Patch:
  Not required — we use different turret rotation code.

- HarmonyPatches/CastSourceReplacer:
  Unknown purpose; not adapted.

- HarmonyPatches/GenDraw_DrawAimPie_Patch:
  Unknown purpose; not adapted.

- HarmonyPatches/ShotReport_HitFactorFromShooter_Patch:
  Not applicable — we don't use ShotReport the same way.

- HarmonyPatches/TurretTop_DrawTurret_Patch
- HarmonyPatches/TurretTop_TurretTopTick_Patch:
  Not adapted — turret rotation is handled by our code.

- HarmonyPatches/Verb_LaunchProjectile_ForcedMissRadius_Patch
- HarmonyPatches/Verb_LaunchProjectile_GetForcedMissTarget_Patch:
  Not applicable — forced miss is not used in CE.

- HarmonyPatches/Verb_LaunchProjectile_TryCastShot_Patch:
  Unknown purpose; not adapted.

- HarmonyPatches/VerbUtility_ProjectileFliesOverhead_Patch:
  Unknown purpose; not adapted.

## Patched

- HarmonyPatches/DynamicDrawManager_DrawDynamicThings_Patch:
  Implemented at Harmony/Verse/DynamicDrawManager_DrawDynamicThings_Patch

- HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch:
  Implemented at Harmony/VGE/WorldComponent_GravshipController_LandingEnded_Patch_AbortEnemyTurretFiringStates_Patch

## Fixed directly in CE classes

- HarmonyPatches/Building_TurretGun_OrderAttack_Patch:
- HarmonyPatches/Building_TurretGun_ResetForcedTarget_Patch:
  We don't use VGE CompWorldArtillery, and the reset part of the patches are included in Building_GravshipTurretCE's code

- HarmonyPatches/Building_TurretGun_Active_Patch:
  bool Active() is overridden in CE's Building_TurretGun, so no patch required.

- HarmonyPatches/Building_TurretGun_TryFindNewTarget_Patch:
  LocalTargetInfo TryFindNewTarget() is overridden in Building_GravshipTurretCE, so no patch required.
