using System;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapInteractionHighlightClient
    {
        private readonly CombatPrototypeMapInteractionHighlightProjection _projection =
            new CombatPrototypeMapInteractionHighlightProjection();
        private CombatPrototypeMapInteractionHighlightSettings _settings;
        private CombatPrototypeMapInteractionHighlightFrame _f;
        private CombatPrototypeMapInteractionHighlightFrame _g;
        private Camera _camera;
        private string _mapId;

        internal void Configure(CombatPrototypeMapInteractionHighlightSettings settings, string mapId, Camera camera)
        {
            Reset();
            _settings = settings;
            if (settings.Enabled == 0 || settings.FTargetsEnabled == 0 && settings.GTargetsEnabled == 0) return;
            if (camera == null) throw new InvalidOperationException("[CombatPrototype.Map] Highlight requires the HUD's Main Camera.");
            _mapId = mapId;
            _camera = camera;
            _projection.Configure(settings.SegmentCount);
        }

        internal void Show(CombatPrototypeMapInteractionHighlightFrame f, CombatPrototypeMapInteractionHighlightFrame g)
        {
            _f = f;
            _g = g;
        }

        internal void Draw()
        {
            if (_settings.Enabled == 0 || (!_f.Visible && !_g.Visible) ||
                Event.current.type != EventType.Repaint || Screen.width <= 0 || Screen.height <= 0)
                return;
            if (!_camera.isActiveAndEnabled) return;
            var width = _settings.LineWidthPixels * Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            // Draw the F ring last; normal HUD panels are drawn after both rings by the host.
            if (_settings.GTargetsEnabled != 0 && _g.Visible)
            {
                try { _projection.Draw(_camera, _g, width); }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Highlight drawing failed; stage=DrawG, map=" + _mapId +
                        ", DropId=" + _g.TargetId + ". " + exception);
                    _g = default;
                }
            }
            if (_settings.FTargetsEnabled != 0 && _f.Visible)
            {
                try { _projection.Draw(_camera, _f, width); }
                catch (Exception exception)
                {
                    Debug.LogError("[CombatPrototype.Map] Highlight drawing failed; stage=DrawF, map=" + _mapId +
                        ", kind=" + _f.Kind + ", placement=" + _f.TargetId + ". " + exception);
                    _f = default;
                }
            }
        }

        internal void Clear() { _f = _g = default; }

        internal void Reset()
        {
            Clear();
            _settings = default;
            _camera = null;
            _mapId = null;
            _projection.Reset();
        }
    }
}
