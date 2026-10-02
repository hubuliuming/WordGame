using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public static class CombatPrototypeDevelopmentIdentity
    {
        public const string PlayerIdArgument = "-combatPrototypePlayerId";

        public static FixedString64Bytes ReadPlayerId(string worldName)
        {
            var arguments = Environment.GetCommandLineArgs();
            string argumentPlayerId = null;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] != PlayerIdArgument)
                    continue;
                if (argumentPlayerId != null || index + 1 >= arguments.Length)
                    throw new InvalidDataException(PlayerIdArgument + " requires exactly one explicitly configured PlayerId.");
                argumentPlayerId = arguments[++index];
            }
            if (argumentPlayerId != null)
                return CombatPrototypePlayerIdentity.Parse(argumentPlayerId);

#if UNITY_EDITOR
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserSettings", "CombatPrototypeDevelopmentIdentity.json"));
            using var reader = new StreamReader(path, new UTF8Encoding(true, true), false);
            var config = JObject.Parse(reader.ReadToEnd(), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            var version = config["Version"];
            if (config.Count != 2 || version == null || version.Type != JTokenType.Integer ||
                version.Value<long>() != 1 || !(config["Clients"] is JObject clients))
                throw new InvalidDataException("Invalid development identity configuration: " + path);
            var configuredPlayerId = clients.GetValue(worldName, StringComparison.Ordinal);
            if (configuredPlayerId == null || configuredPlayerId.Type != JTokenType.String)
                throw new InvalidDataException("Missing explicit PlayerId for World=" + worldName + ", path=" + path);
            return CombatPrototypePlayerIdentity.Parse(configuredPlayerId.Value<string>());
#else
            throw new InvalidDataException("Missing required development identity argument: " + PlayerIdArgument);
#endif
        }
    }
}
