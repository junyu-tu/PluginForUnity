using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class ScriptOne : MonoBehaviour {

    /// <summary>
    /// Unity自带的颜色渐变的组件
    /// </summary>
    public Gradient gradient;

    public AnimationCurve _curve;

	// Use this for initialization
	void Start () {

        #region 移动 选择 缩放
        //移动到某个坐标
        //transform.DOMove(Vector3.one,2f);
        //transform.DOLocalMove(Vector3.one, 2f);

        //跳跃到某个坐标
        //transform.DOJump(Vector3.one,3f,1,1f);

        //三种旋转方式
        //transform.DORotate(new Vector3(0,90f,0),2f);
        //transform.DORotateQuaternion(new Quaternion(0.1f, 0.1f, 0.1f, 0.1f),2f);
        //transform.DOLookAt(Vector3.one, 2f); //朝向某个方向

        //缩放
        //transform.DOScale(Vector3.one*2,2f);
        #endregion

        #region 几个常用的方法
        //Punch系列方法： 实现来回运动
        //第一个参数 punch：需要运动的向量（包含方向 力大小）
        //第二个参数 duration：持续时间
        //第三个参数：震动次数（频率）
        //第四个参数：取值范围0-1   取值最大，往反方向的力越大  
        //transform.DOPunchPosition(new Vector3(0, 1, 0), 2, 2, 0.1f);
        //transform.DOPunchRotation(new Vector3(0, 1, 0), 2, 2, 0.1f);
        //transform.DOPunchScale(new Vector3(0, 1, 0), 2, 2, 0.1f);

        //Shake系列方法：震动(向四面八方震动)   适用于相机的震动效果
        //transform.DOShakePosition(2, Vector3.one, 10, 90);
        //transform.DOShakeRotation(2, Vector3.one, 10, 90);
        //transform.DOShakeScale(2, Vector3.one, 10, 90);

        //Blend系列方法：混合作用 （增量动画） 
        //如果单纯的调用两个DoMove，GameObject会遵循最下面的那个DoMove方法进行移动。
        //调用Blend方法可以对多个坐标进行混合计算，得到GameObject最终需要移动的坐标。
        //transform.DOBlendableMoveBy(Vector3.one,2f);
        //transform.DOBlendableMoveBy(-Vector3.one*2, 2f);
        //transform.DOBlendableRotateBy();
        //transform.DOBlendableScaleBy();
        //transform.DOBlendablePunchRotation();
        #endregion

        #region Material
        Material material = GetComponent<MeshRenderer>().material;
        //1.改变材质颜色 （前提Shader上有main color属性_Color，也就是说明修改material上的color实际上是修改的shader上面的main color）
        //如果shader上面没有main color，可通过查找shader上面的相关color属性（假设是_TintColor），通过添加其名称的方式来修改其color值
        //material.DOColor(Color.red,2);
        //material.DOColor(Color.green,"_TintColor",2);
        //2.改变材质透明度（Alpha值）
        //material.DOColor(Color.clear, 2);//设置RGBA为(0,0,0,0)
        material.DOFade(0, 2);//直接修改Aplha值   注：这里需要透明材质的Image
                              //3.材质颜色渐变  
                              //material.DOGradientColor(gradient,2f);
                              //4.材质Offset改变 （注意：材质的改变是瞬间的，不能有渐变的效果，不然会产生很奇怪的感觉）
                              //material.DOOffset(Vector3.one,2);
                              //5.改变Shader属性值（改变四维向量的属性）
                              //material.DOVector(Color.clear, "_Color", 2);
                              //6.颜色混合
                              //material.DOBlendableColor(Color.green, 2);
                              //material.DOBlendableColor(Color.red, 2);
        #endregion

        #region Camera
        //Camera camera = GetComponent<Camera>();
        //1.改变相机的宽高比  
        //第一个参数：宽/高
        //camera.DOAspect(1f,2);
        //2.改变颜色
        //camera.DOColor(Color.green,2);
        //3.修改相机的近切面和远切面
        //camera.DONearClipPlane();
        //camera.DOFarClipPlane();
        //4.相机的“视域” 简称FOV  实现枪械倍镜（放大缩小）的效果
        //camera.DOFieldOfView(1,2);//修改透视Camera的“视域”
        //camera.DOOrthoSize(1,2); //修改正交Camera的“视域”
        //5.修改Camera显示的屏幕大小（屏幕占比），实现分屏效果
        //camera.DOPixelRect(new Rect(0,0,540,1080),2);//通过直接修改像素
        //camera.DORect(new Rect(0, 0, 0.5f, 0.5f), 2);//通过百分比来改变Camera的屏幕占比,实际上是修改Viewport Rect值
        //6.Camer震动效果
        //camera.DOShakePosition(2, Vector3.one, 10, 90);
        #endregion

        #region Text
        //Text text = GetComponent<Text>();
        //text.DOColor();//改变颜色
        //text.DOFade();//改变Alpha
        //text.DOBlendableColor();//颜色混合
        //text.DOText("hello world",5f).SetEase(Ease.Linear);//匀速实现字一个个出现的效果
        #endregion

        #region 队列
        //两种常用的初始化方法，建议使用第一种
        Sequence sequence = DOTween.Sequence();
        //DOTween.Sequence().Append();
        //1.给队列添加方法
        //添加到sequence的方法会依次执行，并且可以使用AppendInterval方法来延迟执行其后的方法
        //sequence.Append(transform.DOMove(Vector3.one,2));   
        //sequence.AppendInterval(1); //延时1秒
        //sequence.Append(transform.DOMove(new Vector3(2,2,2), 2));
        //2.给队列插入方法  注意：参数atPosition实际上指的是时间
        //在某个时间点插入该方法，要注意这个时间点在当前队里中是否有方法，否则会覆盖其他的方法
        //sequence.Insert(0,transform.DOMove(Vector3.one, 2));
        //3.在队列加入方法，可以通过Insert或Join加入相关方法
        //sequence.Append(transform.DOMove(Vector3.one, 2));
        //sequence.Join(transform.DOScale(Vector3.one*2,2));  //这个方法会和上面的方法同时执行
        //sequence.AppendInterval(1); //延时1秒
        //sequence.Append(transform.DOMove(new Vector3(2, 2, 2), 2));
        //4.在队列之前添加方法   预添加的方法里面后添加的先执行
        // 比如下面fun2先执行  fun1后执行
        //sequence.Prepend(transform.DOMove(Vector3.one, 2));//fun1
        //sequence.Prepend(transform.DOMove(Vector3.one*2, 2));//fun2
        //5.回调函数  在队列指定的时间进行函数的调用   预添加，队列追加，队列插入 三种模式都有回调方法
        //sequence.InsertCallback(5, InsertCallFunction);
        //sequence.AppendCallback(InsertCallFunction);
        //sequence.PrependCallback(InsertCallFunction);
        #endregion

        #region 设置参数&循环设置
        //Dotween采用链式编程，每次返回的对象都是Tweener
        //1.SetLoops()   参数为0，表示无限循环   参数为1，2，3... 表示循环相应的次数
        //LoopType.Yoyo 来回运动 
        //LoopType.Restart 跑到目标位置又瞬移回来 重新开始跑
        //LoopType.Incremental  朝着一个方向做增量运动
        //transform.DOMove(Vector3.one, 2).SetLoops(3, LoopType.Incremental);
        //2.SetAutoKill()   前面的动画运动完后自动销毁，如果不销毁则会将Tweener缓存起来
        //transform.DOMove(Vector3.one, 2).SetAutoKill(true);
        //3.From()  补间动画
        //不添加参数true   使得object从目标点移动到起始点，反向运动
        //添加参数true  会使得object做增量运动，但也是反向的
        //transform.DOMove(Vector3.one, 2).From(true);
        //4.SetDelay()  延时
        //transform.DOMove(Vector3.one, 2).SetDelay(3f);
        //5.SetSpeedBased()  以速度为基准的动画  
        //当添加这个方法，DoMove后面的参数2就代表速度，而不是时间了
        //transform.DOMove(Vector3.one, 2).SetSpeedBased();
        //6.SetId()  设置ID  调用缓存的动画
        //transform.DOMove(Vector3.one, 2).SetId("ID");
        //DOTween.Play("ID");
        //7.SetRecyclable()   设置是否可以回收
        //transform.DOMove(Vector3.one, 2).SetRecyclable(true);
        //8.SetRelative()  增量运动
        //transform.DOMove(Vector3.one, 2).SetRelative(true);
        //9.设置帧函数类型
        //UpdateType.Manual 依赖于 ManualUpdate()方法
        //transform.DOMove(Vector3.one, 2).SetUpdate(UpdateType.Manual,true);
        //DOTween.ManualUpdate();
        #endregion

        #region 运动曲线
        //1.Ease 枚举类型  各种速度变换的曲线
        //针对某些枚举值，可能有三个参数(运动曲线，振幅<运动次数>，力的大小)，用来设置实现来回运动的参数 （官网推荐用来进行图片颜色的闪烁效果） 
        //力的大小 大于0  作用的力越来越小    等于0   作用力不变     小于0   作用的力越来越大
        //transform.DOMove(Vector3.one,2).SetEase(Ease.Linear);
        //transform.DOMove(Vector3.one, 2).SetEase(Ease.Flash,3,0);
        //2.AnimationCurve设置曲线  感觉这个很方便呀   （横轴是时间，纵轴是距离倍数）
        //transform.DOMove(Vector3.one, 2).SetEase(_curve);
        //3.自定义函数设置运动曲线
        //transform.DOMove(Vector3.one, 2).SetEase(EaseFun);
        #endregion

        #region 回调函数
        //1.OnComplete()   运动完成后执行
        transform.DOMove(Vector3.one, 2).OnComplete(()=> { Debug.Log("hello"); });
        //2.常见的方法
        //transform.DOMove(Vector3.one, 2).OnKill();
        //transform.DOMove(Vector3.one, 2).OnPlay();
        //transform.DOMove(Vector3.one, 2).OnPause();
        //transform.DOMove(Vector3.one, 2).OnStart();
        //transform.DOMove(Vector3.one, 2).OnStepComplete();//循环时调用
        //transform.DOMove(Vector3.one, 2).OnUpdate();
        //3.一般在动画重新播放的时候才会去使用OnRewind()
        //transform.DOMove(Vector3.one, 2).OnRewind();
        #endregion
    }

    /// <summary>
    /// 自定义运动曲线函数
    /// </summary>
    /// <param name="time"></param>
    /// <param name="duration"></param>
    /// <param name="overshootOrAmplitude"></param>
    /// <param name="period"></param>
    /// <returns>返回值必须是0-1之间才能正确到达目的地</returns>
    private float EaseFun(float time, float duration, float overshootOrAmplitude, float period)
    {
        return time / duration *1;
    }

    /// <summary>
    /// 
    /// </summary>
    public void InsertCallFunction()
    {
        Debug.Log("InsertCallFunction");
    }

}
