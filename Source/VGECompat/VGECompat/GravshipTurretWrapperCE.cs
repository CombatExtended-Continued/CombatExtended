using System.Linq;
using System.Reflection;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class GravshipTurretWrapperCE: Building_GravshipTurret
{
    // This is so that we can fire ShipCombatProjectile from Verb_ShootShipCE by converting Building_ShipTurretCE into something inheriting from Building_ShipTurret.
    // This class is only used when creating a new SaveOurShip2.ShipCombatProjectile and passing through a Building_ShipTurretCE as one of its constructor arguements.

    // This class uses reflection to automatically delegate all properties and fields. The performance impact "should" be minimal as once the delegation is complete subsequent access calls will be direct.
    // There will be an overhead for the initial conversion of each Building_ShipTurretCE though.

    public Building_GravshipTurretCE turretCE;

    public GravshipTurretWrapperCE(Building_GravshipTurretCE turretCE) : base()
    {
        this.turretCE = turretCE;
        // Delegate properties and fields that share the same names
        DelegateFields();
        DelegateProperties();
    }

    private void DelegateFields()
    {
        var targetType = typeof(Building_GravshipTurret);
        var sourceType = typeof(Building_GravshipTurretCE);

        var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var sourceField = sourceType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceField != null)
            {
                var value = sourceField.GetValue(turretCE);
                field.SetValue(this, value);
            }
        }
    }

    private void DelegateProperties()
    {
        var targetType = typeof(Building_GravshipTurret);
        var sourceType = typeof(Building_GravshipTurretCE);

        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var sourceProperty = sourceType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceProperty != null && sourceProperty.CanRead && sourceProperty.CanWrite)
            {
                var value = sourceProperty.GetValue(turretCE);
                property.SetValue(this, value);
            }
        }
    }

    public override string ToString()
    {
        return turretCE.ToString();
    }
}
