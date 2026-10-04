using Unity.Mathematics;
using UnityEngine;

namespace Code_01.CombatPrototype.Map
{
    internal sealed class CombatPrototypeMapInteractionHighlightProjection
    {
        private Vector2[] _unitCircle;
        private Vector2[] _screenPoints;

        internal void Configure(int segmentCount)
        {
            _unitCircle = new Vector2[segmentCount + 1];
            _screenPoints = new Vector2[segmentCount + 1];
            for (var i = 0; i < segmentCount; i++)
            {
                var angle = i * (2f * Mathf.PI / segmentCount);
                _unitCircle[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            _unitCircle[segmentCount] = _unitCircle[0];
        }

        internal void Draw(Camera camera, CombatPrototypeMapInteractionHighlightFrame frame, float width)
        {
            var viewport = camera.pixelRect;
            if (viewport.width <= width || viewport.height <= width) return;
            var clip = new Rect(viewport.x + width * 0.5f, Screen.height - viewport.yMax + width * 0.5f,
                viewport.width - width, viewport.height - width);
            for (var i = 0; i < _unitCircle.Length; i++)
            {
                var unit = _unitCircle[i];
                var projected = camera.WorldToScreenPoint(new Vector3(frame.Center.x + unit.x * frame.Radius,
                    frame.Center.y, frame.Center.z + unit.y * frame.Radius));
                if (!math.all(math.isfinite(new float3(projected.x, projected.y, projected.z))) ||
                    projected.z <= camera.nearClipPlane || projected.z >= camera.farClipPlane)
                    return;
                _screenPoints[i] = new Vector2(projected.x, Screen.height - projected.y);
            }
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            try
            {
                GUI.color = new Color(frame.Color.x, frame.Color.y, frame.Color.z, frame.Color.w);
                for (var i = 1; i < _screenPoints.Length; i++)
                {
                    var from = _screenPoints[i - 1];
                    var to = _screenPoints[i];
                    if (!ClipLine(clip, ref from, ref to)) continue;
                    var delta = to - from;
                    var length = delta.magnitude;
                    if (length <= 0f) continue;
                    var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    GUI.matrix = Matrix4x4.TRS(new Vector3(from.x, from.y, 0f),
                        Quaternion.Euler(0f, 0f, angle), Vector3.one);
                    GUI.DrawTexture(new Rect(0f, -width * 0.5f, length, width), Texture2D.whiteTexture);
                }
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }
        }

        private static bool ClipLine(Rect rect, ref Vector2 from, ref Vector2 to)
        {
            var delta = to - from;
            var first = 0f;
            var last = 1f;
            if (!Clip(-delta.x, from.x - rect.xMin, ref first, ref last) ||
                !Clip(delta.x, rect.xMax - from.x, ref first, ref last) ||
                !Clip(-delta.y, from.y - rect.yMin, ref first, ref last) ||
                !Clip(delta.y, rect.yMax - from.y, ref first, ref last))
                return false;
            to = from + last * delta;
            from += first * delta;
            return true;
        }

        private static bool Clip(float direction, float distance, ref float first, ref float last)
        {
            if (direction == 0f) return distance >= 0f;
            var ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > last) return false;
                if (ratio > first) first = ratio;
            }
            else
            {
                if (ratio < first) return false;
                if (ratio < last) last = ratio;
            }
            return true;
        }

        internal void Reset() { _unitCircle = null; _screenPoints = null; }
    }
}
