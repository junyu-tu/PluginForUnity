using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class UpdateEX : MonoBehaviour
{
    private bool mButtonClicked = false;

    void Start()
    {
        Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                if (Input.GetMouseButtonDown(0)) {
                    Debug.Log("left mouse button clicked");
                    mButtonClicked = true;
                }
            });

        Observable.EveryUpdate()
           .Subscribe(_ =>
           {
               if (Input.GetMouseButtonDown(1))
               {
                   Debug.Log("right mouse button clicked");
                   mButtonClicked = false;
               }
           });
    }

}
