using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class UniRxTimer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(5.0f))
            .Subscribe(_ =>
            {
                Debug.Log("do something");
            });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
