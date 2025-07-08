using Verse;

namespace CombatExtended;
public class VisibleAccessoryDefExtension : DefModExtension
{
    public int order = 1;

    private bool validated = false;

    public void Validate()
    {
        if (validated)
        {
            return;
        }

        if (order < 1)
        {
            Log.Error("CE detected VisibleAccessoryDefExtension with order lower than 1, viable values are 1-4. Clamping to 1");
            order = 1;
        }
        else if (order > 4)
        {
            Log.Error("CE detected VisibleAccessoryDefExtension with order higher than 4, viable values are 1-4. Clamping to 4");
            order = 4;
        }
        validated = true;
    }
}
