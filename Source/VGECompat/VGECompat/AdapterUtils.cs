using System.Linq;
using System.Reflection;

namespace CombatExtended.Compatibility.VGECompat;

/**
 *  This class uses reflection to automatically delegate all properties and fields.
 *  Performance impact can become significant if used extensively in performance-critical code.
 */
public class AdapterUtils<SourceTypeCE, TargetType> where TargetType : class, new()
{


    public static TargetType DelegateValuesToTargetType(SourceTypeCE sourceCE)
    {
        TargetType target = new TargetType();
        // Delegate properties and fields that share the same names
        DelegateFields(sourceCE, target);
        DelegateProperties(sourceCE, target);
        return target;
    }

    private static void DelegateFields(SourceTypeCE sourceCE, TargetType target)
    {
        var targetType = typeof(TargetType);
        var sourceType = typeof(SourceTypeCE);

        var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var sourceField = sourceType.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceField != null)
            {
                var value = sourceField.GetValue(sourceCE);
                field.SetValue(target, value);
            }
        }
    }

    private static void DelegateProperties(SourceTypeCE sourceCE, TargetType target)
    {
        var targetType = typeof(TargetType);
        var sourceType = typeof(SourceTypeCE);

        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var sourceProperty = sourceType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (sourceProperty != null && sourceProperty.CanRead && sourceProperty.CanWrite)
            {
                var value = sourceProperty.GetValue(sourceCE);
                property.SetValue(target, value);
            }
        }
    }
}
