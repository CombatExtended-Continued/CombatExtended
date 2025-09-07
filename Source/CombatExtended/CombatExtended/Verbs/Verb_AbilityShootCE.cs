
using RimWorld;
using Verse;

namespace CombatExtended
{
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

        public override bool TryCastShot()
        {
            bool flag = base.TryCastShot();
            if (flag)
            {
                ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
            }
            return flag;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref ability, "ability");
            base.ExposeData();
        }
    }
}
