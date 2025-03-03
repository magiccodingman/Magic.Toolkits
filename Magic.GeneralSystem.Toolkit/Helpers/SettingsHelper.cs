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
            if (!string.IsNullOrWhiteSpace(propertyName))
                propertyName = propertyName.Trim();

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

            List<MemberExpression> memberPath = new();

            // Collect all members in the property chain (handling nested properties)
            while (memberExpression != null)
            {
                memberPath.Insert(0, memberExpression); // Insert at the beginning (reverse order)
                if (memberExpression.Expression is MemberExpression innerMember)
                    memberExpression = innerMember;
                else
                    break;
            }

            // Resolve the parent object (traverse the chain, stopping before the last property)
            object? parentObject = target;
            for (int i = 0; i < memberPath.Count - 1; i++)
            {
                var parentProperty = memberPath[i].Member as PropertyInfo;
                if (parentProperty == null)
                    throw new ArgumentException("Expression must refer to a property", nameof(propertyExpression));

                // Get the current value of the parent object
                var currentValue = parentProperty.GetValue(parentObject);
                if (currentValue == null)
                {
                    // Attempt to initialize it if it's a class
                    if (parentProperty.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        currentValue = Activator.CreateInstance(parentProperty.PropertyType);
                        parentProperty.SetValue(parentObject, currentValue);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot set property {parentProperty.Name} because its parent is null.");
                    }
                }
                parentObject = currentValue;
            }

            // Set the final property
            var finalProperty = memberPath[^1].Member as PropertyInfo;
            if (finalProperty == null)
                throw new ArgumentException("Expression must refer to a property", nameof(propertyExpression));

            finalProperty.SetValue(parentObject, value);
        }


    }
}
