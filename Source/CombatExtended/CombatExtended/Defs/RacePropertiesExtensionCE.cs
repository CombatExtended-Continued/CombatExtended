using Verse;

namespace CombatExtended;

public class RacePropertiesExtensionCE : DefModExtension
{
    public BodyShapeDef bodyShape;
    public bool canParry = false;
    public int maxParry = 1;
    public bool overrideFactors = false;
    public float vertScale = 1f;
    public float horiScale = 1f;
}
