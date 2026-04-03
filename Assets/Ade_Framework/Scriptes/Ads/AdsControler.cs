using System;
using UnityEngine;
using Ade_Framework;
using System.Collections.Generic;

#if Ade_TT
using TTSDK;
#elif Ade_WX      
using WeChatWASM;
#elif Ade_KS
using KSWASM;
#elif Ade_BiliBili
using WeChatWASM;
#endif


namespace Ade_Framework
{
    public class AdsControler : Single<AdsControler>
    {
        public Action<bool> AdShowBack;
        public Action<bool> AdCloseBack;
        AdsData adsData;
        AdsPlatformData adsPlatformData;
        Dictionary<string, RewardedAd> RewardedKeyValue = new Dictionary<string, RewardedAd>();
        string UnDataLoadAdName = "UnDataLoadAdName";

        InterstitiaAd _InterstitiaAd;

        BannerAd _BannerAd;
        public void Init()
        {
#if ADE_NO_ADS
            return;
#endif
#if !Ade_TT && !Ade_WX
            adsData = Resources.Load<AdsData>("ScriptableObject/AdsData");
            adsPlatformData = adsData.AdData;
#elif Ade_TT
            adsData = Resources.Load<AdsData>("ScriptableObject/AdsData");
            adsPlatformData = adsData.AdData;

            TimerManager.Instance.OnAddUpdataAction(UnDataLoadAdName, UnDataLoadAd);
#elif Ade_WX
            adsData = Resources.Load<AdsData>("ScriptableObject/AdsData");
            adsPlatformData = adsData.AdData;
#if !UNITY_EDITOR

                _BannerAd = new BannerAd();
                _BannerAd.Init(adsPlatformData.BannerID, null,null);

                _InterstitiaAd = new InterstitiaAd();
                _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);

                RewardedAd ad = new RewardedAd();
                ad.Init(adsPlatformData.RewardID[updatacount], RewardShow, RewardClose);
                RewardedKeyValue[adsPlatformData.RewardID[updatacount].name] = ad;
#endif
#elif Ade_KS
            adsData = Resources.Load<AdsData>("ScriptableObject/AdsData");
            adsPlatformData = adsData.AdData;
    #if !UNITY_EDITOR

                    _BannerAd = new BannerAd();
                    _BannerAd.Init(adsPlatformData.BannerID, null,null);

                    _InterstitiaAd = new InterstitiaAd();
                    _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);

                    RewardedAd ad = new RewardedAd();
                    ad.Init(adsPlatformData.RewardID[updatacount], RewardShow, RewardClose);
                    RewardedKeyValue[adsPlatformData.RewardID[updatacount].name] = ad;
    #endif
#endif

#if !UNITY_EDITOR
           
#endif
        }

        int updatacount;
        void UnDataLoadAd(float t)
        {
            if (_InterstitiaAd == null)
            {
                _InterstitiaAd = new InterstitiaAd();
                _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);
                return;
            }


            if (adsPlatformData.RewardID.Length <= 0 || updatacount >= adsPlatformData.RewardID.Length)
            {
                TimerManager.Instance.OnRemoveUpdataAction(UnDataLoadAdName);
                return;
            }

            RewardedAd ad = new RewardedAd();
            ad.Init(adsPlatformData.RewardID[updatacount], RewardShow, RewardClose);
            RewardedKeyValue[adsPlatformData.RewardID[updatacount].name] = ad;
            updatacount++;
        }

        /// <summary>
        /// 展示激励广告 
        /// </summary>
        /// <param name="name">广告名</param>
        /// <param name="showaction">展示是否成功的回调</param>
        /// <param name="action">关闭回调</param>
        public void ShowReward(string name, Action<bool> showaction, Action<bool> action)
        {
#if UNITY_EDITOR && !Ade_TT && !Ade_WX || UNITY_EDITOR
            showaction?.Invoke(true);
            action?.Invoke(true);
            return;
#else


            AdShowBack = showaction;
            AdCloseBack = action;

            RewardedKeyValue[name].OnShow();
#endif
        }
        void RewardShow(bool Isplay)
        {
            AdShowBack?.Invoke(Isplay);
            AdShowBack = null;

            if(!Isplay) AdCloseBack = null;
        }
        void RewardClose(bool Isplay)
        {
            AdCloseBack?.Invoke(Isplay);
            AdCloseBack = null;
        }

        public void ShowInterstitiaAd()
        {
#if UNITY_EDITOR || (!Ade_TT && !Ade_WX)
            LogManager.Log("展示插屏",Color.yellow);
#else
            LogManager.Log("展示插屏",Color.yellow);
            _InterstitiaAd.OnShow();
#endif
        }

        public void ShowBanner()
        {
            LogManager.Log("展示Banner");
#if UNITY_EDITOR || (!Ade_TT && !Ade_WX)

#else
            _BannerAd.OnShow();
#endif

        }
        public void HideBanner()
        {
            LogManager.Log("关闭Banner");

#if UNITY_EDITOR || (!Ade_TT && !Ade_WX)

#else
             _BannerAd.OnHide();
#endif

        }
    }

}
public abstract class Ad
{
    public AdItemData itemData;
    protected Action<bool> ShowBack;
    protected Action<bool> CloseBack;
    public abstract void Init(AdItemData adid, Action<bool> show, Action<bool> close);

