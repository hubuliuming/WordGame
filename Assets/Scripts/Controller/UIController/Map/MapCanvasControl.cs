using Framework.UI;
using UnityEngine;
using QFramework;
using YFramework.Extension;
using YFramework.UI;
using NotImplementedException = System.NotImplementedException;

namespace Code_01.Controller
{
    public class MapCanvasControl : UIBase
    {
      
        #region 成员变量

        public PlayerDetailsControl PlayerDetails;
        public KnapsackControl KnapsackControl;
        public Transform ItemParent;

        private FactoryUISystem _factoryUISystem;
        private bool _initialized;

        #endregion
        
        private void Start()
        {
            var architecture = GetArchitecture();
            _factoryUISystem = architecture.GetSystem<FactoryUISystem>();
            try
            {
                _factoryUISystem.BindScene(ItemParent);
                PlayerDetails.OnStart();
                KnapsackControl.OnStart();
                _initialized = true;
            }
            catch
            {
                ReleasePanels();
                _factoryUISystem.ClearScene(ItemParent);
                throw;
            }
        }

        public override void OnStart()
        {
            
        }

        private void Update()
        {
            if (!_initialized) return;
            // test
            if (Input.GetKeyDown(KeyCode.E))
            {
                CreateEnemy();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                CreateItem();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                KnapsackControl.gameObject.ActiveSelfInverse();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                PlayerDetails.gameObject.ActiveSelfInverse();
            }
            
        }

        #region TestMeoth

        private void CreateEnemy()
        {
            var go = _factoryUISystem.Get(Msg.EnemyName.野猪);
            go.transform.localPosition = Vector3.zero;
        }

        private void CreateItem()
        {
            var go = _factoryUISystem.Get(Msg.ItemName.活力苹果);
            go.transform.localPosition = new Vector3(300, 0, 0);
        }

        #endregion

        private void OnDestroy()
        {
            _initialized = false;
            ReleasePanels();
            // Start 未完成架构获取时，尚无本场景的池需要清理。
            if (_factoryUISystem != null)
                _factoryUISystem.ClearScene(ItemParent);
        }

        private void ReleasePanels()
        {
            // 先释放面板持有的监听与格子；面板自身 OnDestroy 重复调用也安全。
            if (!ReferenceEquals(KnapsackControl, null)) KnapsackControl.Release();
            if (!ReferenceEquals(PlayerDetails, null)) PlayerDetails.Release();
        }

        public IArchitecture GetArchitecture()
        {
            return Game.Interface;
        }

   
    }
}
