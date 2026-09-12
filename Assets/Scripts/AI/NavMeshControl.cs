/****************************************************
    文件：NavMeshControl.cs
    作者：Y
    邮箱: 916111418@qq.com
    日期：#CreateTime#
    功能：Nothing
*****************************************************/

using System;
using Code_01;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshControl : MonoBehaviour 
{
    #region 成员变量
    private Camera _camera;
    #endregion

    private void Start()
    {
        _camera = Camera.main;
       
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var pos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log(pos);
        }
    }
}