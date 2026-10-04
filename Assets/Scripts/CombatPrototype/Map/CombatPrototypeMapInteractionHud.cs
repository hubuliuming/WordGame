using System;
using Unity.NetCode;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    [DisallowMultipleComponent]
    public sealed class CombatPrototypeMapInteractionHud : MonoBehaviour
    {
        private Unity.Entities.World _clientWorld;
        private CombatPrototypeMapInteractionHudBindingSystem _binding;
        private CombatPrototypeMapInteractionHudSettings _settings;
        private GUIStyle _labelStyle;
        private string _gatherLabel;
        private string _treeLabel;
        private string _mineLabel;
        private string _text;
        private CombatPrototypeMapInteractionHudMode _mode;
        private byte _kind;
        private int _percent = -1;
        private float _progress;
        private bool _visible;

        private void OnEnable()
        {
            BindCurrentWorld();
        }

        private void Update()
        {
            // Scene and network-world lifetimes can end or restart independently.
            if (_clientWorld != ClientServerBootstrap.ClientWorld ||
                (_clientWorld != null && !_clientWorld.IsCreated)) BindCurrentWorld();
        }

        private void BindCurrentWorld()
        {
            Unbind();
            var world = ClientServerBootstrap.ClientWorld;
            // Server-only startup and a disposed client world have no local display.
            if (world == null || !world.IsCreated) return;
            var binding = world.GetExistingSystemManaged<CombatPrototypeMapInteractionHudBindingSystem>();
            if (binding == null)
                throw new InvalidOperationException("[CombatPrototype.Map] Client World=" + world.Name +
                    " requires CombatPrototypeMapInteractionHudBindingSystem.");
            binding.RegisterHud(this);
            _clientWorld = world;
            _binding = binding;
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Unbind()
        {
            Clear();
            if (_binding != null && _clientWorld.IsCreated) _binding.UnregisterHud(this);
            _binding = null;
            _clientWorld = null;
        }

        internal void Configure(CombatPrototypeMapInteractionHudSettings settings)
        {
            _settings = settings;
            _gatherLabel = settings.GatherLabel.ToString();
            _treeLabel = settings.TreeLabel.ToString();
            _mineLabel = settings.MineLabel.ToString();
            _labelStyle = null;
            _mode = CombatPrototypeMapInteractionHudMode.Hidden;
            _kind = 0;
            _percent = -1;
            Clear();
        }

        internal void Show(CombatPrototypeMapInteractionHudState state)
        {
            if (state.Mode == CombatPrototypeMapInteractionHudMode.Hidden)
            {
                Clear();
                return;
            }
            if ((state.Mode != CombatPrototypeMapInteractionHudMode.Ready && state.Mode != CombatPrototypeMapInteractionHudMode.Working) ||
                state.Kind < (byte)CombatPrototypeMapInteractionKind.Gather || state.Kind > (byte)CombatPrototypeMapInteractionKind.Mine ||
                state.PlacementIndex < 0 || state.ProgressPermille > 1000)
                throw new InvalidOperationException("[CombatPrototype.Map] Invalid owner HUD snapshot; mode=" + state.Mode +
                    ", type=" + state.Kind + ", placement=" + state.PlacementIndex + ", progress=" + state.ProgressPermille + ".");
            var percent = state.ProgressPermille / 10;
            if (_mode != state.Mode || _kind != state.Kind || _percent != percent)
            {
                var label = (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Gather ?
                    _gatherLabel : (CombatPrototypeMapInteractionKind)state.Kind == CombatPrototypeMapInteractionKind.Tree ? _treeLabel : _mineLabel;
                _text = state.Mode == CombatPrototypeMapInteractionHudMode.Ready ? "F  " + label : label + "  " + percent + "%";
                _mode = state.Mode;
                _kind = state.Kind;
                _percent = percent;
            }
            _progress = state.ProgressPermille / 1000f;
            _visible = true;
        }

        internal void Clear()
        {
            _visible = false;
        }

        private void OnGUI()
        {
            if (!_visible || Event.current.type != EventType.Repaint || Screen.width <= 0 || Screen.height <= 0) return;
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter, fontSize = _settings.FontSize, richText = false
                };
                _labelStyle.normal.textColor = Color.white;
            }
            var scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            var panel = new Rect(Screen.width / scale * 0.5f - _settings.PanelWidthPixels * 0.5f,
                Screen.height / scale - _settings.BottomMarginPixels - _settings.PanelHeightPixels,
                _settings.PanelWidthPixels, _settings.PanelHeightPixels);
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, _settings.FontSize + 8f), _text, _labelStyle);
                if (_mode != CombatPrototypeMapInteractionHudMode.Working) return;
                var bar = new Rect(panel.x + 16f, panel.yMax - 16f - _settings.ProgressBarHeightPixels,
                    panel.width - 32f, _settings.ProgressBarHeightPixels);
                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
                GUI.color = new Color(0.85f, 0.65f, 0.3f, 1f);
                bar.width *= _progress;
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }
    }
}
