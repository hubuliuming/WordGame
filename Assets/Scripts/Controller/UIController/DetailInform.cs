/****************************************************
    文件：DetailInform.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using Framework.UI;
using QFramework;
using UnityEngine.UI;

namespace Code_01.Controller
{
    public class DetailInform : UIBase
    {
        public string Inform;

        private Text _showText;
        private IUnRegister _useGoodsRegistration;
        private bool _initialized;

        public override void OnStart()
        {
            if (_initialized)
            {
                UpdateShow();
                return;
            }

            try
            {
                _showText = transform.Find("Text").GetComponent<Text>();
                if (_showText == null)
                    throw new InvalidOperationException("DetailInform 初始化失败，缺少 Text 组件。");
                _useGoodsRegistration = StringEventSystem.Global.Register<ItemData>(Msg.Register.UseGoods, OnUseGoods);
                _initialized = true;
                UpdateShow();
            }
            catch
            {
                Release();
                throw;
            }
        }

        public void UpdateShow()
        {
            _showText.text = Inform;
        }

        private void OnUseGoods(ItemData itemData)
        {
            // 详情使用链尚未接入，当前不应用物品效果或扣减库存。
        }

        public void Release()
        {
            _initialized = false;
            _useGoodsRegistration?.UnRegister();
            _useGoodsRegistration = null;
            _showText = null;
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}
