using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapJsonReader
    {
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        public static T Read<T>(TextAsset asset, string bindingName)
        {
            if (asset == null)
                throw new InvalidDataException("Required map JSON binding is missing: " + bindingName);
            var resource = ResourcePath(asset);
            try
            {
                var bytes = asset.bytes;
                var offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
                var text = Encoding.GetString(bytes, offset, bytes.Length - offset);
                using var input = new StringReader(text);
                using var reader = new JsonTextReader(input)
                {
                    DateParseHandling = DateParseHandling.None,
                    FloatParseHandling = FloatParseHandling.Double
                };
                var token = JToken.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (reader.Read())
                    throw Error(resource, "$", "Extra content after the JSON root.");
                CheckShape(token, typeof(T), resource, "$");
                return token.ToObject<T>(JsonSerializer.Create(new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    DateParseHandling = DateParseHandling.None,
                    Culture = CultureInfo.InvariantCulture
                }));
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Map JSON requires valid UTF-8; resource=" + resource + "; path=$.", exception);
            }
            catch (JsonException exception)
            {
                var path = exception is JsonReaderException readerException ? readerException.Path :
                    exception is JsonSerializationException serializationException ? serializationException.Path : "";
                throw new InvalidDataException("Map JSON parse failed; resource=" + resource +
                    "; path=" + JsonPath(path) + "; " + exception.Message, exception);
            }
        }

        private static void CheckShape(JToken token, Type type, string resource, string path, bool nullableString = false)
        {
            if (type == typeof(string))
            {
                if (token.Type != JTokenType.String && !(nullableString && token.Type == JTokenType.Null))
                    throw Error(resource, path, nullableString ? "Expected string or null." : "Expected string.");
                return;
            }
            if (type == typeof(bool))
            {
                if (token.Type != JTokenType.Boolean) throw Error(resource, path, "Expected boolean.");
                return;
            }
            if (type == typeof(int))
            {
                if (token.Type != JTokenType.Integer ||
                    !int.TryParse(token.ToString(Formatting.None), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    throw Error(resource, path, "Expected a 32-bit integer.");
                return;
            }
            if (type == typeof(float))
            {
                if ((token.Type != JTokenType.Integer && token.Type != JTokenType.Float) ||
                    !double.TryParse(token.ToString(Formatting.None), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
                    double.IsNaN(number) || double.IsInfinity(number) || number < -float.MaxValue || number > float.MaxValue)
                    throw Error(resource, path, "Expected a finite number representable as float.");
                return;
            }
            if (type.IsArray)
            {
                if (!(token is JArray array)) throw Error(resource, path, "Expected array.");
                var elementType = type.GetElementType();
                for (var i = 0; i < array.Count; i++)
                    CheckShape(array[i], elementType, resource, path + "[" + i + "]");
                return;
            }
            if (!(token is JObject value)) throw Error(resource, path, "Expected object.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (var property in value.Properties())
                if (type.GetField(property.Name, flags) == null)
                    throw Error(resource, path + "." + property.Name, "Unknown field.");
            foreach (var field in type.GetFields(flags))
            {
                var child = value[field.Name];
                if (child == null) throw Error(resource, path + "." + field.Name, "Required field is missing.");
                CheckShape(child, field.FieldType, resource, path + "." + field.Name,
                    type == typeof(MapObjectDefinitionConfig) && field.Name == nameof(MapObjectDefinitionConfig.yieldItemId));
            }
        }

        public static string ResourcePath(TextAsset asset)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
#else
            return asset.name;
#endif
        }

        private static string JsonPath(string path) =>
            string.IsNullOrEmpty(path) ? "$" : path[0] == '[' ? "$" + path : "$." + path;

        private static InvalidDataException Error(string resource, string path, string reason) =>
            new InvalidDataException("Map JSON validation failed; resource=" + resource + "; path=" + path + "; " + reason);
    }
}
