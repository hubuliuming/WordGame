/****************************************************
    文件：TestController.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using System.Collections;
using Code_01;
using QFramework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TestController : MonoBehaviour,IController
{
    public Button enemyBtn;
    public Button activeBtn;

    private void Start()
    {
        enemyBtn.onClick.AddListener(CreateEnemy);
        activeBtn.onClick.AddListener(CreateItem);
        
        Debug.Log("DDD    "+Application.persistentDataPath);  
        
    }

    //private IEnumerator Load()
    // {
    //     UnityWebRequest webRequest = new UnityWebRequest()
    // }
    
    private void CreateEnemy()
    {
        var go = FactoryUISystem.Get(Msg.EnemyName.野猪);
        go.transform.localPosition =Vector3.zero;
    }
    private void CreateItem()
    {
        var go =FactoryUISystem.Get(Msg.ItemName.活力苹果);
        go.transform.localPosition = new Vector3(300, 0, 0);
    }

    public IArchitecture GetArchitecture()
    {
        return Game.Interface;
    }
}