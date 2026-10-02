#if UNITY_EDITOR
using Unity.Entities;
using Unity.NetCode;

namespace Code_01.CombatPrototype.Networking
{
    public sealed class CombatPrototypeStartupDriverConstructor : INetworkStreamDriverConstructor
    {
        private readonly CombatPrototypeStartupSettings _settings;

        public CombatPrototypeStartupDriverConstructor(CombatPrototypeStartupSettings settings)
        {
            _settings = settings;
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            using var networkSettings = DefaultDriverBuilder.GetNetworkClientSettings();
            if (_settings.IsSinglePlayer)
                DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, networkSettings);
            else if (_settings.OnlineRole == CombatPrototypeOnlineRole.Client)
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, networkSettings);
            else
                DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, networkSettings);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            using var networkSettings = DefaultDriverBuilder.GetNetworkServerSettings();
            if (_settings.IsSinglePlayer)
                DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, networkSettings);
            else
                DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, networkSettings);
        }
    }
}
#endif
