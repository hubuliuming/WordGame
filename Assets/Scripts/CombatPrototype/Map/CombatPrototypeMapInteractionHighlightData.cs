using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    public struct CombatPrototypeMapInteractionHighlightSettings : IComponentData
    {
        public byte Enabled;
        public byte FTargetsEnabled;
        public byte GTargetsEnabled;
        public float GatherRadius;
        public float TreeRadius;
        public float MineRadius;
        public float DropRadius;
        public float LineWidthPixels;
        public int SegmentCount;
        public float3 ReadyColor;
        public float3 WorkingColor;
        public float3 PickupColor;
        public float Opacity;
        public float HeightOffset;

        // The configuration boundary has already validated #RRGGBB.
        internal static float3 ColorFromHex(string value)
        {
            var rgb = Convert.ToUInt32(value.Substring(1), 16);
            return new float3((rgb >> 16) & 255u, (rgb >> 8) & 255u, rgb & 255u) / 255f;
        }
    }

    internal struct CombatPrototypeMapInteractionHighlightFrame
    {
        public bool Visible;
        public byte Kind;
        public int TargetId;
        public float3 Center;
        public float Radius;
        public float4 Color;
    }
}
