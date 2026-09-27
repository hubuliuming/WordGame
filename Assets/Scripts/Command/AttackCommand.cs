/****************************************************
    文件：AttackCommand.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System.Collections.Generic;
using Code_01.Enemy;
using Code_01.Mode;
using Code_01.System;
using QFramework;
using UnityEngine;

namespace Code_01.Command
{
    public class AttackCommand : AbstractCommand
    {
        private PlayerModel _playerModel;
        private PlayerEventSystem _playerEventSystem;
        private EnemyData _data;
        private GameObject _curObj;
        public AttackCommand() { }

        public AttackCommand(GameObject obj)
        {
            _curObj = obj;
            _data = obj.GetComponent<EnemyBase>().data;
        }

        protected override void OnExecute()
        {
            _playerModel = this.GetModel<PlayerModel>();
            _playerEventSystem = this.GetSystem<PlayerEventSystem>();
            if (!_playerEventSystem.EnableAttack(_data.CostPower))
            {
                Debug.Log("玩家已经死亡或者体力不足");
                return;
            }

            var playerHp = AttackPlayer();
            var won = AttackResult();
            var changes = new ItemData
            {
                changePower = -_data.CostPower,
                changeHp = Mathf.Max(0, playerHp) - _playerModel.Hp
            };
            Dictionary<string, int> goods = null;
            if (won)
            {
                if (string.IsNullOrEmpty(_data.award.GoodsName))
                {
                    LogUtility.LogError("战斗结算失败，敌人：" + _curObj.name + "，掉落物品名为空。");
                    return;
                }
                changes.changeExp = _data.award.Exp;
                changes.changeCoin = _data.award.Coin;
                // 沿用现有掉落公式；与体力、HP、经验、金币一起提交。
                goods = DropSystem.GetRangeGoods(_data.award.GoodsName, 1, 3);
            }
            if (!_playerEventSystem.ChangeAll(changes, goods)) return;

            Debug.Log("战斗结果:" + won);
            if (won)
            {
                foreach (var goodsItem in goods)
                    Debug.Log("战利品为经验值:" + _data.award.Exp + ",金币:" + _data.award.Coin +
                              ",物品为:" + goodsItem.Value + "个" + goodsItem.Key);
                _curObj.Release();
            }
        }

        private int AttackPlayer()
        {
            int playerHp = _playerModel.Hp;
            while (_data.HP > 0 && playerHp > 0)
            {
                // 玩家先手；保留每轮双方都执行攻击的现有规则。
                if (_playerModel.Speed >= _data.Speed)
                {
                    _data.HP -= AttackMath.AttackValue(_playerModel.Attack, _data.Defence);
                    playerHp -= AttackMath.AttackValue(_data.Attack, _playerModel.Defence);
                }
                else
                {
                    playerHp -= AttackMath.AttackValue(_data.Attack, _playerModel.Defence);
                    _data.HP -= AttackMath.AttackValue(_playerModel.Attack, _data.Defence);
                }
            }
            return playerHp;
        }

        private bool AttackResult()
        {
            return _data.HP <= 0;
        }
    }
}
