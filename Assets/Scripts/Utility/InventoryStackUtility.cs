using System;

namespace Code_01
{
    internal static class InventoryStackUtility
    {
        // 调用方传入非负数量与已确认的正数堆叠、列数参数。
        public static int GetStackCount(int quantity, int maxStackCount)
        {
            return quantity / maxStackCount + (quantity % maxStackCount == 0 ? 0 : 1);
        }

        public static int GetStackQuantity(int quantity, int stackIndex, int maxStackCount)
        {
            return Math.Min(maxStackCount, quantity - stackIndex * maxStackCount);
        }

        public static int GetRequiredRows(int gridCount, int columnCount)
        {
            return gridCount / columnCount + (gridCount % columnCount == 0 ? 0 : 1);
        }
    }
}
