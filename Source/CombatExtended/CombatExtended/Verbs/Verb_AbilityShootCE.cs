
using RimWorld;
using Verse;

namespace CombatExtended;
public class Verb_AbilityShootCE : Verb_ShootCE, IAbilityVerb
{
    private Ability ability;

    public Ability Ability
    {
        get
        {
            return ability;
        }
        set
        {
            ability = value;
        }
    }
    public override VerbPropertiesCE VerbPropsCE => Ability.def.verbProperties as VerbPropertiesCE;
    public override ThingDef Projectile => Ability.def.verbProperties.defaultProjectile;

    public override bool Available()
    {
        if (ability.OnCooldown)
        {
            return false;
        }
        return base.Available();
    }
    public override bool TryCastShot()
    {
        if (ability.OnCooldown)
        {
            return false;
        }
        return base.TryCastShot() && ability.Activate(currentTarget, currentDestination);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref ability, "ability");
        base.ExposeData();
    }
}
