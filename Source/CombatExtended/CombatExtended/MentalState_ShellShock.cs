using RimWorld;
using Verse.AI;

namespace CombatExtended
{
    public class MentalState_ShellShock : MentalState
    {
        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}