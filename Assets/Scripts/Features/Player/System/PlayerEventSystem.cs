/****************************************************
    文件：PlayerEvent.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections.Generic;
using Code_01.Mode;
using QFramework;

namespace Code_01.System
{
    public class PlayerEventSystem : AbstractSystem
    {
        private PlayerModel _playerModel;
        private PlayerDataStore _dataStore;

        protected override void OnInit()
        {
            _playerModel = this.GetModel<PlayerModel>();
            _dataStore = this.GetUtility<PlayerDataStore>();
        }

        public bool ChangeName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                LogUtility.LogError("玩家名称修改失败，名称不能为空。");
                return false;
            }
            var property = _playerModel.Data.property;
            property.Name = newName;
            return Commit(property, null);
        }

        // Change* 的数值参数均为增量；最终值只在模型校验后交给 Commit。
        public bool ChangeLevel(int value) => ChangeAll(new ItemData { changeLevel = value });
        public bool ChangeExp(long value) => ChangeAll(new ItemData { changeExp = value });
        public bool ChangePower(int value) => ChangeAll(new ItemData { changePower = value });
        public bool ChangeHp(int value) => ChangeAll(new ItemData { changeHp = value });
        public bool ChangeAttack(int value) => ChangeAll(new ItemData { changeAttack = value });
        public bool ChangeDefence(int value) => ChangeAll(new ItemData { changeDefence = value });
        public bool ChangeSpeed(int value) => ChangeAll(new ItemData { changeSpeed = value });
        public bool ChangeCoin(int value) => ChangeAll(new ItemData { changeCoin = value });
        public bool ChangeUpperPower(int value) => ChangeAll(new ItemData { changeUpperPower = value });
        public bool ChangeUpperHp(int value) => ChangeAll(new ItemData { changeUpperHp = value });
        public bool ChangeUpperAttack(int value) => ChangeAll(new ItemData { changeUpperAttack = value });
        public bool ChangeUpperDefence(int value) => ChangeAll(new ItemData { changeUpperDefence = value });
        public bool ChangeUpperSpeed(int value) => ChangeAll(new ItemData { changeUpperSpeed = value });

        public bool LevelUp()
        {
            var needExp = (long)_playerModel.Level * 100 + 100;
            if (_playerModel.Exp < needExp)
            {
                LogUtility.Log("经验不足升级");
                return false;
            }
            return ChangeAll(new ItemData { changeLevel = 1, changeExp = -needExp });
        }

        public bool ChangeGoodsDic(string goodsName, int num)
        {
            if (goodsName == "Coin") return ChangeCoin(num);
            if (string.IsNullOrEmpty(goodsName))
            {
                LogUtility.LogError("玩家库存修改失败，物品名为空。");
                return false;
            }
            if (num == 0) return true;
            return ChangeAll(default, new Dictionary<string, int> { { goodsName, num } });
        }

        /// <summary>一次完整操作：先准备全部最终值，失败不应用任何部分。</summary>
        public bool ChangeAll(ItemData data, IReadOnlyDictionary<string, int> goodsChanges = null)
        {
            PlayerData.Property finalProperty;
            Dictionary<string, int> finalGoods = null;
            try
            {
                if (!_playerModel.TryPrepareProperty(data, out finalProperty)) return false;
                if (data.changeCoin != 0 && !PrepareGoods("Coin", data.changeCoin, ref finalGoods)) return false;
                if (goodsChanges != null)
                {
                    foreach (var goods in goodsChanges)
                    {
                        if (!PrepareGoods(goods.Key, goods.Value, ref finalGoods)) return false;
                    }
                }
            }
            catch (OverflowException exception)
            {
                LogUtility.LogError("玩家属性修改失败，增量计算超过字段数值范围：" + exception);
                return false;
            }
            return Commit(finalProperty, finalGoods);
        }

        public bool EnableAttack(int costPower = 0)
        {
            if (costPower < 0)
            {
                LogUtility.LogError("攻击体力成本不能为负：" + costPower);
                return false;
            }
            return !_playerModel.IsDied && _playerModel.Power >= costPower;
        }

        private bool PrepareGoods(string goodsName, int delta, ref Dictionary<string, int> finalGoods)
        {
            if (string.IsNullOrEmpty(goodsName))
            {
                LogUtility.LogError("玩家库存修改失败，物品名为空。");
                return false;
            }
            if (delta == 0) return true;
            var exists = _playerModel.GoodsDict.TryGetValue(goodsName, out var current);
            if (finalGoods != null && finalGoods.TryGetValue(goodsName, out var prepared))
            {
                current = prepared;
                exists = goodsName == "Coin" || prepared != 0;
            }
            if (!_playerModel.TryPrepareGoods(goodsName, delta, current, exists, out var finalCount)) return false;
            if (finalGoods == null) finalGoods = new Dictionary<string, int>();
            finalGoods[goodsName] = finalCount;
            return true;
        }

        private bool Commit(PlayerData.Property finalProperty, Dictionary<string, int> finalGoods)
        {
            var previousProperty = _playerModel.Data.property;
            var propertyChanged = !previousProperty.Equals(finalProperty);
            var inventoryChanged = false;
            var coinChanged = false;
            // 只记录本次实际变动项用于保存失败回滚，不复制整个库存。
            Dictionary<string, int?> previousGoods = null;
            if (finalGoods != null)
            {
                foreach (var goods in finalGoods)
                {
                    var existed = _playerModel.GoodsDict.TryGetValue(goods.Key, out var previousCount);
                    var willExist = goods.Key == "Coin" || goods.Value != 0;
                    if (existed == willExist && (!existed || previousCount == goods.Value)) continue;
                    if (previousGoods == null) previousGoods = new Dictionary<string, int?>();
                    previousGoods.Add(goods.Key, existed ? previousCount : (int?)null);
                    if (goods.Key == "Coin") coinChanged = true;
                    else inventoryChanged = true;
                }
            }
            if (!propertyChanged && previousGoods == null) return true;

            _playerModel.ApplyProperty(finalProperty);
            if (previousGoods != null)
            {
                foreach (var goods in previousGoods)
                    _playerModel.ApplyGoods(goods.Key, finalGoods[goods.Key]);
            }
            if (!_dataStore.Save(_playerModel.Data))
            {
                _playerModel.ApplyProperty(previousProperty);
                if (previousGoods != null)
                {
                    foreach (var goods in previousGoods)
                    {
                        if (goods.Value.HasValue)
                            _playerModel.Data.goodsDict[goods.Key] = goods.Value.Value;
                        else
                            _playerModel.Data.goodsDict.Remove(goods.Key);
                    }
                }
                return false;
            }

            if (propertyChanged || coinChanged) this.SendEvent<Msg.Register.UpdateShowData>();
            if (inventoryChanged) this.SendEvent<Msg.Register.InventoryChanged>();
            return true;
        }
    }
}
