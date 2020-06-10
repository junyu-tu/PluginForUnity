using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonTimer : MonoBehaviour
{
    private float mStartTime;
    void Start()
    {
        mStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - mStartTime > 5) {
            Debug.Log("do something");
            mStartTime = float.MaxValue;
        }
    }
}
