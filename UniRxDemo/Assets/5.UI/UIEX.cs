using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class UIEX : MonoBehaviour
{
    // Start is called before the first frame update
    public Button button;
    public Toggle toggle;
    public Image img;

    void Start()
    {
        button.OnClickAsObservable()
            .Subscribe(_ =>
            {
                Debug.Log("button clicked!");
            });

        toggle.OnValueChangedAsObservable()
           .Subscribe(on =>
           {
               Debug.Log("toggle clicked!: "+on);
           });

        img.OnBeginDragAsObservable()
            .Subscribe(_ =>
            {
                Debug.Log("began drag");
            });

        img.OnDragAsObservable()
            .Subscribe(_ =>
            {
                Debug.Log("draging");
            });

        img.OnEndDragAsObservable()
            .Subscribe(_ =>
            {
                Debug.Log("end drag");
            });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
