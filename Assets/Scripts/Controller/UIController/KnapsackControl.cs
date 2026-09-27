/****************************************************
    文件：Knapsack.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections.Generic;
using Code_01.Mode;
using Framework.UI;
using QFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Code_01
{
    public class KnapsackControl : UIBase, IController
    {
        private struct GridBinding
        {
            public GameObject GameObject;
            public Button Button;
            public UnityAction Click;
            public string GoodsName;
            public int Quantity;
        }

        private PlayerModel _playerModel;
        private FactoryUISystem _factoryUISystem;
        private IUnRegister _inventoryRegistration;
        private readonly List<GridBinding> _grids = new List<GridBinding>();
        private float _initialHeight;
        private bool _initialized;

        public RectTransform contextRect;

        private const int Row = 6;
        private const int Column = 10;
        private const int MaxGirdNum = 99;
        private const int RowHeight = 150;

        public override void OnStart()
        {
            if (_initialized)
            {
                UpdateGoods();
                return;
            }

            try
            {
                _playerModel = this.GetModel<PlayerModel>();
                _factoryUISystem = this.GetSystem<FactoryUISystem>();
                if (contextRect == null || _playerModel == null || _factoryUISystem == null)
                    throw new InvalidOperationException("Knapsack 初始化失败，缺少 Content、PlayerModel 或 FactoryUISystem。");
                _initialHeight = contextRect.sizeDelta.y;
                _initialized = true;
                _inventoryRegistration = this.RegisterEvent<Msg.Register.InventoryChanged>(o => UpdateGoods());
                UpdateGoods();
            }
            catch
            {
                Release();
                throw;
            }
        }

        public void UpdateGoods()
        {
            if (!_initialized || contextRect == null || _playerModel == null || _factoryUISystem == null)
            {
                LogUtility.LogError("Knapsack 刷新失败，阶段：批次前提检查，未初始化或缺少 Content、PlayerModel、FactoryUISystem。");
                return;
            }

            ClearGrids();
            foreach (var goods in _playerModel.GoodsDict)
            {
                // 金币不展示背包格子，合法归零条目不生成空格子。
                if (goods.Key == "Coin" || goods.Value == 0) continue;
                if (goods.Value < 0)
                {
                    LogUtility.LogError("Knapsack 格子生成失败，阶段：数量检查，物品：" + goods.Key +
                                        "，数量：" + goods.Value + "，资源：" + Msg.Prefab.Goods + "，原因：数量不能为负。");
                    continue;
                }

                var stackCount = InventoryStackUtility.GetStackCount(goods.Value, MaxGirdNum);
                for (var stackIndex = 0; stackIndex < stackCount; stackIndex++)
                {
                    var quantity = InventoryStackUtility.GetStackQuantity(goods.Value, stackIndex, MaxGirdNum);
                    CreateGrid(goods.Key, quantity);
                }
            }

            var needRow = InventoryStackUtility.GetRequiredRows(_grids.Count, Column);
            var addRow = Math.Max(0, needRow - Row);
            contextRect.sizeDelta = new Vector2(contextRect.sizeDelta.x, _initialHeight + addRow * RowHeight);
            LogUtility.Log("总计背包有：" + _grids.Count + "格子物品，需要的行数：" + needRow);
        }

        private void CreateGrid(string goodName, int num)
        {
            var grid = new GridBinding { GoodsName = goodName, Quantity = num };
            var stage = "加载/实例化/借出";
            try
            {
                grid.GameObject = _factoryUISystem.Get(Msg.ItemName.Goods);
                stage = "挂载容器";
                grid.GameObject.transform.SetParent(contextRect, false);
                stage = "绑定数量文本";
                grid.GameObject.transform.Find("TxtNum").GetComponent<Text>().text = num.ToString();
                stage = "绑定名称文本";
                grid.GameObject.transform.Find("TxtName").GetComponent<Text>().text = goodName;
                stage = "绑定点击监听";
                grid.Button = grid.GameObject.GetComponent<Button>();
                grid.Click = SendUseGoods;
                grid.Button.onClick.AddListener(grid.Click);
                _grids.Add(grid);
            }
            catch (Exception exception)
            {
                LogGridError(grid, stage, exception);
                ReleaseGrid(grid, true);
            }
        }

        private static void SendUseGoods()
        {
            // 真实物品选择与使用仍由后续已确认阶段接入。
            StringEventSystem.Global.Send(Msg.Register.UseGoods, new ItemData());
        }

        private void ClearGrids()
        {
            foreach (var grid in _grids)
                ReleaseGrid(grid, false);
            _grids.Clear();
        }

        private void ReleaseGrid(GridBinding grid, bool discard)
        {
            try
            {
                // 场景卸载时按钮可能已先销毁；只移除本面板添加的监听。
                if (grid.Button != null && grid.Click != null)
                    grid.Button.onClick.RemoveListener(grid.Click);
            }
            catch (Exception exception)
            {
                LogGridError(grid, "移除点击监听", exception);
                discard = true;
            }

            // 已被 Unity 销毁的对象由场景池清理登记，不再访问其组件。
            if (grid.GameObject == null) return;
            try
            {
                if (discard) _factoryUISystem.Discard(grid.GameObject);
                else _factoryUISystem.Release(grid.GameObject);
            }
            catch (Exception exception)
            {
                LogGridError(grid, discard ? "销毁失败格子" : "归还格子", exception);
            }
        }

        private static void LogGridError(GridBinding grid, string stage, Exception exception)
        {
            LogUtility.LogError("Knapsack 格子处理失败，阶段：" + stage + "，物品：" + grid.GoodsName +
                                "，数量：" + grid.Quantity + "，资源：" + Msg.Prefab.Goods + "，原始异常：" + exception);
        }

        public void Release()
        {
            var wasInitialized = _initialized;
            _initialized = false;
            _inventoryRegistration?.UnRegister();
            _inventoryRegistration = null;
            ClearGrids();
            if (wasInitialized && contextRect != null)
                contextRect.sizeDelta = new Vector2(contextRect.sizeDelta.x, _initialHeight);
            _playerModel = null;
            _factoryUISystem = null;
        }

        private void OnDestroy()
        {
            Release();
        }

        public IArchitecture GetArchitecture()
        {
            return Game.Interface;
        }
    }
}
