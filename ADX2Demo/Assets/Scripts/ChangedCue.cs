using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangedCue : MonoBehaviour
{
    private CriAtomSource atomSrc;

    // Start is called before the first frame update
    void Start()
    {
        // 获取CriAtomSource 
        atomSrc = GetComponent<CriAtomSource>();
    }

    public void PlayStick(string cueName) {
        atomSrc.cueName = cueName;
        if (atomSrc != null)
        {
            atomSrc.Play(atomSrc.cueName);
        }
    }

    public void PlaySword(string cueName) {
        atomSrc.cueName = cueName;
        if (atomSrc != null)
        {
            atomSrc.Play(atomSrc.cueName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
