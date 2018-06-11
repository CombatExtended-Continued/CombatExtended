using RimWorld;
using Verse.AI;

namespace CombatExtended
{
    public class MentalState_CombatFrenzy : MentalState
    {
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}