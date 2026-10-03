using System;
using Unity.Collections;

namespace Code_01.CombatPrototype.Map
{
    public static class CombatPrototypeMapYieldItemResolver
    {
        public const string VitalityAppleId = "vitality_apple";

        public static FixedString64Bytes Resolve(string itemId)
        {
            switch (itemId)
            {
                case VitalityAppleId:
                    return new FixedString64Bytes(global::Code_01.Msg.ItemName.活力苹果);
                default:
                    throw new InvalidOperationException("Unsupported map yieldItemId: " + itemId);
            }
        }
    }
}
