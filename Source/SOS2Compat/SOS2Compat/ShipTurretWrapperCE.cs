using System.Linq;
using SaveOurShip2;
using System.Reflection;

namespace CombatExtended.Compatibility.SOS2Compat
{
    public class ShipTurretWrapperCE : Building_ShipTurret
    {
        // This is so that we can fire ShipCombatProjectile from Verb_ShootShipCE by converting Building_ShipTurretCE into something inheriting from Building_ShipTurret
        // This class uses reflection to automatically delegate all properties and fields. This could cause performance issues and/or runtime issues if setup incorrectly.
        // This class is only used by ShipCombatProjectile.

        public Building_ShipTurretCE shipTurretCE;

        public ShipTurretWrapperCE(Building_ShipTurretCE turretCE)
        {
            this.shipTurretCE = turretCE;
            // Delegate properties and fields that share the same names
            DelegateFields();
            DelegateProperties();
        }

        private void DelegateFields()
        {
            var targetType = typeof(Building_ShipTurret);
            var sourceType = typeof(Building_ShipTurretCE);

            var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var sourceField = sourceType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (sourceField != null)
                {
                    var value = sourceField.GetValue(this.shipTurretCE);
                    field.SetValue(this, value);
                }
            }
        }

        private void DelegateProperties()
        {
            var targetType = typeof(Building_ShipTurret);
            var sourceType = typeof(Building_ShipTurretCE);

            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var sourceProperty = sourceType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (sourceProperty != null && sourceProperty.CanRead && sourceProperty.CanWrite)
                {
                    var value = sourceProperty.GetValue(this.shipTurretCE);
                    property.SetValue(this, value);
                }
            }
        }

        // Override the ToString method for testing purposes
        public override string ToString()
        {
            return shipTurretCE.ToString();
        }
    }
}
