using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class ReactivePropertyEX : MonoBehaviour
{
    //传统改变值 通知值的方式
    //public Action<int> onAgeChanged = null;
    //private int mAge = 0;
    //public int Age {
    //    get { return mAge; }
    //    set {
    //        if (mAge != value) {
    //            mAge = value;
    //            if (onAgeChanged != null) {
    //                onAgeChanged(value);
    //            }
    //        }
    //    }
    //}

    //void Start()
    //{
    //    onAgeChanged += Age => {
    //        Debug.Log("age changed!!");
    //    };
    //}




    public ReactiveProperty<int> Age = new ReactiveProperty<int>(0);

    void Start()
    {
        Age.Subscribe(age =>
        {
            Debug.Log("age changed!!");
        });

        Age.Value = 15;
    }

}
