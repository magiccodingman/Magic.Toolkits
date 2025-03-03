using Magic.GeneralSystem.Toolkit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public static class MagicSettingHelper
    {
        // Get the Name and Description from the attribute (or default to property name)
        public static (string Name, string? Description) GetMagicSettingInfo<T, TProperty>(
            Expression<Func<T, TProperty>> propertyExpression)
        {
            if (!(propertyExpression.Body is MemberExpression memberExpression))
                throw new ArgumentException("Expression must be a property", nameof(propertyExpression));

            if (!(memberExpression.Member is PropertyInfo propertyInfo))
                throw new ArgumentException("Expression must refer to a property", nameof(propertyExpression));

            var attribute = propertyInfo.GetCustomAttribute<MagicSettingInfoAttribute>();
            return (attribute?.Name ?? propertyInfo.Name, attribute?.Description);
        }

        // Reads input from the user and safely converts it to the correct property type
        public static TProperty ReadUserInput<TProperty>(string propertyName)
        {
            Type propertyType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);

            if (!propertyType.IsPrimitive && propertyType != typeof(string))
                throw new InvalidOperationException($"ReadUserInput only supports primitive types and strings. '{propertyType.Name}' is not supported.");

            string typeHint = propertyType == typeof(bool) ? "(true/false)" : $"({propertyType.Name.ToLower()})";

            while (true)
            {
                Console.Write($"Enter value for {propertyName} {typeHint}: ");
                string input = Console.ReadLine();

                try
                {
                    if (string.IsNullOrWhiteSpace(input) && Nullable.GetUnderlyingType(typeof(TProperty)) != null)
                    {
                        return default; // Return null for nullable types
                    }

                    return (TProperty)Convert.ChangeType(input, propertyType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid input. Error: {ex.Message}. Please try again.");
                }
            }
        }

        // Dynamically set a property based on an expression
        public static void SetPropertyValue<T, TProperty>(T target, Expression<Func<T, TProperty>> propertyExpression, TProperty value)
        {
            if (!(propertyExpression.Body is MemberExpression memberExpression))
                throw new ArgumentException("Expression must be a property", nameof(propertyExpression));

            if (!(memberExpression.Member is PropertyInfo propertyInfo))
                throw new ArgumentException("Expression must refer to a property", nameof(propertyExpression));

            propertyInfo.SetValue(target, value);
        }
    }
}
