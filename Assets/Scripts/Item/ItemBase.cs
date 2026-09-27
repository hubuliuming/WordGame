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
public class ItemBase : UIBase,IController,IBaseLife
{
    public void Init(string itemName)
    {
         var datas = JsonUti.ReadFromJson<Dictionary<string,ItemData>>(MsgPaths.Config.RecoverItem);
         var data = datas[itemName];
         transform.Find("Btn").GetComponent<Button>().onClick.AddListener(() =>
         {
             if (this.SendCommand<bool>(new UseItemCommand(data)))
             {
                 gameObject.Release();
             }
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