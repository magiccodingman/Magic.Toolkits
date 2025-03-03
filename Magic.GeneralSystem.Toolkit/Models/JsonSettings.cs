using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Magic.GeneralSystem.Toolkit.Helpers;
using System.Reflection;
using Magic.GeneralSystem.Toolkit.Attributes;
using System.Collections;

namespace Magic.GeneralSystem.Toolkit.Models
{
    public abstract class JsonSettings<T> where T : JsonSettings<T>
    {
        [JsonIgnore]
        public string FullDirectoryPath { get; protected set; }

        [JsonIgnore]
        public string FileName { get; protected set; }

        [JsonIgnore]
        private string FullFilePath => Path.Combine(FullDirectoryPath, FileName);

        [JsonInclude]
        public string? PasswordHash { get; private set; }

        [JsonIgnore]
        private string? _memoryPassword;

        protected JsonSettings(string fullDirectoryPath, string fileName, string? password = null)
        {
            if (string.IsNullOrWhiteSpace(fullDirectoryPath) || string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Both directory path and file name must be provided.");

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";

            FullDirectoryPath = fullDirectoryPath;
            FileName = fileName;

            // 🔎 Before loading, check if ANYTHING in the actual settings class requires encryption
            if (HasEncryptedProperties(typeof(T)))
            {
                if (password != null)
                {
                    if (PasswordHash != null && !EncryptionHelper.VerifyPassword(password, PasswordHash))
                        throw new UnauthorizedAccessException("Invalid encryption password provided.");
                    _memoryPassword = password;
                }
                else
                {
                    RequestPassword();
                }
            }

            Load(password);
        }



        private bool HasEncryptedProperties(Type type)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // Check if the property itself has the encryption attribute
                if (prop.GetCustomAttribute<MagicSettingEncryptAttribute>() != null)
                    return true;

                // If the property is a collection, check inside
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                {
                    Type? elementType = prop.PropertyType.IsArray
                        ? prop.PropertyType.GetElementType()
                        : prop.PropertyType.GenericTypeArguments.FirstOrDefault();

                    if (elementType != null && HasEncryptedProperties(elementType))
                        return true;
                }
                else if (HasEncryptedProperties(prop.PropertyType)) // Recursively check nested objects
                {
                    return true;
                }
            }

            return false;
        }

        private List<(PropertyInfo Property, Type DeclaringType)> GetEncryptedProperties(Type type, Type? parentType = null)
        {
            var encryptedProperties = new List<(PropertyInfo, Type)>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<MagicSettingEncryptAttribute>() != null)
                {
                    encryptedProperties.Add((prop, parentType ?? type));
                }

