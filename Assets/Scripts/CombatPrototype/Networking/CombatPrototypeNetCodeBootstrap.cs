using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Networking.Transport;
#endif

namespace Code_01.CombatPrototype.Networking
{
    [Preserve]
    public sealed class CombatPrototypeNetCodeBootstrap : ClientServerBootstrap
    {
#if UNITY_EDITOR
        public static CombatPrototypeStartupSettings ActiveEditorSettings { get; private set; }
#endif

        public override bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            ActiveEditorSettings = null;
            AutoConnectPort = 0;
            DefaultConnectAddress = NetworkEndpoint.LoopbackIpv4;
            DefaultListenAddress = NetworkEndpoint.AnyIpv4;
            NetworkStreamReceiveSystem.DriverConstructor = DefaultDriverBuilder.DefaultDriverConstructor;
#endif
            // Use NetCode's scene marker discovery; the active scene can be invalid during domain reload.
            var sceneBootstrap = DiscoverAutomaticNetcodeBootstrap();
            if (sceneBootstrap == null || sceneBootstrap.ForceAutomaticBootstrapInScene !=
                NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap)
            {
                AutoConnectPort = 0;
                CreateLocalWorld(defaultWorldName);
                return true;
            }

#if UNITY_EDITOR
            return InitializeEditorWorlds();
#else
            AutoConnectPort = 7979;
            Application.runInBackground = true;
            Debug.Log("[CombatPrototype.NetCode] Network scene bootstrap; port=7979, mode=" + RequestedPlayType);
            return base.Initialize(defaultWorldName);
#endif
        }

#if UNITY_EDITOR
        private static bool InitializeEditorWorlds()
        {
            var serverCount = ServerWorlds.Count;
            var clientCount = ClientWorlds.Count;
            var previousInjectionWorld = World.DefaultGameObjectInjectionWorld;
            var previousRunInBackground = Application.runInBackground;
            try
            {
                var settings = CombatPrototypeStartupSettingsStore.Load();
                NetworkStreamReceiveSystem.DriverConstructor = new CombatPrototypeStartupDriverConstructor(settings);
                Application.runInBackground = settings.RunInBackground;

                // AutoConnectPort stays zero so PlayMode Tools cannot override these explicit endpoints.
                if (settings.CreatesServer)
                {
                    var server = CreateServerWorld(CombatPrototypeStartupSettings.ServerWorldName);
                    using var driverQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                    if (!driverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(settings.ListenEndpoint))
                        throw new InvalidOperationException("Server listen failed; endpoint=" + settings.ListenEndpoint);
                }
                if (settings.CreatesClient)
                {
                    var client = CreateClientWorld(CombatPrototypeStartupSettings.ClientWorldName);
                    using var driverQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                    if (driverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(
                            client.EntityManager, settings.ConnectEndpoint) == Entity.Null)
                        throw new InvalidOperationException("Client connect failed; endpoint=" + settings.ConnectEndpoint);
                }

                ActiveEditorSettings = settings;
                Debug.Log("[CombatPrototype.Startup] Editor bootstrap; mode=" + settings.GameMode +
                          ", role=" + (settings.IsSinglePlayer ? "LocalClientAndServer" : settings.OnlineRole.ToString()) +
                          ", listen=" + (settings.CreatesServer ? settings.ListenEndpoint.ToString() : "none") +
                          ", connect=" + (settings.CreatesClient ? settings.ConnectEndpoint.ToString() : "none") +
                          ", runInBackground=" + settings.RunInBackground);
                return true;
            }
            catch (Exception exception)
            {
                DisposeCreatedWorlds(ClientWorlds, clientCount);
                DisposeCreatedWorlds(ServerWorlds, serverCount);
                World.DefaultGameObjectInjectionWorld = previousInjectionWorld;
                Application.runInBackground = previousRunInBackground;
                NetworkStreamReceiveSystem.DriverConstructor = DefaultDriverBuilder.DefaultDriverConstructor;
                Debug.LogError("[CombatPrototype.Startup] Editor bootstrap failed; path=" +
                               CombatPrototypeStartupSettingsStore.SettingsPath + "; " + exception);
                throw;
            }
        }

        private static void DisposeCreatedWorlds(List<World> worlds, int preservedCount)
        {
            while (worlds.Count > preservedCount)
            {
                var index = worlds.Count - 1;
                var world = worlds[index];
                var worldName = world.Name;
                worlds.RemoveAt(index);
                try
                {
                    ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);
                    world.Dispose();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Startup] Failed startup world cleanup; world=" +
                                   worldName + "; " + exception);
                }
            }
        }
#endif
    }
}