    protected abstract void AddEvent();

    public abstract void OnLoad();

    public abstract void LaodErrorBack();

    public abstract void LaodSuccessBack();

    public abstract void OnShow();

    public abstract void ShowErrorBack();

    public abstract void ShowSuccessBack();

    public abstract void OnClose(bool Isplay = true);


}

public class AdItemBase : Ad
{
    protected override void AddEvent()
    {
        throw new System.NotImplementedException();
    }

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        itemData = adid;
        ShowBack = show;
        CloseBack = close;
    }

    public override void LaodErrorBack()
    {
        LogManager.Log("加载失败");
    }

    public override void LaodSuccessBack()
    {
        LogManager.Log("加载成功");
    }

    public override void OnClose(bool Isplay)
    {
        CloseBack?.Invoke(Isplay);
    }

    public override void OnLoad()
    {
        throw new System.NotImplementedException();
    }

    public override void OnShow()
    {
        throw new System.NotImplementedException();
    }

    public override void ShowErrorBack()
    {
        ShowBack?.Invoke(false);
    }

    public override void ShowSuccessBack()
    {
        ShowBack?.Invoke(true);
    }
}

#if Ade_TT
public class RewardedAd : AdItemBase
{

    CreateRewardedVideoAdParam createRewardedVideoAd;
    int TrueShowCount;
    /// <summary>
    /// 激励视频广告组件
    /// </summary>
    TTRewardedVideoAd tTRewardedVideoAd;

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        createRewardedVideoAd = new CreateRewardedVideoAdParam();
        createRewardedVideoAd.AdUnitId = adid.ID;
        tTRewardedVideoAd = TT.CreateRewardedVideoAd(createRewardedVideoAd);
        AddEvent();
        OnLoad();
    }

    protected override void AddEvent()
    {
        tTRewardedVideoAd.OnLoad += LaodSuccessBack;
        tTRewardedVideoAd.OnError += LaodErrorBack;
        tTRewardedVideoAd.OnClose += OnClose;
    }

    /// <summary>
    /// 加载
    /// </summary>
    public override void OnLoad()
    {
        tTRewardedVideoAd.Load();
    }
    public void LaodErrorBack(int code, string mes)
    {
        LaodErrorBack();
    }
    public override void LaodErrorBack()
    {
        OnLoad();
    }

    public override void OnShow()
    {

        tTRewardedVideoAd.Show();
        //wXRewardedVideo.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
    }

    public void OnClose(bool Isplay, int count)
    {
        bool bbb = Isplay && count > 0;

        OnClose(bbb);

        if (bbb)
        {
            OnLoad();
        }
    }
    public override void OnClose(bool Isplay)
    {

        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }

    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class InterstitiaAd : AdItemBase
{
    CreateInterstitialAdParam createInterstitialVideoAd;
    int TrueShowCount;
    /// <summary>
    /// Ƶ
    /// </summary>
    TTInterstitialAd tTInterstitialVideoAd;

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        createInterstitialVideoAd = new CreateInterstitialAdParam();
        createInterstitialVideoAd.InterstitialAdId = adid.ID;
        tTInterstitialVideoAd = TT.CreateInterstitialAd(createInterstitialVideoAd);
        AddEvent();
        //OnLoad();
    }

    protected override void AddEvent()
    {
        tTInterstitialVideoAd.OnLoad += LaodSuccessBack;
        tTInterstitialVideoAd.OnError += LaodErrorBack;
        tTInterstitialVideoAd.OnClose += OnClose;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void OnLoad()
    {
        tTInterstitialVideoAd.Load();
    }
    public void LaodErrorBack(int code, string mes)
    {
        LaodErrorBack();
    }
    public override void LaodErrorBack()
    {
        OnLoad();
    }

    public override void OnShow()
    {
        OnLoad();
        tTInterstitialVideoAd.Show();
    }

    public void OnClose()
    {
        OnClose(true);

        tTInterstitialVideoAd.Destroy();
        TimerManager.Instance.StartCountdown("InterstitialVideoAd", 30, (tt) =>
        {
            
        }, () =>
        {
            tTInterstitialVideoAd = TT.CreateInterstitialAd(createInterstitialVideoAd);
            AddEvent();
            //OnLoad();
        });
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }

    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class BannerAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);


    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {

    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {

    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }

    public void OnHide()
    {

    }
}
#endif

