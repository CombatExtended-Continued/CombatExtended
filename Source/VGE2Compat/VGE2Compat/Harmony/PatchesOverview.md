# Overview of VGE2 patches

## Patched

### [CompPointDefence_InterceptionRadius_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs):
Implemented at Harmony/CE/Building_CIWS_CE_SpawnSetup_Patch
Implemented at Harmony/CE/Building_CIWS_CE_Tick_Patch

### [Verb_LaunchProjectile_TryCastShot_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Verb_LaunchProjectile_TryCastShot_Patch.cs):
Implemented at Harmony/CE/Verb_LaunchProjectileCE_TryCastShot_Patch

### [Building_TurretGun_TryStartShootSomething_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Building_TurretGun_TryStartShootSomething.cs):
Implemented at Harmony/CE/Building_TurretGunCE_TryStartShootSomething_Patch

### [Building_TurretGun_SpawnSetup_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Building_TurretGun_SpawnSetup_Patch.cs):
Implemented at Harmony/VGECompat/Building_GravshipTurretCE_SpawnSetup_Patch

### [Building_TurretGun_Tick_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Building_TurretGun_Tick_Patch.cs):
Implemented at Harmony/VGECompat/Building_GravshipTurretCE_Tick_Patch

## Fixed directly in CE classes

### [Verb_TicksBetweenBurstShots_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Verb_TicksBetweenBurstShots_Patch.cs):
### [Verb_BurstShotCount_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Verb_BurstShotCount_Patch.cs):
### [CompPointDefence_InterceptionRadius_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs):
Implemented in WarcomputerHelperCE.ApplyForcedMissRadiusBuffs(this Building_TurretGunCE turret) at Utils/WarcomputerHelperCE.cs

## [Projectile_Launch_Patch](https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Projectile_Launch_Patch.cs)
Added a cb for RegisterCheckForCollisionBetween in VGE2Compat that implements the VGE Gravship Armor mechanic