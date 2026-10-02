#if UNITY_EDITOR
using System;
using System.IO;
using Code_01.CombatPrototype.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code_01.Editor
{
    public sealed class CombatPrototypeStartupSettingsWindow : EditorWindow
    {
        private const string NetworkScenePath = "Assets/Scenes/CombatPrototypeNetCode.unity";
        private const string SpawnerScenePath = "Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity";
        private const string PlayerPrefabPath = "Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkPlayer.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkEnemy.prefab";
        private static readonly string[] GameModeLabels = { "单机", "联机" };
        private static readonly string[] OnlineRoleLabels = { "主机 Host", "客户端 Client", "服务端 Server" };

        [SerializeField] private CombatPrototypeGameMode _gameMode = CombatPrototypeGameMode.Online;
        [SerializeField] private CombatPrototypeOnlineRole _onlineRole = CombatPrototypeOnlineRole.Host;
        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private int _port = 7979;
        [SerializeField] private bool _runInBackground = true;
        [SerializeField] private bool _hasSavedSettings;
        [SerializeField] private bool _hasUnsavedChanges;
        private string _error;
        private string _status;
        private string _playerId;
        private string _identitySource;
        private string _identityError;
        private Vector2 _scroll;

        [MenuItem("Tools/CombatPrototype/启动配置")]
        public static void Open()
        {
            var window = GetWindow<CombatPrototypeStartupSettingsWindow>("启动配置");
            window.minSize = new Vector2(510f, 560f);
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            var locked = EditorApplication.isPlayingOrWillChangePlaymode;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("CombatPrototype 启动配置", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前场景", SceneManager.GetActiveScene().path);
            EditorGUILayout.HelpBox(SceneManager.GetActiveScene().path == NetworkScenePath
                ? "进入 PlayMode 前保存配置。启动时读取已保存文件，运行期间不会重读启动参数。"
                : "配置作用于带有现有网络启动标记的原型场景。当前场景可能不使用这些设置；请从 CombatPrototypeNetCode 主场景启动。",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("启动参数", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(locked))
            {
                EditorGUI.BeginChangeCheck();
                var previousMode = _gameMode;
                _gameMode = (CombatPrototypeGameMode)EditorGUILayout.Popup("游戏模式", (int)_gameMode, GameModeLabels);
                if (previousMode != _gameMode && _gameMode == CombatPrototypeGameMode.Online)
                    _runInBackground = true;
                using (new EditorGUI.DisabledScope(_gameMode == CombatPrototypeGameMode.SinglePlayer))
                    _onlineRole = (CombatPrototypeOnlineRole)EditorGUILayout.Popup("联机角色", (int)_onlineRole, OnlineRoleLabels);
                using (new EditorGUI.DisabledScope(_gameMode != CombatPrototypeGameMode.Online ||
                           _onlineRole != CombatPrototypeOnlineRole.Client))
                    _serverAddress = EditorGUILayout.TextField("服务器 IPv4", _serverAddress);
                using (new EditorGUI.DisabledScope(_gameMode == CombatPrototypeGameMode.SinglePlayer))
                    _port = EditorGUILayout.IntField("联机端口", _port);
                using (new EditorGUI.DisabledScope(_gameMode == CombatPrototypeGameMode.Online))
                    _runInBackground = EditorGUILayout.Toggle("后台运行", _runInBackground);
                if (EditorGUI.EndChangeCheck())
                {
                    _hasUnsavedChanges = true;
                    _status = null;
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("保存并校验"))
                    Save();
                if (GUILayout.Button("重新读取"))
                    Reload();
                EditorGUILayout.EndHorizontal();
            }

            if (_gameMode == CombatPrototypeGameMode.SinglePlayer)
                EditorGUILayout.HelpBox("单机创建 ServerWorld 和 ClientWorld，双方仅使用进程内 IPC。本地通道编号为 7979；联机角色、地址和端口不参与本次连接。", MessageType.Info);
            if (!_hasSavedSettings || _hasUnsavedChanges)
                EditorGUILayout.HelpBox("窗口中有未保存的候选值。启动只读取磁盘中的已保存配置；文件缺失或非法时启动会报错并中止。", MessageType.Warning);
            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            EditorGUILayout.LabelField("启动配置文件");
            EditorGUILayout.SelectableLabel(CombatPrototypeStartupSettingsStore.SettingsPath,
                EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f));

            if (locked)
                DrawActiveSettings();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("开发身份（沿用现有配置）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("客户端 World", CombatPrototypeStartupSettings.ClientWorldName);
            EditorGUILayout.LabelField("当前玩家 ID", _playerId ?? "读取失败");
            EditorGUILayout.LabelField("来源", _identitySource ?? "UNKNOWN");
            if (!string.IsNullOrEmpty(_identityError))
                EditorGUILayout.HelpBox(_identityError, MessageType.Error);
            EditorGUILayout.HelpBox("身份仍按原启动参数／身份 JSON 读取，不重复写入启动配置。纯服务端无需本地玩家 ID；同一存档目录中的同一 ID 沿用原存档。", MessageType.Info);
            if (GUILayout.Button("定位现有身份配置文件"))
                EditorUtility.RevealInFinder(IdentityPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("玩法参数（原 Inspector 为唯一来源）", EditorStyles.boldLabel);
            if (GUILayout.Button("定位敌人生成参数所在 SubScene"))
                SelectAsset(SpawnerScenePath);
            if (GUILayout.Button("定位玩家 Authoring Prefab"))
                SelectAsset(PlayerPrefabPath);
            if (GUILayout.Button("定位敌人 Authoring Prefab"))
                SelectAsset(EnemyPrefabPath);
            EditorGUILayout.HelpBox("在原 Spawner／Authoring Inspector 中编辑数值并保存，由现有 SubScene 烘焙链生效。启动窗口不覆写这些资源。", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveSettings()
        {
            var active = CombatPrototypeNetCodeBootstrap.ActiveEditorSettings;
            if (active == null)
            {
                EditorGUILayout.HelpBox("本次没有成功生效的原型启动快照。", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("本次生效配置", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("模式／角色", active.IsSinglePlayer ? "单机 / IPC" : "联机 / " + active.OnlineRole);
            if (active.CreatesServer)
                EditorGUILayout.LabelField("监听端点", active.ListenEndpoint.ToString());
            if (active.CreatesClient)
                EditorGUILayout.LabelField("连接端点", active.ConnectEndpoint.ToString());
            EditorGUILayout.LabelField("后台运行", active.RunInBackground.ToString());
        }

        private void Save()
        {
            try
            {
                var settings = CombatPrototypeStartupSettingsStore.Create(
                    _gameMode, _onlineRole, _serverAddress, _port, _runInBackground);
                CombatPrototypeStartupSettingsStore.Save(settings);
                _hasSavedSettings = true;
                _hasUnsavedChanges = false;
                _error = null;
                _status = "配置已校验并保存，下次启动生效。";
            }
            catch (Exception exception)
            {
                _error = "启动配置保存失败：" + exception.Message;
                Debug.LogError("[CombatPrototype.Startup] Settings save failed; path=" +
                               CombatPrototypeStartupSettingsStore.SettingsPath + "; " + exception);
            }
            RefreshIdentity();
        }

        private void Reload()
        {
            try
            {
                var settings = CombatPrototypeStartupSettingsStore.Load();
                _gameMode = settings.GameMode;
                _onlineRole = settings.OnlineRole;
                _serverAddress = settings.ServerAddress;
                _port = settings.Port;
                _runInBackground = settings.RunInBackground;
                _hasSavedSettings = true;
                _hasUnsavedChanges = false;
                _error = null;
                _status = "已读取保存的启动配置。";
            }
            catch (Exception exception)
            {
                _hasSavedSettings = false;
                _error = "启动配置读取失败：" + exception.Message;
                _status = null;
            }
            RefreshIdentity();
        }

        private void RefreshIdentity()
        {
            _identitySource = Array.IndexOf(Environment.GetCommandLineArgs(),
                CombatPrototypeDevelopmentIdentity.PlayerIdArgument) >= 0 ? "进程启动参数" : IdentityPath;
            try
            {
                _playerId = CombatPrototypeDevelopmentIdentity.ReadPlayerId(
                    CombatPrototypeStartupSettings.ClientWorldName).ToString();
                _identityError = null;
            }
            catch (Exception exception)
            {
                _playerId = null;
                _identityError = "身份读取失败：" + exception.Message;
            }
        }

        private void SelectAsset(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                _error = "参数资源不存在：" + path;
                Debug.LogError("[CombatPrototype.Startup] Parameter asset navigation failed; path=" + path);
                return;
            }
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static string IdentityPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "..", "UserSettings", "CombatPrototypeDevelopmentIdentity.json"));
    }
}
#endif