#if Ade_WX
    public class RewardedAd : AdItemBase
    {

        WXCreateRewardedVideoAdParam RewardedVideoAdParam;

        /// <summary>
        /// 激励视频广告组件
        /// </summary>
        WXRewardedVideoAd wXRewardedVideo;
        public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
        {
            base.Init(adid, show, close);
            RewardedVideoAdParam = new WXCreateRewardedVideoAdParam();
            RewardedVideoAdParam.adUnitId = adid.ID;
            wXRewardedVideo = WX.CreateRewardedVideoAd(RewardedVideoAdParam);
            AddEvent();
        }

        protected override void AddEvent()
        {
            wXRewardedVideo.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); });
            wXRewardedVideo.OnError((WXADLoadResponse) => { LaodErrorBack(); });
            wXRewardedVideo.OnClose((wxRewardedVideoAdOnCloseResponse) => { OnClose(wxRewardedVideoAdOnCloseResponse.isEnded); });
        }

        /// <summary>
        /// WX 加载失败时调用  其他时机WX自动加载
        /// </summary>
        public override void OnLoad()
        {
            wXRewardedVideo.Load();
        }

        public override void LaodErrorBack()
        {
            //OnLoad();
        }

        public override void OnShow()
        {
            wXRewardedVideo.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
        }

        public override void OnClose(bool Isplay = true)
        {
            base.OnClose(Isplay);
        }

        public override void ShowSuccessBack()
        {
            base.ShowSuccessBack();
        }
        public override void ShowErrorBack()
        {
            base.ShowErrorBack();
        }
    }


public class InterstitiaAd : AdItemBase
{
    WXCreateInterstitialAdParam RewardedVideoAdParam;

    /// <summary>
    /// 激励视频广告组件
    /// </summary>
    WXInterstitialAd  WXInterstitialAd;
    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        RewardedVideoAdParam = new WXCreateInterstitialAdParam();
        RewardedVideoAdParam.adUnitId = adid.ID;
        WXInterstitialAd = WX.CreateInterstitialAd(RewardedVideoAdParam);
        AddEvent();
    }

    protected override void AddEvent()
    {
        WXInterstitialAd.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); });
        WXInterstitialAd.OnError((WXADLoadResponse) => { LaodErrorBack(); });
    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
        WXInterstitialAd.Load();
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
        WXInterstitialAd.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class BannerAd : AdItemBase
{
    WXCustomAd CustomBannerAd;
    WXCreateCustomAdParam WXCreateCustomParam;
    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        CustomStyle style = new CustomStyle();
        style.left = (int)(AdeSDK.Instance.Wx_windowInfo.windowWidth * 0.1f);
        style.top = (int)(AdeSDK.Instance.Wx_windowInfo.windowHeight - 100);
        style.width = (int)(AdeSDK.Instance.Wx_windowInfo.windowWidth * 0.8f);
        WXCreateCustomParam = new WXCreateCustomAdParam();
        WXCreateCustomParam.adUnitId = adid.ID;
        WXCreateCustomParam.style = style;
        CustomBannerAd = WX.CreateCustomAd(WXCreateCustomParam);
        AddEvent();

    }

    protected override void AddEvent()
    {
        CustomBannerAd.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); LogManager.Log("BBllll:" + WXADLoadResponse.errMsg); });
        CustomBannerAd.OnError((WXADLoadResponse) => { LaodErrorBack();LogManager.Log( "BBBBBBB:"+WXADLoadResponse.errMsg); });

        
    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
       
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
        CustomBannerAd.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }

    public void OnHide()
    {
        CustomBannerAd.Hide();
    }
}
#endif

