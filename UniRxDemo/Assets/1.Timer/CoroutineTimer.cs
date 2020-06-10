using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineTimer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Timer(5, () =>
        {
            Debug.Log("do something");
        }));
    }

    IEnumerator Timer(float seconds, Action callback) {
        yield return new WaitForSeconds(seconds);
        callback();
    }
}
