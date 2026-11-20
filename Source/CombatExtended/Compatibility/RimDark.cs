using System;
using HarmonyLib;
using Verse;

namespace CombatExtended.Compatibility;

public class RimDark : IPatch
{
    private Type dropPodType;

    public bool CanInstall()
    {
        dropPodType = AccessTools.TypeByName("Genes40k.Building_DropDrop");
        return dropPodType != null;
    }

    public void Install()
    {
        // Steel Rain casts its payload to this type. Keep RimDark's building and
        // its deployment/lifetime code, and register its gun with CE instead.
        TurretRegistry.RegisterReloadableTurret(dropPodType,
            (turret, value) =>
            {
                if (turret.TryGetComp<CompRimDarkSummonedTurret>() is { } comp)
                {
                    comp.Reloading = value;
                }
            },
            turret => turret.TryGetComp<CompRimDarkSummonedTurret>()?.Reloading ?? false,
            turret => turret.TryGetComp<CompRimDarkSummonedTurret>()?.Gun,
            turret => turret.TryGetComp<CompRimDarkSummonedTurret>()?.Ammo);
    }
}
