using System;
using System.IO;
using QFramework;

namespace Code_01.Mode
{
    /// <summary>沿用当前 JSON 结构与路径的玩家存储入口。</summary>
    public class PlayerDataStore : IUtility
    {
        public PlayerData Load()
        {
            var data = JsonUti.ReadFromJson<PlayerData>(MsgPaths.Config.PlayerData);
            if (data == null || data.goodsDict == null || !data.goodsDict.ContainsKey("Coin"))
            {
                throw new InvalidDataException("玩家存档缺少 PlayerData、goodsDict 或必需的 Coin 键：" + MsgPaths.Config.PlayerData);
            }
            return data;
        }

        public bool Save(PlayerData data)
        {
            try
            {
                JsonUti.WriteToJson(data, MsgPaths.Config.PlayerData);
                return true;
            }
            catch (Exception exception)
            {
                LogUtility.LogError("玩家存档保存失败，路径：" + MsgPaths.Config.PlayerData + "，异常：" + exception);
                return false;
            }
        }
    }
}
