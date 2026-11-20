using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended;

public class CompProperties_RimDarkSummonedTurret : CompProperties
{
    public CompProperties_RimDarkSummonedTurret()
    {
        compClass = typeof(CompRimDarkSummonedTurret);
    }
}

public class CompRimDarkSummonedTurret : ThingComp
{
    private static readonly FieldInfo NativeGun = AccessTools.Field(typeof(Building_TurretGun), "gun");
    private bool initialized;
    private bool reloading;
    private int reloadTicks;

    public bool Reloading
    {
        get => reloading;
        set => reloading = value;
    }

    public Thing Gun => parent is Building_TurretGunCE ceTurret
        ? ceTurret.Gun
        : NativeGun?.GetValue(parent) as Thing;

    public CompAmmoUser Ammo
    {
        get
        {
            var ammo = Gun?.TryGetComp<CompAmmoUser>();
            if (ammo != null)
            {
                ammo.turret = (Building_Turret)parent;
            }
            return ammo;
        }
    }

    public void EnsureAmmoInitialized()
    {
        var ammo = Ammo;
        if (!initialized && ammo != null)
        {
            // Ability-created buildings arrive with one magazine. Do not grant
            // another magazine when a save is loaded or the building respawns.
            ammo.ResetAmmoCount();
            initialized = true;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        EnsureAmmoInitialized();
        if (parent is Building_TurretGun nativeTurret)
        {
            parent.Map.GetComponent<TurretTracker>().Register(nativeTurret);
        }
    }

    public override void CompTickInterval(int delta)
    {
        // Some turret classes create their gun after the comps' spawn callback.
        EnsureAmmoInitialized();
        var ammo = Ammo;
        if (parent is Building_TurretGun && ammo != null && !ammo.UseAmmo && ammo.EmptyMagazine && !reloading)
        {
            reloadTicks += delta;
            if (reloadTicks >= ammo.Props.reloadTime * GenTicks.TicksPerRealSecond)
            {
                ammo.ResetAmmoCount();
                reloadTicks = 0;
            }
        }
        else
        {
            reloadTicks = 0;
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (parent is Building_TurretGun nativeTurret)
        {
            map.GetComponent<TurretTracker>().Unregister(nativeTurret);
        }
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref initialized, "CE_RimDarkAmmoInitialized");
        Scribe_Values.Look(ref reloading, "CE_RimDarkReloading");
        Scribe_Values.Look(ref reloadTicks, "CE_RimDarkReloadTicks");
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        // Building_TurretGunCE already supplies its own ammo gizmos.
        if (parent is Building_TurretGun && Ammo is { } ammo)
        {
            foreach (var gizmo in ammo.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
        }
    }
}
