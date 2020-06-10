using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class Intro : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Observable.EveryUpdate()                          //开启 Update 的事件监听
            .Where(_ => Input.GetMouseButtonDown(0))      //进行一个鼠标是否抬起的判断
            .First()                                      //只获取第一次的点击事件
            .Subscribe(_ =>                               //订阅/处理事件
            {
                Debug.Log("mouse clicked");
            });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
