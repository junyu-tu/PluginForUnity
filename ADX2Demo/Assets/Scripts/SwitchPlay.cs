using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchPlay : MonoBehaviour
{
    private CriAtomSource atomSrc;

    private CriAtomExAcb acb = null;
    private CriAtomEx.CueInfo[] cueInfos;
    private int selectedCueIndex = 0;
    //选择器
    private string selectorName = "Footstep";

    void Start()
    {
        // 获取CriAtomSource 
        atomSrc = GetComponent<CriAtomSource>();
        // 根据CueSheet 名称去获取相关的acb文件
        acb = CriAtom.GetAcb(atomSrc.cueSheet);
        //Test:获取当前acb下面的所有Cue信息
        cueInfos = acb.GetCueInfoList();
        for (int i = 0; i < cueInfos.Length; i++)
        {
            Debug.Log("xxx: "+cueInfos[i].name);
        }

        //CriAtomSource脚本里面包含了 CriAtomExPlayer 对象player
        atomSrc.player.SetCue(acb, atomSrc.cueName);

        //设置当前的效果为 水的声音
        //注意：通过设置选择器里面的label 来改变当前Switch Cue下面的Track
        atomSrc.player.SetSelectorLabel(selectorName, "Water");
        //atomSrc.player.Start();
       
    }

    public void Set(int index) {
        switch (index)
        {
            case 1:
                atomSrc.player.SetSelectorLabel(selectorName, "Grass");
                break;
            case 2:
                atomSrc.player.SetSelectorLabel(selectorName, "Water");
                break;
            case 3:
                atomSrc.player.SetSelectorLabel(selectorName, "Wood");
                break;
            default:
                atomSrc.player.SetSelectorLabel(selectorName, "Grass");
                break;
        }
    }

    public void PlaySound()
    {  
        if (atomSrc != null)
        {
            atomSrc.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
