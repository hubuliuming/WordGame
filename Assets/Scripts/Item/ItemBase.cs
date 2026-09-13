/****************************************************
    文件：ItemBase.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections.Generic;
using Code_01;
using Code_01.Command;
using Framework.UI;
using QFramework;
using UnityEngine.UI;
using YFramework.UI;


public interface IItem
{
    void Init(string itemName);
}
public class ItemBase : UIBase,IController,IInit
{
    [Serializable]
    public struct ItemData
    {
        public int changeHp;
        public int changePower;
        public int changeLevel;
        public long changeExp;
        public int changeAttack;
        public int changeDefence;
        public int changeSpeed;
        public int changeCoin;
        
        public int changeUpperHp;
        public int changeUpperPower;
        public int changeUpperAttack;
        public int changeUpperDefence;
        public int changeUpperSpeed;
    }
    
    public void Init(string itemName)
    {
         var datas = JsonUti.ReadFromJson<Dictionary<string,ItemData>>(MsgPaths.Config.RecoverItem);
         var data = datas[itemName];
         transform.Find("Btn").GetComponent<Button>().onClick.AddListener(() =>
         {
             this.SendCommand(new UseItemCommand(data));
             gameObject.Release();
         });
    }

    public IArchitecture GetArchitecture()
    {
        return Game.Interface;
    }

    public override void OnStart()
    {
       
    }
}