#if Ade_KS
public class RewardedAd : AdItemBase
    {

        /// <summary>
        /// 激励视频广告组件
        /// </summary>
        RewardVideoAd KsRewardedVideo;
        public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
        {
            base.Init(adid, show, close);
            KsRewardedVideo = KS.CreateRewardedVideoAd(adid.ID);
            AddEvent();
        }

        protected override void AddEvent()
        {
            KsRewardedVideo.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); });
            KsRewardedVideo.OnError((WXADLoadResponse) => { LaodErrorBack(); });
            KsRewardedVideo.OnClose((wxRewardedVideoAdOnCloseResponse) => { OnClose(wxRewardedVideoAdOnCloseResponse.isEnded); });
        }

        /// <summary>
        /// WX 加载失败时调用  其他时机WX自动加载
        /// </summary>
        public override void OnLoad()
        {
            //KsRewardedVideo.Load();
        }

        public override void LaodErrorBack()
        {
            //OnLoad();
        }

        public override void OnShow()
        {
            KsRewardedVideo.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
        }

        public override void OnClose(bool Isplay = true)
        {
            base.OnClose(Isplay);
        }

        public override void ShowSuccessBack()
        {
            base.ShowSuccessBack();
        }
        public override void ShowErrorBack()
        {
            base.ShowErrorBack();
        }
    }


public class InterstitiaAd : AdItemBase
{
    /// <summary>
    /// 激励视频广告组件
    /// </summary>
    InterstitialAd KsInterstitialAd;
    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        KsInterstitialAd = KS.CreateInterstitialAd(adid.ID);
        AddEvent();
    }

    protected override void AddEvent()
    {
        KsInterstitialAd.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); });
        KsInterstitialAd.OnError((WXADLoadResponse) => { LaodErrorBack(); });
    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
        KsInterstitialAd.Load();
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
        KsInterstitialAd.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class BannerAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);

        AddEvent();

    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
       
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
       
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }

    public void OnHide()
    {
      
    }
}
#endif

#if Ade_BiliBili
public class RewardedAd : AdItemBase
{

    WXCreateRewardedVideoAdParam RewardedVideoAdParam;

    /// <summary>
    /// 激励视频广告组件
    /// </summary>
    WXRewardedVideoAd wXRewardedVideo;
    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        RewardedVideoAdParam = new WXCreateRewardedVideoAdParam();
        RewardedVideoAdParam.adUnitId = adid.ID;
        wXRewardedVideo = WX.CreateRewardedVideoAd(RewardedVideoAdParam);
        AddEvent();
    }

    protected override void AddEvent()
        {
            wXRewardedVideo.OnLoad((WXADLoadResponse) => { LaodSuccessBack(); });
            wXRewardedVideo.OnError((WXADLoadResponse) => { LaodErrorBack(); });
            wXRewardedVideo.OnClose((wxRewardedVideoAdOnCloseResponse) => { OnClose(wxRewardedVideoAdOnCloseResponse.isEnded); });
        }

        /// <summary>
        /// WX 加载失败时调用  其他时机WX自动加载
        /// </summary>
        public override void OnLoad()
        {
            //KsRewardedVideo.Load();
        }

        public override void LaodErrorBack()
        {
            //OnLoad();
        }

        public override void OnShow()
        {
            wXRewardedVideo.Show((WXTextResponse) => { ShowSuccessBack(); }, (WXTextResponse) => { ShowErrorBack(); });
        }

        public override void OnClose(bool Isplay = true)
        {
            base.OnClose(Isplay);
        }

        public override void ShowSuccessBack()
        {
            base.ShowSuccessBack();
        }
        public override void ShowErrorBack()
        {
            base.ShowErrorBack();
        }
}


public class InterstitiaAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);

        AddEvent();
    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
    
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
       
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class BannerAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);

        AddEvent();

    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
       
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
       
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }

    public void OnHide()
    {
      
    }
}
#endif

#if !Ade_KS && !Ade_TT && !Ade_WX && !Ade_BiliBili
public class RewardedAd : AdItemBase
{


    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);

        AddEvent();
    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {

    }

    public override void LaodErrorBack()
    {
        OnLoad();
    }

    public override void OnShow()
    {
    
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class InterstitiaAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);

    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// ����
    /// </summary>
    public override void OnLoad()
    {
    
    }
    public void LaodErrorBack(int code, string mes)
    {
       
    }
    public override void LaodErrorBack()
    {
       
    }

    public override void OnShow()
    {
       
    }

    public void OnClose()
    {

    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }

    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }
}

public class BannerAd : AdItemBase
{

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);


    }

    protected override void AddEvent()
    {

    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {

    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
       
    }

    public override void OnClose(bool Isplay = true)
    {
        base.OnClose(Isplay);
    }

    public override void ShowSuccessBack()
    {
        base.ShowSuccessBack();
    }
    public override void ShowErrorBack()
    {
        base.ShowErrorBack();
    }

    public void OnHide()
    {
       
    }
}
#endif


