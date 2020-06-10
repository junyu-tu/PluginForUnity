using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdController : MonoBehaviour {

    private BannerView bannerView;
    private InterstitialAd interstitial;
    private RewardBasedVideoAd rewardBasedVideo;

    public string bannerAndID = "ca-app-pub-3940256099942544/6300978111";
    public string bannerIosId = "ca-app-pub-3940256099942544/2934735716";
    public string InterstitialAndID = "ca-app-pub-3940256099942544/1033173712";
    public string InterstitialIosAD = "ca-app-pub-3940256099942544/4411468910";
    public string RewardVideoAndID = "ca-app-pub-3940256099942544/5224354917";
    public string RewardVideoIosID = "ca-app-pub-3940256099942544/1712485313";


    void Start () {
        string appId = "unexpected_platform";
#if UNITY_ANDROID
         appId = "ca-app-pub-3940256099942544~3347511713";
#elif UNITY_IPHONE
         appId = "ca-app-pub-3940256099942544~1458002511";
#endif
        //初始化 Google Mobile Ads SDK.
        MobileAds.Initialize(appId);

        //RewardBasedVideoAd是单例
        rewardBasedVideo = RewardBasedVideoAd.Instance;

        //RewardVideo回调方法
        rewardBasedVideo.OnAdLoaded += HandleRewardBasedVideoLoaded;
        rewardBasedVideo.OnAdFailedToLoad += HandleRewardBasedVideoFailedToLoad;
        rewardBasedVideo.OnAdOpening += HandleRewardBasedVideoOpened;
        //广告开始播放时会调用此方法
        rewardBasedVideo.OnAdStarted += HandleRewardBasedVideoStarted;
        // 观看完视频获得奖励（Reward参数就是呈现给玩家的奖励）
        rewardBasedVideo.OnAdRewarded += HandleRewardBasedVideoRewarded;
        rewardBasedVideo.OnAdClosed += HandleRewardBasedVideoClosed;
        rewardBasedVideo.OnAdLeavingApplication += HandleRewardBasedVideoLeftApplication;
    }

    /// <summary>
    /// 请求Banner广告
    /// </summary>
    public void RequestBanner()
    {
        string adUnitId = "unexpected_platform";
#if UNITY_ANDROID
        adUnitId = bannerAndID;
#elif UNITY_IPHONE
        adUnitId = bannerIosID;
#endif

        //实例化Banner
        // adUnitId - BannerView加载广告单元的ID
        // AdSize - Banner广告的相关尺寸（如果尺寸太大，可能Banner不会显示）
        // AdPosition - banner广告显示的位置（枚举类型）
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Top);

        // 广告加载完成后调用
        bannerView.OnAdLoaded += HandleOnAdLoadedBanner;
        // 广告加载失败时调用
        bannerView.OnAdFailedToLoad += HandleOnAdFailedToLoadBanner;
        // 当广告被点击时调用（记录 跟踪用户点击率的好地方）
        bannerView.OnAdOpening += HandleOnAdOpenedBanner;
        // 当玩家观看完广告后，点击返回游戏时调用（可以使用此方法恢复暂停的活动，或执行任何其他必要的操作，以做好互动准备。）
        bannerView.OnAdClosed += HandleOnAdClosedBanner;
        // 当玩家点击打开了其他应用程序（如Google Play）时调用（也就是点击广告跳转出去当前游戏了）
        bannerView.OnAdLeavingApplication += HandleOnAdLeavingApplicationBanner;

        // 创建一个请求
        AdRequest request = new AdRequest.Builder().Build();

        //通过请求去加载Banner. 
        bannerView.LoadAd(request);
    }

    //显示Banner
    public void ShowBanner()
    {
        bannerView.Show();
    }

    //隐藏Banner
    public void HideBanner()
    {
        bannerView.Hide();
    }

    #region Banner广告的回调函数
    public void HandleOnAdLoadedBanner(object sender, EventArgs args)
    {
        print("HandleAdLoaded event received");
    }

    public void HandleOnAdFailedToLoadBanner(object sender, AdFailedToLoadEventArgs args)
    {
        print("HandleFailedToReceiveAd event received with message: "+ args.Message);
    }

    public void HandleOnAdOpenedBanner(object sender, EventArgs args)
    {
        print("HandleAdOpened event received");
    }

    public void HandleOnAdClosedBanner(object sender, EventArgs args)
    {
        print("HandleAdClosed event received");
    }

    public void HandleOnAdLeavingApplicationBanner(object sender, EventArgs args)
    {
        print("HandleAdLeavingApplication event received");
    }
    #endregion

    /// <summary>
    /// 请求Interstitial广告
    /// </summary>
    public void RequestInterstitial()
    {
        string adUnitId = "unexpected_platform";
#if UNITY_ANDROID
        adUnitId = InterstitialAndID;
#elif UNITY_IPHONE
        adUnitId = InterstitialIosAD;
#endif

        //初始化InterstitialAd.
        interstitial = new InterstitialAd(adUnitId);

        interstitial.OnAdLoaded += HandleOnAdLoadedInterstitial;
        interstitial.OnAdFailedToLoad += HandleOnAdFailedToLoadInterstitial;
        interstitial.OnAdOpening += HandleOnAdOpenedInterstitial;
        interstitial.OnAdClosed += HandleOnAdClosedInterstitial;
        interstitial.OnAdLeavingApplication += HandleOnAdLeavingApplicationInterstitial;

        // 创建一个请求.
        AdRequest request = new AdRequest.Builder().Build();
        // 通过请求去加载InterstitialAd.
        interstitial.LoadAd(request);
    }
    /*注意：在iOS上，InterstitialAd对象是一次性使用对象。
     * 这意味着，一旦显示插页式广告，该InterstitialAd对象就无法用于加载其他广告。
     * 要请求其他插页式广告，您需要创建一个新 InterstitialAd对象。*/

    /// <summary>
    /// 显示Interstitial广告
    /// </summary>
    public void ShowInterstitial() {
        if (interstitial.IsLoaded())
        {
            interstitial.Show();
        }
    }

    #region Interstitial广告的回调函数
    public void HandleOnAdLoadedInterstitial(object sender, EventArgs args)
    {
        print("HandleAdLoaded event received");
    }

    public void HandleOnAdFailedToLoadInterstitial(object sender, AdFailedToLoadEventArgs args)
    {
        print("HandleFailedToReceiveAd event received with message: "
                            + args.Message);
    }

    public void HandleOnAdOpenedInterstitial(object sender, EventArgs args)
    {
        print("HandleAdOpened event received");
    }

    public void HandleOnAdClosedInterstitial(object sender, EventArgs args)
    {
        print("HandleAdClosed event received");

        //当关闭一个Interstitial时，需要重新实例化一个
        if (interstitial != null)
        {
            interstitial.Destroy();
        }
        RequestInterstitial();
    }

    public void HandleOnAdLeavingApplicationInterstitial(object sender, EventArgs args)
    {
        print("HandleAdLeavingApplication event received");
    }
    #endregion

    /// <summary>
    /// 请求RewardVideo
    /// </summary>
    public void RequestRewardVideo()
    {
        string adUnitId = "unexpected_platform";
#if UNITY_ANDROID
        adUnitId = RewardVideoAndID;
#elif UNITY_IPHONE
        adUnitId = RewardVideoIosID;        
#endif

        // 创建一个请求
        AdRequest request = new AdRequest.Builder().Build();
        // 根据请求去加载相关RewardVideo
        rewardBasedVideo.LoadAd(request, adUnitId);
    }

    /// <summary>
    /// 显示RewardVideo
    /// </summary>
    public void ShowRewardVideo() {
        if (rewardBasedVideo.IsLoaded())
        {
            rewardBasedVideo.Show();
        }
    }

    #region RewardVideo回调函数
    public void HandleRewardBasedVideoLoaded(object sender, EventArgs args)
    {
        print("HandleRewardBasedVideoLoaded event received");
    }

    public void HandleRewardBasedVideoFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        print(
            "HandleRewardBasedVideoFailedToLoad event received with message: "
                             + args.Message);
    }

    public void HandleRewardBasedVideoOpened(object sender, EventArgs args)
    {
        print("HandleRewardBasedVideoOpened event received");
    }

    public void HandleRewardBasedVideoStarted(object sender, EventArgs args)
    {
        print("HandleRewardBasedVideoStarted event received");
    }

    public void HandleRewardBasedVideoClosed(object sender, EventArgs args)
    {
        print("HandleRewardBasedVideoClosed event received");
        //重新加载RewardVideo
        RequestRewardVideo();
    }

    public void HandleRewardBasedVideoRewarded(object sender, Reward args)
    {
        string type = args.Type;
        double amount = args.Amount;
        print(
            "HandleRewardBasedVideoRewarded event received for "
                        + amount.ToString() + " " + type);
    }

    public void HandleRewardBasedVideoLeftApplication(object sender, EventArgs args)
    {
        print("HandleRewardBasedVideoLeftApplication event received");
    }
    #endregion

}
