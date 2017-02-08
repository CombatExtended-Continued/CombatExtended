using RimWorld;
using Verse;

namespace CombatExtended
{
    
    public class CompShearableRenameable : CompShearable
    {

        private string                      growthLabel = "WoolGrowth".Translate();

        private CompProperties_ShearableRenameable properties
        {
            get
            {
                return props as CompProperties_ShearableRenameable;
            }
        }

        public override void                Initialize( CompProperties props )
        {
            base.Initialize( props );
            if(
                ( properties != null )&&
                ( !properties.growthLabel.NullOrEmpty() )
            )
            {
                growthLabel = properties.growthLabel;
            }
        }

        public override string              CompInspectStringExtra()
        {
            if( !Active )
            {
                return (string)null;
            }
            return growthLabel + ": " + Fullness.ToStringPercent();
        }

    }

}
