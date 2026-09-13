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

        #endregion
        
        private void Start()
        {
            //WriteItemJson();
            //WriteEnemyJson();

            PlayerDetails.OnStart();
            KnapsackControl.OnStart();
        }

        public override void OnStart()
        {
            
        }

        private void Update()
        {
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
            var go = FactoryUISystem.Get(Msg.EnemyName.野猪);
            go.transform.localPosition = Vector3.zero;
        }

        private void CreateItem()
        {
            var go = FactoryUISystem.Get(Msg.ItemName.活力苹果);
            go.transform.localPosition = new Vector3(300, 0, 0);
        }

        #endregion

        public IArchitecture GetArchitecture()
        {
            return Game.Interface;
        }

   
    }
}