#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Networking.Transport;
using UnityEngine;

namespace Code_01.CombatPrototype.Networking
{
    public static class CombatPrototypeStartupSettingsStore
    {
        private static readonly UTF8Encoding ReadEncoding = new UTF8Encoding(true, true);
        private static readonly UTF8Encoding WriteEncoding = new UTF8Encoding(false, true);

        public static string SettingsPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "..", "UserSettings", "CombatPrototypeStartupSettings.json"));

        public static CombatPrototypeStartupSettings Load()
        {
            using var reader = new StreamReader(SettingsPath, ReadEncoding, false);
            var config = JObject.Parse(reader.ReadToEnd(), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            if (config.Count != 6 || ReadInteger(config, "Version") != CombatPrototypeStartupSettings.CurrentVersion)
                throw new InvalidDataException("Startup settings require exactly six fields and Version=1: " + SettingsPath);

            var gameMode = ReadString(config, "GameMode") switch
            {
                "SinglePlayer" => CombatPrototypeGameMode.SinglePlayer,
                "Online" => CombatPrototypeGameMode.Online,
                _ => throw new InvalidDataException("GameMode must be SinglePlayer or Online: " + SettingsPath)
            };
            var onlineRole = ReadString(config, "OnlineRole") switch
            {
                "Host" => CombatPrototypeOnlineRole.Host,
                "Client" => CombatPrototypeOnlineRole.Client,
                "Server" => CombatPrototypeOnlineRole.Server,
                _ => throw new InvalidDataException("OnlineRole must be Host, Client or Server: " + SettingsPath)
            };
            var port = ReadInteger(config, "Port");
            if (port < 1 || port > ushort.MaxValue)
                throw new InvalidDataException("Port must be an integer in 1..65535: " + SettingsPath);
            var background = config["RunInBackground"];
            if (background == null || background.Type != JTokenType.Boolean)
                throw new InvalidDataException("RunInBackground must be a boolean: " + SettingsPath);

            return Create(gameMode, onlineRole, ReadString(config, "ServerAddress"),
                (int)port, background.Value<bool>());
        }

        public static CombatPrototypeStartupSettings Create(CombatPrototypeGameMode gameMode,
            CombatPrototypeOnlineRole onlineRole, string serverAddress, int port, bool runInBackground)
        {
            if (!Enum.IsDefined(typeof(CombatPrototypeGameMode), gameMode) ||
                !Enum.IsDefined(typeof(CombatPrototypeOnlineRole), onlineRole))
                throw new InvalidDataException("Startup settings contain an unsupported mode or role.");
            if (port < 1 || port > ushort.MaxValue)
                throw new InvalidDataException("Port must be an integer in 1..65535.");
            if (!NetworkEndpoint.TryParse(serverAddress, (ushort)port, out var serverEndpoint, NetworkFamily.Ipv4) ||
                serverEndpoint.IsAny)
                throw new InvalidDataException("ServerAddress must be an explicit IPv4 address other than 0.0.0.0.");
            if (gameMode == CombatPrototypeGameMode.Online && !runInBackground)
                throw new InvalidDataException("Online mode requires RunInBackground=true.");

            var singlePlayer = gameMode == CombatPrototypeGameMode.SinglePlayer;
            var loopback = NetworkEndpoint.LoopbackIpv4.WithPort(singlePlayer
                ? CombatPrototypeStartupSettings.LocalIpcPort : (ushort)port);
            var listenEndpoint = singlePlayer ? loopback : NetworkEndpoint.AnyIpv4.WithPort((ushort)port);
            var connectEndpoint = !singlePlayer && onlineRole == CombatPrototypeOnlineRole.Client
                ? serverEndpoint : loopback;
            return new CombatPrototypeStartupSettings(gameMode, onlineRole, serverAddress,
                (ushort)port, runInBackground, listenEndpoint, connectEndpoint);
        }

        public static void Save(CombatPrototypeStartupSettings settings)
        {
            var config = new JObject
            {
                ["Version"] = CombatPrototypeStartupSettings.CurrentVersion,
                ["GameMode"] = settings.GameMode.ToString(),
                ["OnlineRole"] = settings.OnlineRole.ToString(),
                ["ServerAddress"] = settings.ServerAddress,
                ["Port"] = settings.Port,
                ["RunInBackground"] = settings.RunInBackground
            };
            var path = SettingsPath;
            var temporaryPath = path + ".tmp";
            var bytes = WriteEncoding.GetBytes(config.ToString(Formatting.Indented) + "\n");
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }

        private static string ReadString(JObject config, string name)
        {
            var value = config[name];
            if (value == null || value.Type != JTokenType.String)
                throw new InvalidDataException(name + " must be a string: " + SettingsPath);
            return value.Value<string>();
        }

        private static long ReadInteger(JObject config, string name)
        {
            var value = config[name];
            if (value == null || value.Type != JTokenType.Integer)
                throw new InvalidDataException(name + " must be an integer: " + SettingsPath);
            return value.Value<long>();
        }
    }
}
#endif
