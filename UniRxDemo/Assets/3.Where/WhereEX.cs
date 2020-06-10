using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class WhereEX : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Observable.EveryUpdate()                        
           .Where(_ => Input.GetMouseButtonDown(0))
           .Subscribe(_ =>                           
           {
                Debug.Log("mouse clicked");
           });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
