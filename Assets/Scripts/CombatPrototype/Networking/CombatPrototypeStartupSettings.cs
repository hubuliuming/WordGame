#if UNITY_EDITOR
using Unity.Networking.Transport;

namespace Code_01.CombatPrototype.Networking
{
    public enum CombatPrototypeGameMode
    {
        SinglePlayer,
        Online
    }

    public enum CombatPrototypeOnlineRole
    {
        Host,
        Client,
        Server
    }

    public sealed class CombatPrototypeStartupSettings
    {
        public const int CurrentVersion = 1;
        public const ushort LocalIpcPort = 7979;
        public const string ClientWorldName = "ClientWorld";
        public const string ServerWorldName = "ServerWorld";

        public CombatPrototypeGameMode GameMode { get; }
        public CombatPrototypeOnlineRole OnlineRole { get; }
        public string ServerAddress { get; }
        public ushort Port { get; }
        public bool RunInBackground { get; }
        public NetworkEndpoint ListenEndpoint { get; }
        public NetworkEndpoint ConnectEndpoint { get; }

        public bool IsSinglePlayer => GameMode == CombatPrototypeGameMode.SinglePlayer;
        public bool CreatesServer => IsSinglePlayer || OnlineRole != CombatPrototypeOnlineRole.Client;
        public bool CreatesClient => IsSinglePlayer || OnlineRole != CombatPrototypeOnlineRole.Server;

        internal CombatPrototypeStartupSettings(CombatPrototypeGameMode gameMode,
            CombatPrototypeOnlineRole onlineRole, string serverAddress, ushort port,
            bool runInBackground, NetworkEndpoint listenEndpoint, NetworkEndpoint connectEndpoint)
        {
            GameMode = gameMode;
            OnlineRole = onlineRole;
            ServerAddress = serverAddress;
            Port = port;
            RunInBackground = runInBackground;
            ListenEndpoint = listenEndpoint;
            ConnectEndpoint = connectEndpoint;
        }
    }
}
#endif
