using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace CombatExtended;

public class RimDarkPlasmaModeExtension : DefModExtension
{
}

public class Verb_ShootCERimDarkPlasma : Verb_ShootCE
{
    private static readonly Type AmmoChangerType = AccessTools.TypeByName("Core40k.Comp_AmmoChanger");
    private static readonly PropertyInfo SelectedProjectile = AmmoChangerType == null
        ? null
        : AccessTools.Property(AmmoChangerType, "CurrentlySelectedProjectile");
    private ThingComp ammoChanger;

    private ThingDef OverchargedProjectile
    {
        get
        {
            if (SelectedProjectile == null || EquipmentSource == null)
            {
                return null;
            }
            ammoChanger ??= EquipmentSource.AllComps.FirstOrDefault(AmmoChangerType.IsInstanceOfType);
            var selected = ammoChanger == null ? null : SelectedProjectile.GetValue(ammoChanger) as ThingDef;
            return selected?.GetModExtension<RimDarkPlasmaModeExtension>() != null
                && selected.projectile is ProjectilePropertiesCE
                ? selected
                : null;
        }
    }

    // CE still loads and consumes plasma canisters in both modes. RimDark owns
    // the mode gizmo, save state, and its WarmupTime/EffectiveRange modifiers.
    public override ThingDef Projectile => OverchargedProjectile ?? base.Projectile;

    public override int ShotsPerBurstFor(FireMode mode) => OverchargedProjectile != null
        ? 1
        : base.ShotsPerBurstFor(mode);
}
