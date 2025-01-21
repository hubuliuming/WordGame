using UnityEngine;

namespace Code_01
{
    public class MsgPaths
    {
        public static readonly string RecoverItem = "RecoverItem";
        public struct Config
        {
// #if UNITY_ANDROID
//             public static readonly string PlayerData = "jar:file://" + Application.dataPath + "!/assets/Data/PlayerData/Player";
//             public static readonly string RecoverItem = "jar:file://" + Application.dataPath + "!/assets/Data/RecoverItem";
//             public static readonly string Enemy = "jar:file://" + Application.dataPath + "!/assets/Data/Enemy";
//
// #else
//             public static readonly string PlayerData = Application.streamingAssetsPath + "/Data/PlayerData/Player";
//             public static readonly string RecoverItem = Application.streamingAssetsPath + "/Data/RecoverItem";
//             public static readonly string Enemy = Application.streamingAssetsPath + "/Data/Enemy";
// #endif

#if UNITY_EDITOR
            // public static readonly string PlayerData = "http://127.0.0.1:8888/Player";
            public static readonly string PlayerData = Application.streamingAssetsPath + "/Data/PlayerData/Player";
            public static readonly string RecoverItem = Application.streamingAssetsPath + "/Data/RecoverItem";
            public static readonly string Enemy = Application.streamingAssetsPath + "/Data/Enemy";
#elif UNITY_ANDROID
            public static readonly string PlayerData = "jar:file://" + Application.dataPath + "!/assets/Data/PlayerData/Player";
            public static readonly string RecoverItem = "jar:file://" + Application.dataPath + "!/assets/Data/RecoverItem";
            public static readonly string Enemy = "jar:file://" + Application.dataPath + "!/assets/Data/Enemy";
#else
            public static readonly string PlayerData = Application.streamingAssetsPath + "/Data/PlayerData/Player";
            public static readonly string RecoverItem = Application.streamingAssetsPath + "/Data/RecoverItem";
            public static readonly string Enemy = Application.streamingAssetsPath + "/Data/Enemy";
#endif
        }
    }
}