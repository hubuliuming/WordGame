/****************************************************
    文件：FactoryBaseSystem.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections.Generic;
using Code_01.Enemy;
using QFramework;
using UnityEngine;
using UnityEngine.Pool;
using YFramework;
using Object = UnityEngine.Object;

namespace Code_01
{
    public class FactoryUISystem : AbstractSystem
    {
        private readonly Dictionary<string, ObjectPool<GameObject>> _pools = new Dictionary<string, ObjectPool<GameObject>>();
        private readonly Dictionary<GameObject, string> _instances = new Dictionary<GameObject, string>();
        private readonly HashSet<GameObject> _borrowed = new HashSet<GameObject>();
        private Transform _itemParent;

        protected override void OnInit()
        {
            // 架构初始化不访问场景；由 MapCanvasControl 显式绑定当前场景。
        }

        public void BindScene(Transform itemParent)
        {
            if (itemParent == null)
                throw new ArgumentNullException(nameof(itemParent), "FactoryUISystem 绑定失败，缺少场景 ItemParent 引用。");

            ClearPools();
            _itemParent = itemParent;
            CreatePool(Msg.Prefab.活力苹果, Msg.ItemName.活力苹果, itemParent,
                go => go.GetComponent<IBaseLife>().Init(Msg.ItemName.活力苹果));
            CreatePool(Msg.Prefab.野猪, Msg.EnemyName.野猪, itemParent,
                go => go.GetComponent<IBaseLife>().Init(Msg.EnemyName.野猪),
                go => go.GetComponent<EnemyBase>().InitData());
            CreatePool(Msg.Prefab.Goods, Msg.ItemName.Goods, itemParent);
        }

        public GameObject Get(string name)
        {
            if (_itemParent == null)
                throw new InvalidOperationException("FactoryUISystem 借出失败，当前场景尚未绑定 ItemParent，池：" + name);
            if (!_pools.TryGetValue(name, out var pool))
                throw new InvalidOperationException("FactoryUISystem 未注册对象池：" + name);

            return pool.Get();
        }

        public void Release(GameObject go)
        {
            if (!_instances.TryGetValue(go, out var objName))
            {
                LogUtility.LogWarning("工厂对象池中不存在该物体:" + go.name);
                return;
            }
            if (!_borrowed.Remove(go))
                throw new InvalidOperationException("FactoryUISystem 不允许重复归还，池：" + objName);

            try
            {
                _pools[objName].Release(go);
            }
            catch
            {
                DestroyInstance(go, objName);
                throw;
            }
        }

        public void Discard(GameObject go)
        {
            if (!_instances.TryGetValue(go, out var objName) || !_borrowed.Contains(go))
                throw new InvalidOperationException("FactoryUISystem 只能销毁本系统当前借出的对象。");
            DestroyInstance(go, objName);
        }

        public void ClearScene(Transform itemParent)
        {
            // 已销毁的 Unity 对象也保留引用身份，旧场景不能清理新场景的池。
            if (!ReferenceEquals(_itemParent, itemParent)) return;
            ClearPools();
        }

        private void CreatePool(string path, string objName, Transform parent,
            Action<GameObject> onCreated = null, Action<GameObject> onGet = null)
        {
            _pools.Add(objName, new ObjectPool<GameObject>(
                () => OnCreate(path, objName, parent, onCreated),
                go => OnGet(go, objName, onGet),
                go =>
                {
                    go.SetActive(false);
                    go.transform.SetParent(parent, false);
                },
                go => DestroyInstance(go, objName)));
        }

        private GameObject OnCreate(string path, string objName, Transform parent, Action<GameObject> onCreated)
        {
            GameObject go = null;
            try
            {
                var prefab = Resources.Load<GameObject>(path);
                go = Object.Instantiate(prefab, parent);
                go.name = objName;
                onCreated?.Invoke(go);
                _instances.Add(go, objName);
                return go;
            }
            catch
            {
                DestroyInstance(go, objName);
                throw;
            }
        }

        private void OnGet(GameObject go, string objName, Action<GameObject> onGet)
        {
            try
            {
                go.SetActive(true);
                onGet?.Invoke(go);
                _borrowed.Add(go);
            }
            catch
            {
                DestroyInstance(go, objName);
                throw;
            }
        }

        private void DestroyInstance(GameObject go, string objName)
        {
            if (ReferenceEquals(go, null)) return;
            _instances.Remove(go);
            _borrowed.Remove(go);
            DestroyObject(go, objName);
        }

        private static void DestroyObject(GameObject go, string objName)
        {
            // 场景卸载时子对象可能已先销毁；清理不依赖父子 OnDestroy 顺序。
            if (go == null) return;
            try
            {
                Object.Destroy(go);
            }
            catch (Exception exception)
            {
                LogUtility.LogError("FactoryUISystem 销毁失败，池：" + objName + "，原始异常：" + exception);
            }
        }

        private void ClearPools()
        {
            _itemParent = null;
            foreach (var pool in _pools.Values)
                pool.Clear();
            _pools.Clear();

            // Clear 只处理池内对象，剩余登记包含被背包改挂父节点的借出对象。
            foreach (var instance in _instances)
                DestroyObject(instance.Key, instance.Value);
            _instances.Clear();
            _borrowed.Clear();
        }
    }

    public static class FactoryExtensive
    {
        public static void Release(this GameObject go) => Game.Interface.GetSystem<FactoryUISystem>().Release(go);
    }
}