                // If the property is a collection, check inside each element type
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                {
                    Type? elementType = prop.PropertyType.IsArray
                        ? prop.PropertyType.GetElementType()
                        : prop.PropertyType.GenericTypeArguments.FirstOrDefault();

                    if (elementType != null)
                        encryptedProperties.AddRange(GetEncryptedProperties(elementType, type));
                }
                else
                {
                    encryptedProperties.AddRange(GetEncryptedProperties(prop.PropertyType, type));
                }
            }

            return encryptedProperties;
        }





        private void RequestPassword()
        {
            Console.Clear();
            Console.WriteLine("Encryption is enabled for these settings.");

            // 🟢 Check if PasswordHash exists in JSON
            string? existingPasswordHash = GetPasswordHashFromJson();
            if (!string.IsNullOrWhiteSpace(existingPasswordHash))
            {
                PasswordHash = existingPasswordHash;
            }

            if (!string.IsNullOrWhiteSpace(PasswordHash))
            {
                // 🔐 PasswordHash exists, so we keep asking until they enter the correct password
                Console.WriteLine("Please enter the encryption password:");

                while (true)
                {
                    Console.Write("> ");
                    string input = ReadHelper.ReadSecureInput() ?? "";

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.Clear();
                        Console.WriteLine("Password cannot be empty.");
                        continue;
                    }

                    if (EncryptionHelper.VerifyPassword(input, PasswordHash))
                    {
                        _memoryPassword = input;
                        Console.Clear();
                        Console.WriteLine("Password accepted.");
                        break;
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Incorrect password. Try again.");
                    }
                }
            }
            else
            {
                // 🆕 No password hash found - set a new one
                Console.WriteLine("Please create a new encryption password:");

                while (true)
                {
                    Console.Write("> ");
                    string input1 = ReadHelper.ReadSecureInput() ?? "";

                    if (string.IsNullOrWhiteSpace(input1))
                    {
                        Console.WriteLine("Password cannot be empty.");
                        continue;
                    }

                    Console.Write("Re-enter password to confirm: ");
                    string input2 = ReadHelper.ReadSecureInput() ?? "";

                    if (input1 != input2)
                    {
                        Console.WriteLine("Passwords do not match. Please try again.");
                        continue;
                    }

                    // 🔐 Store the hashed password & save
                    PasswordHash = EncryptionHelper.HashPassword(input1);
                    _memoryPassword = input1;
                    Save();

                    Console.Clear();
                    Console.WriteLine("Encryption password set successfully.");
                    break;
                }
            }
        }
       
        private string? GetPasswordHashFromJson()
        {
            try
            {
                if (!File.Exists(FullFilePath)) return null;

                using var stream = File.OpenRead(FullFilePath);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("PasswordHash", out JsonElement passwordElement))
                {
                    return passwordElement.GetString();
                }
            }
            catch
            {
                Console.WriteLine($"Warning: Unable to read password hash from {FullFilePath}.");
            }

            return null;
        }


        /*public void Save()
        {
            try
            {
                if (!Directory.Exists(FullDirectoryPath))
                    Directory.CreateDirectory(FullDirectoryPath);

                EncryptProperties(this);

                var json = JsonSerializer.Serialize((T)this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FullFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }*/

        public void Save()
        {
            try
            {
                if (!Directory.Exists(FullDirectoryPath))
                    Directory.CreateDirectory(FullDirectoryPath);

                EncryptProperties(this);

                var json = JsonSerializer.Serialize((T)this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FullFilePath, json);

                // 🔥 Now process and save any nested JsonSettings instances
                RecursivelySave(typeof(T), this, new HashSet<object>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private void RecursivelySave(Type rootType, object obj, HashSet<object> processedObjects)
        {
            if (obj == null || processedObjects.Contains(obj))
                return;

            // ✅ Track processed objects to prevent infinite recursion
            processedObjects.Add(obj);

            Type objType = obj.GetType();

            // 🔍 Fetch properties dynamically, just like in your Load method
            var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p =>
                    !p.GetIndexParameters().Any() &&  // 🚫 Skip indexer properties (e.g., Item[], Chars[])
                    !p.DeclaringType?.Namespace?.StartsWith("System.Collections") == true &&  // 🚫 Skip system collection metadata
                    !p.Name.StartsWith("System.Collections.") // 🚫 Skip interface metadata
                ).ToArray();

            foreach (var prop in properties)
            {
                try
                {
                    var propValue = prop.GetValue(obj);
                    if (propValue == null) continue;

                    Type propType = propValue.GetType();

                    // 🔥 If it's a JsonSettings<T> instance, save it
                    if (IsJsonSettingsType(propType))
                    {
                        var saveMethod = propType.GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);
                        saveMethod?.Invoke(propValue, null);
                    }

                    // 🔄 If it's a collection (List<T>, arrays), process its elements recursively
                    else if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                    {
                        foreach (var item in (IEnumerable)propValue)
                        {
                            if (item != null)
                            {
                                // ✅ Recurse into collection items EVEN IF they are NOT JsonSettings<T>
                                RecursivelySave(item.GetType(), item, processedObjects);
                            }
                        }
                    }

                    // 🔄 If it's a normal class (not a system type), process its properties recursively
                    else if (prop.PropertyType.IsClass && !propType.Namespace.StartsWith("System"))
                    {
                        RecursivelySave(prop.PropertyType, propValue, processedObjects);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to process property {prop.Name} in {objType.Name}. Error: {ex.Message}");
                }
            }
        }

        // 🔥 Utility method to check if a type inherits from JsonSettings<T>
        private bool IsJsonSettingsType(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonSettings<>))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }


        public void Load(string? password = null)
        {
            try
            {
                if (!File.Exists(FullFilePath)) return;

                var json = File.ReadAllText(FullFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("Settings file is empty. Resetting settings.");
                    return;
                }

                // Deserialize into a dictionary instead of T to avoid constructor issues
                var settingsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (settingsDict == null) return;

                // 🛑 RESET _processedDecryption to ensure a fresh run
                _processedDecryption = new HashSet<string>();

                // Get all encrypted properties BEFORE processing
                var encryptedProperties = GetEncryptedProperties(typeof(T));

                // Process each top-level property
                foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (settingsDict.TryGetValue(prop.Name, out var value))
                    {
                        try
                        {
                            object? convertedValue = value is JsonElement jsonElement
                                ? JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType)
                                : value;

                            // Recursively decrypt properties and ensure assignment
                            convertedValue = RecursivelyDecrypt(convertedValue, prop.PropertyType, encryptedProperties, this, prop);

                            // ✅ ENSURE FINAL VALUE IS SET
                            SetPropertyValue(this, prop, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to set property {prop.Name}. Value might be incompatible. Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid settings file detected. Resetting: {FullFilePath} - Error: {ex.Message}");
            }
        }



        private object? RecursivelyProcessObject(object? obj, Type targetType, Dictionary<PropertyInfo, Type> encryptedProperties)
        {
            if (obj == null) return null;

            // If it's a list, process each element
            if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
            {
                Type elementType = targetType.IsArray
                    ? targetType.GetElementType()
                    : targetType.GenericTypeArguments.FirstOrDefault();

                if (elementType != null)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var newList = (IList)Activator.CreateInstance(listType);

                    foreach (var item in (IEnumerable)obj)
                    {
                        newList.Add(RecursivelyProcessObject(item, elementType, encryptedProperties));
                    }

                    return newList;
                }
            }

            // If it's an object, process each of its properties recursively
            foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object? propValue = prop.GetValue(obj);
                object? processedValue = RecursivelyProcessObject(propValue, prop.PropertyType, encryptedProperties);

                SetPropertyValue(obj, prop, processedValue);
            }

            return obj;
        }

        private void SetPropertyValue(object target, PropertyInfo prop, object? value)
        {
            if (target == null || prop == null) return;

            var setMethod = prop.SetMethod;
            if (setMethod == null || !setMethod.IsPublic)
            {
                setMethod = prop.DeclaringType?.GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetMethod;
            }

            if (setMethod != null)
            {
                setMethod.Invoke(target, new object?[] { value });
            }
        }


        private HashSet<string> _processedDecryption = new();

        private object? RecursivelyDecrypt(object? obj, Type targetType, List<(PropertyInfo Property, Type DeclaringType)> encryptedProperties, object? parentObject = null, PropertyInfo? parentProperty = null)
        {
            if (obj == null) return null;

            // 🔍 Find if this property is actually encrypted
            var encryptedProperty = encryptedProperties.FirstOrDefault(ep => ep.Property.PropertyType == targetType);

            if (encryptedProperty.Property != null)
            {
                string propertyKey = $"{encryptedProperty.DeclaringType.FullName}.{encryptedProperty.Property.Name}";

                // ✅ If we've already processed this property, don't do it again!
                if (_processedDecryption.Contains(propertyKey))
                {
                    return obj;
                }

                if (encryptedProperty.Property.GetCustomAttribute<MagicSettingEncryptAttribute>() != null && obj is string cipherText)
                {
                    if (_memoryPassword == null)
                        throw new InvalidOperationException("Cannot decrypt properties without a password.");

                    if (string.IsNullOrWhiteSpace(cipherText))
                        return null;

                    try
                    {
                        // 🔐 Decrypt only once
                        string decryptedText = EncryptionHelper.Decrypt(cipherText, _memoryPassword);

                        object? finalValue = targetType == typeof(string)
                            ? decryptedText
                            : JsonSerializer.Deserialize(decryptedText, targetType);

                        // ✅ Mark this property as decrypted so we never process it again
                        _processedDecryption.Add(propertyKey);

                        // ✅ SET THE DECRYPTED VALUE BACK TO THE OBJECT
                        if (parentObject != null && parentProperty != null)
                        {
                            SetPropertyValue(parentObject, parentProperty, finalValue);
                        }

                        return finalValue;
                    }
                    catch
                    {
                        return cipherText;
                    }
                }
            }

            // 🔄 Process lists/collections recursively
            if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
            {
                Type elementType = targetType.IsArray
                    ? targetType.GetElementType()
                    : targetType.GenericTypeArguments.FirstOrDefault();

                if (elementType != null)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var newList = (IList)Activator.CreateInstance(listType);

                    foreach (var item in (IEnumerable)obj)
                    {
                        object? decryptedItem = RecursivelyDecrypt(item, elementType, encryptedProperties);
                        newList.Add(decryptedItem);
                    }

                    return newList;
                }
            }

            // 🔄 Process nested objects recursively (only if at least one property inside is encrypted)
            bool hasEncryptedFields = targetType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(p => encryptedProperties.Any(ep => ep.Property == p));

            if (hasEncryptedFields)
            {
                foreach (var prop in targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    // ✅ Skip properties that do NOT have MagicSettingEncrypt!
                    if (!encryptedProperties.Any(ep => ep.Property == prop)) continue;

                    object? propValue = prop.GetValue(obj);
                    object? decryptedValue = RecursivelyDecrypt(propValue, prop.PropertyType, encryptedProperties, obj, prop);

                    // ✅ ENSURE VALUE IS SET BACK
                    SetPropertyValue(obj, prop, decryptedValue);
                }
            }

            return obj;
        }

        private void EncryptProperties(object obj)
        {
            if (obj == null)
                return;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<MagicSettingEncryptAttribute>() != null && prop.GetValue(obj) is string plainValue)
                {
                    if (_memoryPassword == null)
                        throw new InvalidOperationException("Cannot save encrypted properties without a password.");

                    prop.SetValue(obj, EncryptionHelper.Encrypt(plainValue, _memoryPassword));
                }
                else
                {
                    var value = prop.GetValue(obj);
                    if (value is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                            EncryptProperties(item);
                    }
                    else
                    {
                        EncryptProperties(value);
                    }
                }
            }
        }

        private void DecryptProperties(object obj)
        {
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<MagicSettingEncryptAttribute>() != null && prop.GetValue(obj) is string cipherText)
                {
                    if (_memoryPassword == null)
                        throw new InvalidOperationException("Cannot decrypt properties without a password.");

                    prop.SetValue(obj, EncryptionHelper.Decrypt(cipherText, _memoryPassword));
                }
                else
                {
                    var value = prop.GetValue(obj);
                    if (value is IEnumerable enumerable)
                    {
                        foreach (var item in enumerable)
                            DecryptProperties(item);
                    }
                    else
                    {
                        DecryptProperties(value);
                    }
                }
            }
        }
    }
}
