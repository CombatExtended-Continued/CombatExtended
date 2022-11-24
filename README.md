<p>
  <img src="Media/Icon_CE_large.png" height="128" align="right">
  <h1 align="left">Combat Extended</h1>
</p>
<p align="left">
  <a href="https://discord.gg/safyWuA">
    <img src="https://img.shields.io/badge/join%20us-black?style=for-the-badge&logo=discord&logoColor=white">
  </a>
  <a href="https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044">
    <img src="https://img.shields.io/badge/subscribe-black?style=for-the-badge&logo=steam">
  </a>
  <a href="https://github.com/CombatExtended-Continued/CombatExtended/releases">
    <img src="https://img.shields.io/badge/-latest%20release-black?style=for-the-badge&logo=github">
  </a>
  <a href="https://combatextended.lp-programming.com/CombatExtended-latest.zip">
    <img src="https://img.shields.io/badge/development%20snapshot-black?style=for-the-badge&logo=github">
  </a>
</p>

Combat Extended completely overhauls combat. It adds completely new shooting and melee mechanics, an inventory system, and rebalances the health system.

**NOTE:** This mod requires a new save to play, and should not be removed from existing saves.

## Index
- [Index](#index)
- [Development Version](#development-version)
- [Features](#features)
  - [Shooting](#shooting)
  - [Melee](#melee)
  - [Armor](#armor)
  - [Inventory](#inventory)
  - [Medical](#medical)
- [Supported Mods](#supported-third-party-mods) 

## Development Version
**Do not directly download this repository if you aren't going to build it yourself!**

This repository contains an uncompiled assembly file that will **not work** with the latest changes.

If you want to play with the latest changes without compiling the assembly yourself, you can download the [development snapshot](https://combatextended.lp-programming.com/CombatExtended-latest.zip), it is updated a few minutes after a pull request is merged to the ` Development ` branch.

Pull Requests also have their own built versions, including the changes made by said pull request. To download it, simply click on the latest link provided by the Github-Actions bot as shown below:

![image](https://user-images.githubusercontent.com/25396698/146984853-4717f30d-c6e3-4508-afd5-b7bd79cd25a9.png)

If you want to build the assembly yourself, please see [Building](Building.md) for further instructions.

## Features

### Shooting

**Projectile Rebalance**
- Vanilla percentage based shooting system is completely gone. Instead, CE uses a ballistic model where bullets fly along a trajectory and intersect hitboxes.
- Range and damage has been completely rebalanced. No more raiders shrugging off dozens of bullets or sniping with pistols.
- Guns will dominate in their assigned role and range bracket. Rifles excel at medium-long range engagements, MGs are good at area suppression, and shotguns and SMG's are lethal in close quarters.
- Bullets track their height as part of their trajectory. When striking a pawn this will affect where the bullet hits, i.e. someone crouching behind sandbags can only be shot in the head, so make sure you wear your protective helmet.

**New Mechanics**
- Weapons can switch between different aim and fire modes.
- Pawns in the open will crouch in combat when not moving, making them harder to hit.
- Projectiles create suppression on near misses. As suppression builds up, pawns will run for cover (even against orders) and suffer from significantly decreased accuracy. If suppression reaches a critical threshold, pawns will hunker down in panic and become completely unresponsive.
- **` Toggleable `** Ranged weapons require ammo and can switch between different ammo types such as armor-piercing and high-explosive rounds.
  
### Melee

**New Melee System**
- New melee stats: critical hit and parry chance.
- Critical hits produce different effects depending on the source of the attack: blunt weapons will stun the opponent, sharp weapons have twice as much armor penetration and animals cause knockdown.
- On a successful parry, the defender's weapon/shield will absorb the attack. Unarmed parries do half damage. A pawn can only parry a limited amount of times, depending on his melee skill and attack speed.
- Parries can roll for a critical hit to turn into a riposte. If successful the defender gets a free non-critical hit against the attacker.
- In combat, critical hit and parry chance are compared against the opponent's to determine final hit chances. Don't expect your level 1 fighter to parry a level 20.
- Different melee weapons provide bonuses to crit/parry/dodge.
- Dodge has been rebalanced.

### Armor
**Armor Rebalance**
- Vanilla percentage based armor system is gone. CE replaces it with a deflection-based model where projectiles have armor penetration which determines whether a bullet penetrates and how much its damage is reduced. A rifle bullet will go straight through a synthread parka but will completely bounce off of legendary power armor.

**Shields**
- New shields provide additional protection against ranged fire. Basic melee shields can be made at the smithy and will block arrows, researching machining gives access to modern ballistic shields with improved coverage and protection against gunfire.
- In melee, shields increase parry chances.
- Equipping a shield confers penalties to melee hit chance and shooting accuracy. It also prevents using two-handed weapons such as rifles, longswords and spears.

### Inventory

**Inventory System**
- Pawns can pick up various items and carry them in their inventory for easy access.
- Carrying capacity is limited by weight and bulk. High weight negatively affects movement speed. Bulk can be increased by equipping tactical vests and backpacks but will negatively affect workspeed.
- Most apparel won't add to bulk when worn but some are very heavy and/or bulky, such as armor vests. If an apparel has a "Worn bulk" stat, it will increase bulk by that much when worn.

**Loadouts**
- By default, colonists will only pick up ammo for their equipped weapon.
- Colonists can be assigned a loadout. Loadouts contain a list of things a colonist should automatically keep in his inventory, such as meals, ammo and sidearms.
- Colonists with a loadout will automatically clear their inventory of surplus items and pick up anything they're missing. Use them to automate picking up ammo, grenades and other consumables.

### Medical

**Body Part Rebalance**
- RNG death on downed has been disabled. No more wimp visitors dying from one squirrel bite.
- Body part coverage has been overhauled. Torso shots are significantly more likely to hit a vital organ.
- Limbs are overall tougher.
- Bleed rates of internal organs are *significantly* higher. Someone shot in the heart or lung will only have a few hours before they bleed out and require immediate medical attention.
- Internal organs continue bleeding when destroyed.

**Stabilizing**
- Pawns can use medicine to stabilize bleeding. Requires one medicine per stabilized pawn.
- Stabilize will temporarily reduce bleed rate of all wounds on a pawn and wears off over time. Stabilizing a heavily wounded colonist can buy you time, but it won't save him unless he gets proper treatment.
- Missing parts and internal injuries are harder to stabilize than external bleedings.
- Amount of reduction depends on medicine used and doctor skill. Don't expect the guy with 2 doctoring skill and herbal medicine to stop a destroyed lung from bleeding.

### Supported Third-Party Mods
In addition to external patches, CE comes with built-in support for a number of popular third-party mods, a [list of which can be found here](SupportedThirdPartyMods.md).

### License
This mod is a continuation and expansion of the original [Combat Extended](https://ludeon.com/forums/index.php?topic=33461.0) and is used under the [Creative Commons 4.0 License](https://creativecommons.org/licenses/by-nc-sa/4.0/).
