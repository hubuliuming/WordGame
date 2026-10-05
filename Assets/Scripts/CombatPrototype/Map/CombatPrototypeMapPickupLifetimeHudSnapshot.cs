using System;
using Unity.Mathematics;

namespace Code_01.CombatPrototype.Map
{
    internal static class CombatPrototypeMapPickupLifetimeHudSnapshot
    {
        internal static void Apply(ref CombatPrototypeMapPickupHudState frame,
            CombatPrototypeMapPickupHudSettings settings, double expiresAt, double time)
        {
            if (settings.Enabled == 0 || settings.LifetimeEnabled == 0) return;
            if (double.IsNaN(expiresAt) || double.IsInfinity(expiresAt) || expiresAt < 0d)
                throw InvalidLifetime(frame, expiresAt, time);
            if (expiresAt == 0d)
            {
                frame.LifetimeMode = CombatPrototypeMapPickupLifetimeHudMode.Permanent;
                return;
            }
            var remaining = expiresAt - time;
            var rounded = (float)Math.Ceiling(remaining);
            if (remaining <= 0d || !math.isfinite(rounded) || rounded <= 0f)
                throw InvalidLifetime(frame, expiresAt, time);
            frame.LifetimeMode = settings.ExpiryWarningEnabled != 0 && remaining <= settings.ExpiryWarningSeconds
                ? CombatPrototypeMapPickupLifetimeHudMode.ExpiringSoon : CombatPrototypeMapPickupLifetimeHudMode.Timed;
            frame.RemainingSeconds = rounded;
        }

        private static InvalidOperationException InvalidLifetime(CombatPrototypeMapPickupHudState frame,
            double expiresAt, double time) =>
            new InvalidOperationException("Invalid pickup HUD lifetime; DropId=" + frame.DropId + ", itemId=" +
                frame.ItemId + ", expiresAt=" + expiresAt + ", time=" + time + ".");
    }
}
