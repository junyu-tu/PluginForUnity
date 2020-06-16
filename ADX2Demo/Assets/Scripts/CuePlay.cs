using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 通过点击按钮 来播放音效
/// </summary>
public class CuePlay : MonoBehaviour
{
    private CriAtomSource atomSrc;

    // Start is called before the first frame update
    void Start()
    {
        // 获取CriAtomSource 
        atomSrc = GetComponent<CriAtomSource>();
        Debug.Log("atomSrc.status: "+ atomSrc.status);
    }

    /// <summary>
    /// 使用这个方法 不断点击，会发现声音会不断的进行播放，每一个都会叠加
    /// </summary>
    public void PlaySound()
    {
        if (atomSrc != null)
        {
            atomSrc.Play();
        }
    }

    /// <summary>
    /// 点击开始  再次点击暂停  注：再次开始音效是从头开始播放的
    /// </summary>
    public void PlayAndStopSound()
    {
        if (atomSrc != null)
        {
            // 获得CriAtomSource状态
            CriAtomSource.Status status = atomSrc.status;
            if ((status == CriAtomSource.Status.Stop) || (status == CriAtomSource.Status.PlayEnd))
            {
                /* 处于停止状态可以播放 */
                atomSrc.Play();
            }
            else
            {
                /* 停止播放 */
                atomSrc.Stop();
            }
        }
    }

}
