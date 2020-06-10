using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
public class FirstEX : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Observable.EveryUpdate()                         //事件源/数据源   发布者
            .First(_ => Input.GetMouseButtonUp(0))       //处理 组织 整理
            .Subscribe(_ => Debug.Log("mouse clicked"))  //订阅者
            .AddTo(this);                                //生命周期绑定
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
