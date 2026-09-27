/****************************************************
    文件：PlayerDataUI.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using Code_01.Mode;
using Framework.UI;
using QFramework;
using UnityEngine.UI;

namespace Code_01.Controller
{
    public class PlayerDetailsControl : UIBase, IController
    {
        private Text _showText;
        private PlayerModel _playerModel;
        private IUnRegister _updateRegistration;
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
                _playerModel = this.GetModel<PlayerModel>();
                if (_showText == null || _playerModel == null)
                    throw new InvalidOperationException("PlayerDetails 初始化失败，缺少 Text 组件或 PlayerModel。");
                _updateRegistration = this.RegisterEvent<Msg.Register.UpdateShowData>(o => UpdateShow());
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
            _showText.text = ("昵称：" + _playerModel.Name +
                              "\n\n等级：" + _playerModel.Level +
                              "\n\n经验：" + _playerModel.Exp +
                              "\n\n生命：" + _playerModel.Hp +
                              "\n\n体力：" + _playerModel.Power +
                              "\n\n攻击力：" + _playerModel.Attack +
                              "\n\n防御力：" + _playerModel.Defence +
                              "\n\n速度：" + _playerModel.Speed +
                              "\n\n金币：" + _playerModel.Coin);
        }

        public void Release()
        {
            _initialized = false;
            _updateRegistration?.UnRegister();
            _updateRegistration = null;
            _showText = null;
            _playerModel = null;
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
