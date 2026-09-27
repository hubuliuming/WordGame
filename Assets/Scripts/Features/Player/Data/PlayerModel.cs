/****************************************************
    文件：PlayerModel.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QFramework;

namespace Code_01.Mode
{
    public class PlayerModel : AbstractModel
    {
        private PlayerData _playerData;
        private ReadOnlyDictionary<string, int> _goods;

        protected override void OnInit()
        {
            _playerData = this.GetUtility<PlayerDataStore>().Load();
            _goods = new ReadOnlyDictionary<string, int>(_playerData.goodsDict);
            // 只暴露旧数据错误，不在加载时修正数值或写回文件。
            ValidateLoadedRange("Hp", Hp, UpperHp, 0, PlayerData.LimitMinHP);
            ValidateLoadedRange("Power", Power, UpperPower, 0, PlayerData.LimitMinPower);
            ValidateLoadedRange("Attack", Attack, UpperAttack, PlayerData.LimitMinAttack, PlayerData.LimitMinAttack);
            ValidateLoadedRange("Defence", Defence, UpperDefence, PlayerData.LimitMinDefence, PlayerData.LimitMinDefence);
            ValidateLoadedRange("Speed", Speed, UpperSpeed, PlayerData.LimitMinSpeed, PlayerData.LimitMinSpeed);
            foreach (var goods in _playerData.goodsDict)
            {
                if (goods.Value < 0)
                    LogUtility.LogError("玩家存档库存错误，物品：" + goods.Key + "，数量不能为负：" + goods.Value);
            }
        }

        public bool IsDied => Hp <= 0;
        public bool IsEmptyPower => Power <= 0;
        public string Name => _playerData.property.Name;
        public int Level => _playerData.property.Level;
        public long Exp => _playerData.property.Exp;
        public int Power => _playerData.property.Power;
        public int Hp => _playerData.property.Hp;
        public int Attack => _playerData.property.Attack;
        public int Defence => _playerData.property.Defence;
        public int Speed => _playerData.property.Speed;
        public int Coin => _playerData.goodsDict["Coin"];
        public int UpperPower => _playerData.property.UpperPower;
        public int UpperHp => _playerData.property.UpperHp;
        public int UpperAttack => _playerData.property.UpperAttack;
        public int UpperDefence => _playerData.property.UpperDefence;
        public int UpperSpeed => _playerData.property.UpperSpeed;
        public IReadOnlyDictionary<string, int> GoodsDict => _goods;

        internal PlayerData Data => _playerData;

        /// <summary>输入是增量，输出是尚未应用的最终属性；只校验本次涉及的属性。</summary>
        internal bool TryPrepareProperty(ItemData delta, out PlayerData.Property finalProperty)
        {
            finalProperty = _playerData.property;
            if (!TryChangeUpper(UpperHp, delta.changeUpperHp, PlayerData.LimitMinHP, "UpperHp", out finalProperty.UpperHp) ||
                !TryChangeUpper(UpperPower, delta.changeUpperPower, PlayerData.LimitMinPower, "UpperPower", out finalProperty.UpperPower) ||
                !TryChangeUpper(UpperAttack, delta.changeUpperAttack, PlayerData.LimitMinAttack, "UpperAttack", out finalProperty.UpperAttack) ||
                !TryChangeUpper(UpperDefence, delta.changeUpperDefence, PlayerData.LimitMinDefence, "UpperDefence", out finalProperty.UpperDefence) ||
                !TryChangeUpper(UpperSpeed, delta.changeUpperSpeed, PlayerData.LimitMinSpeed, "UpperSpeed", out finalProperty.UpperSpeed))
                return false;

            finalProperty.Level = checked(Level + delta.changeLevel);
            finalProperty.Exp = checked(Exp + delta.changeExp);
            if (delta.changePower != 0)
            {
                var power = checked(Power + delta.changePower);
                if (power < 0)
                {
                    LogUtility.LogError("玩家体力不足，当前体力：" + Power + "，变更量：" + delta.changePower);
                    return false;
                }
                if (!ValidateUpper(finalProperty.UpperPower, PlayerData.LimitMinPower, "UpperPower")) return false;
                finalProperty.Power = Math.Min(power, finalProperty.UpperPower);
            }
            if (delta.changeHp != 0)
            {
                if (!ValidateUpper(finalProperty.UpperHp, PlayerData.LimitMinHP, "UpperHp")) return false;
                // 死亡不解除；最终 HP 只用一次增量计算，不再把最终值当作增量判死。
                finalProperty.Hp = IsDied ? 0 : (int)Math.Max(0L, Math.Min((long)Hp + delta.changeHp, finalProperty.UpperHp));
            }
            if (!TryChangeAttribute(Attack, delta.changeAttack, finalProperty.UpperAttack, PlayerData.LimitMinAttack, "Attack", out finalProperty.Attack) ||
                !TryChangeAttribute(Defence, delta.changeDefence, finalProperty.UpperDefence, PlayerData.LimitMinDefence, "Defence", out finalProperty.Defence) ||
                !TryChangeAttribute(Speed, delta.changeSpeed, finalProperty.UpperSpeed, PlayerData.LimitMinSpeed, "Speed", out finalProperty.Speed))
                return false;
            return true;
        }

        internal bool TryPrepareGoods(string goodsName, int delta, int current, bool exists, out int finalCount)
        {
            finalCount = current;
            if (delta == 0) return true;
            var count = (long)current + delta;
            if (goodsName == "Coin")
            {
                if (count > int.MaxValue)
                {
                    LogUtility.LogError("玩家金币修改失败，物品：Coin，数量超过 int 范围。");
                    return false;
                }
                finalCount = (int)Math.Max(0L, count);
                return true;
            }
            if ((!exists && delta < 0) || count < 0 || count > int.MaxValue)
            {
                LogUtility.LogError("玩家库存修改失败，物品：" + goodsName + "，当前数量：" + current +
                                    "，变更量：" + delta + "，原因：" +
                                    (!exists && delta < 0 ? "不能扣减不存在的物品。" : "变更后数量为负或超过 int 范围。"));
                return false;
            }
            finalCount = (int)count;
            return true;
        }

        internal void ApplyProperty(PlayerData.Property finalProperty)
        {
            _playerData.property = finalProperty;
        }

        internal void ApplyGoods(string goodsName, int finalCount)
        {
            if (goodsName != "Coin" && finalCount == 0)
                _playerData.goodsDict.Remove(goodsName);
            else
                _playerData.goodsDict[goodsName] = finalCount;
        }

        private static bool TryChangeUpper(int current, int delta, int minimum, string name, out int finalValue)
        {
            finalValue = current;
            if (delta == 0) return true;
            finalValue = checked(current + delta);
            return ValidateUpper(finalValue, minimum, name);
        }

        private static bool TryChangeAttribute(int current, int delta, int upper, int minimum, string name, out int finalValue)
        {
            finalValue = current;
            if (delta == 0) return true;
            if (!ValidateUpper(upper, minimum, "Upper" + name)) return false;
            finalValue = (int)Math.Max(minimum, Math.Min((long)current + delta, upper));
            return true;
        }

        private static bool ValidateUpper(int upper, int minimum, string name)
        {
            if (upper >= minimum) return true;
            LogUtility.LogError("玩家属性修改失败，" + name + "=" + upper + "，上限不得低于下限 " + minimum + "。");
            return false;
        }

        private static void ValidateLoadedRange(string name, int value, int upper, int minimum, int upperMinimum)
        {
            if (upper < upperMinimum || value < minimum || value > upper)
                LogUtility.LogError("玩家存档属性错误，" + name + "=" + value + "，Upper" + name + "=" + upper +
                                    "，属性下限：" + minimum + "，上限下限：" + upperMinimum + "；保留旧值，不自动修正。");
        }
    }
}
