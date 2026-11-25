# AOC The Cleanup Devil 1.7

Compatibility for **Millap.AOCNew** ([Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3820707817)).
The existing `Millap.AOC` compatibility folder is retained separately for the earlier mod version.
XML paths inside this folder are versioned to avoid collisions with the legacy load folder.

## Integration

- Convert guns, melee tools, armour, backpacks, race collision shapes, animals, NPC ammunition and starting equipment to CE. Preserve native equipment and ability comps.
- Preserve A2E cleaning and fuel charges. Its intake intercepts CE and native bullets for five seconds without a bullet-count cap, generates one waste bag per activation that catches bullets, and has a ten-second cooldown. Optional Character Editor previews use its UI icon.
- Keep the short spear's two-hit lightning passive and add EMP 5 within 1.9 cells. Exclude its caster from the associated flame and EMP explosions while retaining effects and ground fires.
- Resolve the long spear's skill through CE melee damage. Push independently of hit, parry, dodge or miss, while retaining the native size, mechanoid, death and obstacle exclusions. Defer the sequence while the caster is busy instead of cancelling before cooldown.
- Use CE projectiles for reusable javelins. Gungnir waits through a busy recovery stance and uses its own 36-cell verb for both target validation and firing; the regular throw remains at 32 cells.
- Handle zero final weapon toughness on the three spear forms without dividing by zero. The guard preserves incoming damage/AP and the native zero-wear outcome; other weapons and positive armour retain normal CE processing.
- Connect the skin brush, cleaner backpack, B2H eraser and dog foam abilities. Dog foam deliberately retains `Projectile_Explosive` because its native throwing code requires `Verse.Projectile`.
- Preserve the TCB Armour Piercer's delayed explosion with a dedicated slug mapping and translated ammunition readout. The shared 12-gauge slug remains unchanged.

## Weapon and ammunition settings

- TCB 26K offers single fire and two-round burst, 15 ticks apart.
- Armour Piercer uses 12-gauge slugs with direct damage 28, sharp AP 15 and blunt AP 85.2. Range multiplier 1.5 gives a base effective range of 34.5 cells. A delayed blast follows the hit target after 120 ticks: damage 8, radius 1, blunt AP 16.
- KILLAKILL uses dedicated explosive plunger rounds: single/burst/automatic modes fire 1/2/5 shots, 60 ticks apart; magazine 10, reload four seconds. The delayed explosive payload matches CE's fragmentation grenade: damage 56, radius 1.5 and 40 small fragments. The original 100-tick impact delay remains.
- KILLAKILL rounds are crafted only at the AoC printer: work 300, steel 8, FSX 4 and two industrial components produce 20 rounds. Loaded legacy ammunition is migrated without changing the loaded count or loose shared grenade ammunition.
- The plunger ammunition uses one `Graphic_Single` texture at `Textures/ThirdParty/AOC/Ammo/AOC_KILLAKILL/plunger.png`; the flying projectile retains the original AoC texture.

## Build and validation

`Source/AOCCompat` is part of the normal CE compatibility build. Its reference assemblies contain API metadata only, and are not runtime mod dependencies shipped by CE.

Local validation used the installed RimWorld 1.7 and AoC assemblies, actual CE XML patch workers across 32 DLC combinations, assembly/Harmony loading, and independent sequence fixtures. The long-spear knockback change was also confirmed in game by the contributor. These checks are not a complete colony playtest: Gungnir's full-range flight, A2E intake under sustained fire, delayed explosions and save/load during active effects still need gameplay verification.
