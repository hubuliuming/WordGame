using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

namespace Code_01.CombatPrototype.Networking
{
    [Preserve]
    public sealed class CombatPrototypeNetCodeBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            // Use NetCode's scene marker discovery; the active scene can be invalid during domain reload.
            var sceneBootstrap = DiscoverAutomaticNetcodeBootstrap();
            if (sceneBootstrap == null || sceneBootstrap.ForceAutomaticBootstrapInScene !=
                NetCodeConfig.AutomaticBootstrapSetting.EnableAutomaticBootstrap)
            {
                AutoConnectPort = 0;
                CreateLocalWorld(defaultWorldName);
                return true;
            }

            AutoConnectPort = 7979;
            Application.runInBackground = true;
            Debug.Log("[CombatPrototype.NetCode] Network scene bootstrap; port=7979, mode=" + RequestedPlayType);
            return base.Initialize(defaultWorldName);
        }
    }
}