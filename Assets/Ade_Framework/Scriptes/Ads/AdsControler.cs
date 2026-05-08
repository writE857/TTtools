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
        Dictionary<string, GridAd> GridKeyValue = new Dictionary<string, GridAd>();
        string UnDataLoadAdName = "UnDataLoadAdName";

        InterstitiaAd _InterstitiaAd;

        BannerAd _BannerAd;
        public void Init()
        {
#if ADE_NO_ADS
            return;
#endif
            ResetGridAds();
            adsData = Resources.Load<AdsData>("ScriptableObject/AdsData");
            adsPlatformData = adsData != null ? adsData.AdData : null;

#if Ade_Debug
            LogManager.Log("Ade_Debug: 跳过广告初始化");
            return;
#endif
            if (adsPlatformData == null)
            {
                LogManager.LogError("AdsData.AdData未配置");
                return;
            }

#if Ade_TT
            TimerManager.Instance.OnAddUpdataAction(UnDataLoadAdName, UnDataLoadAd);
#elif Ade_WX
#if !UNITY_EDITOR

            if (adsPlatformData.BannerID != null)
            {
                _BannerAd = new BannerAd();
                _BannerAd.Init(adsPlatformData.BannerID, null,null);
            }

            if (adsPlatformData.InterstitialID != null)
            {
                _InterstitiaAd = new InterstitiaAd();
                _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);
            }

            if (adsPlatformData.RewardID != null && adsPlatformData.RewardID.Length > 0)
            {
                RewardedAd ad = new RewardedAd();
                ad.Init(adsPlatformData.RewardID[updatacount], RewardShow, RewardClose);
                RewardedKeyValue[adsPlatformData.RewardID[updatacount].name] = ad;
            }
#endif
#elif Ade_KS
#if !UNITY_EDITOR

            if (adsPlatformData.BannerID != null)
            {
                _BannerAd = new BannerAd();
                _BannerAd.Init(adsPlatformData.BannerID, null,null);
            }

            if (adsPlatformData.InterstitialID != null)
            {
                _InterstitiaAd = new InterstitiaAd();
                _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);
            }

            if (adsPlatformData.RewardID != null && adsPlatformData.RewardID.Length > 0)
            {
                RewardedAd ad = new RewardedAd();
                ad.Init(adsPlatformData.RewardID[updatacount], RewardShow, RewardClose);
                RewardedKeyValue[adsPlatformData.RewardID[updatacount].name] = ad;
            }
#endif
#endif
        }

        int updatacount;
        void UnDataLoadAd(float t)
        {
            if (adsPlatformData == null)
            {
                return;
            }

            if (_InterstitiaAd == null)
            {
                if (adsPlatformData.InterstitialID == null)
                {
                    return;
                }

                _InterstitiaAd = new InterstitiaAd();
                _InterstitiaAd.Init(adsPlatformData.InterstitialID, null, null);
                return;
            }


            if (adsPlatformData.RewardID == null || adsPlatformData.RewardID.Length <= 0 || updatacount >= adsPlatformData.RewardID.Length)
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
#if UNITY_EDITOR || Ade_Debug
            showaction?.Invoke(true);
            action?.Invoke(true);
            return;
#else


            AdShowBack = showaction;
            AdCloseBack = action;

            if (!RewardedKeyValue.TryGetValue(name, out RewardedAd ad) || ad == null)
            {
                LogManager.LogError($"未找到激励广告:{name}");
                RewardShow(false);
                return;
            }

            ad.OnShow();
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
#if UNITY_EDITOR || Ade_Debug || (!Ade_TT && !Ade_WX && !Ade_KS)
            LogManager.Log("展示插屏",Color.yellow);
#else
            LogManager.Log("展示插屏",Color.yellow);
            if (_InterstitiaAd == null)
            {
                LogManager.LogError("插屏广告未初始化");
                return;
            }

            _InterstitiaAd.OnShow();
#endif
        }

        public void ShowBanner()
        {
            LogManager.Log("展示Banner");
#if UNITY_EDITOR || Ade_Debug || (!Ade_TT && !Ade_WX && !Ade_KS)

#else
            if (_BannerAd == null)
            {
                LogManager.LogError("Banner广告未初始化");
                return;
            }

            _BannerAd.OnShow();
#endif

        }
        public void HideBanner()
        {
            LogManager.Log("关闭Banner");

#if UNITY_EDITOR || Ade_Debug || (!Ade_TT && !Ade_WX && !Ade_KS)

#else
            if (_BannerAd == null)
            {
                return;
            }

            _BannerAd.OnHide();
#endif

        }

        public void ShowGridAd(int index)
        {
            if (!TryGetGridAdData(index, out GridAdData gridData))
            {
                return;
            }

            ShowGridAd(gridData.NameId);
        }

        public void ShowGridAd(string nameId)
        {
#if Ade_WX && !UNITY_EDITOR && !Ade_Debug
            if (!TryGetGridAdData(nameId, out GridAdData gridData))
            {
                return;
            }

            if (!GridKeyValue.TryGetValue(gridData.NameId, out GridAd gridAd) || gridAd == null)
            {
                gridAd = new GridAd();
                gridAd.Init(gridData);
                GridKeyValue[gridData.NameId] = gridAd;
            }

            gridAd.OnShow();
#else
            LogManager.Log($"展示格子广告:{nameId}", Color.yellow);
#endif
        }

        public void HideGridAd(int index)
        {
            if (!TryGetGridAdData(index, out GridAdData gridData))
            {
                return;
            }

            HideGridAd(gridData.NameId);
        }

        public void HideGridAd(string nameId)
        {
#if Ade_WX && !UNITY_EDITOR && !Ade_Debug
            if (GridKeyValue.TryGetValue(nameId, out GridAd gridAd) && gridAd != null)
            {
                gridAd.OnHide();
            }
#else
            LogManager.Log($"关闭格子广告:{nameId}", Color.yellow);
#endif
        }

        public void HideAllGridAds()
        {
            foreach (GridAd gridAd in GridKeyValue.Values)
            {
                gridAd?.OnHide();
            }
        }

        void ResetGridAds()
        {
            foreach (GridAd gridAd in GridKeyValue.Values)
            {
                gridAd?.Destroy();
            }

            GridKeyValue.Clear();
        }

        bool TryGetGridAdData(string nameId, out GridAdData gridData)
        {
            gridData = null;

            if (adsPlatformData == null || adsPlatformData.GridAdList == null)
            {
                LogManager.LogError("格子广告配置未初始化");
                return false;
            }

            if (string.IsNullOrWhiteSpace(nameId))
            {
                LogManager.LogError("格子广告NameId为空");
                return false;
            }

            for (int i = 0; i < adsPlatformData.GridAdList.Count; i++)
            {
                GridAdData item = adsPlatformData.GridAdList[i];
                if (item == null)
                {
                    continue;
                }

                if (item.NameId == nameId)
                {
                    gridData = item;
                    break;
                }
            }

            if (gridData == null)
            {
                LogManager.LogError($"未找到格子广告NameId:{nameId}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(gridData.AdUnitId))
            {
                LogManager.LogError($"格子广告ID为空:{nameId}");
                return false;
            }

            return true;
        }

        bool TryGetGridAdData(int index, out GridAdData gridData)
        {
            gridData = null;

            if (adsPlatformData == null || adsPlatformData.GridAdList == null)
            {
                LogManager.LogError("格子广告配置未初始化");
                return false;
            }

            if (index < 0 || index >= adsPlatformData.GridAdList.Count)
            {
                LogManager.LogError($"格子广告索引越界:{index}");
                return false;
            }

            gridData = adsPlatformData.GridAdList[index];
            if (gridData == null)
            {
                LogManager.LogError($"格子广告配置为空:{index}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(gridData.NameId))
            {
                LogManager.LogError($"格子广告NameId为空:{index}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(gridData.AdUnitId))
            {
                LogManager.LogError($"格子广告ID为空:{index}");
                return false;
            }

            return true;
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

public static class GridAdLayoutUtility
{
    static readonly Vector2 SingleSize = new Vector2(68f, 106f);
    static readonly Vector2 VerticalSize = new Vector2(72f, 410f);
    static readonly Vector2 MatrixSize = new Vector2(360f, 188f);
    static readonly Vector2 HorizontalSize = new Vector2(360f, 106f);

    public static Vector2 GetTemplateSize(GridAdType type)
    {
        switch (type)
        {
            case GridAdType.Single:
                return SingleSize;
            case GridAdType.Vertical:
                return VerticalSize;
            case GridAdType.Matrix:
                return MatrixSize;
            case GridAdType.Horizontal:
            default:
                return HorizontalSize;
        }
    }

    public static Vector2 GetTopLeftPosition(Vector2 windowSize, Vector2 adSize, GridAnchorType anchor, Vector2 uiOffset)
    {
        Vector2 screenAnchorPoint = GetAnchorPoint(windowSize, anchor);
        Vector2 adAnchorOffset = GetAnchorPoint(adSize, anchor);
        Vector2 wxOffset = new Vector2(uiOffset.x, -uiOffset.y);
        return screenAnchorPoint - adAnchorOffset + wxOffset;
    }

    static Vector2 GetAnchorPoint(Vector2 size, GridAnchorType anchor)
    {
        switch (anchor)
        {
            case GridAnchorType.TopLeft:
                return new Vector2(0f, 0f);
            case GridAnchorType.Top:
                return new Vector2(size.x * 0.5f, 0f);
            case GridAnchorType.TopRight:
                return new Vector2(size.x, 0f);
            case GridAnchorType.Left:
                return new Vector2(0f, size.y * 0.5f);
            case GridAnchorType.Center:
                return new Vector2(size.x * 0.5f, size.y * 0.5f);
            case GridAnchorType.Right:
                return new Vector2(size.x, size.y * 0.5f);
            case GridAnchorType.BottomLeft:
                return new Vector2(0f, size.y);
            case GridAnchorType.Bottom:
                return new Vector2(size.x * 0.5f, size.y);
            case GridAnchorType.BottomRight:
            default:
                return new Vector2(size.x, size.y);
        }
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

public class GridAd
{
    WXCustomAd customGridAd;
    WXCreateCustomAdParam createGridParam;
    GridAdData gridData;

    public void Init(GridAdData adData)
    {
        gridData = adData;
        CreateAd();
    }

    void CreateAd()
    {
        createGridParam = new WXCreateCustomAdParam();
        createGridParam.adUnitId = gridData.AdUnitId;
        createGridParam.style = BuildStyle(gridData);
        customGridAd = WX.CreateCustomAd(createGridParam);
        AddEvent();
    }

    CustomStyle BuildStyle(GridAdData adData)
    {
        Vector2 adSize = GridAdLayoutUtility.GetTemplateSize(adData.Type);
        Vector2 windowSize = new Vector2((float)AdeSDK.Instance.Wx_windowInfo.windowWidth, (float)AdeSDK.Instance.Wx_windowInfo.windowHeight);
        Vector2 topLeftPosition = GridAdLayoutUtility.GetTopLeftPosition(windowSize, adSize, adData.Anchor, adData.Position);

        CustomStyle style = new CustomStyle();
        style.left = Mathf.RoundToInt(topLeftPosition.x);
        style.top = Mathf.RoundToInt(topLeftPosition.y);
        style.width = Mathf.RoundToInt(adSize.x);
        return style;
    }

    void AddEvent()
    {
        customGridAd.OnLoad((WXADLoadResponse res) =>
        {
            LogManager.Log($"格子广告加载成功:{gridData.AdUnitId}");
        });
        customGridAd.OnError((WXADErrorResponse res) =>
        {
            LogManager.LogError($"格子广告加载失败:{gridData.AdUnitId}_{res.errMsg}");
        });
    }

    public void OnShow()
    {
        if (customGridAd == null)
        {
            CreateAd();
        }

        customGridAd.Show((WXTextResponse res) =>
        {
            LogManager.Log($"格子广告展示成功:{gridData.AdUnitId}");
        }, (WXTextResponse res) =>
        {
            LogManager.LogError($"格子广告展示失败:{gridData.AdUnitId}_{res.errMsg}");
        });
    }

    public void OnHide()
    {
        customGridAd?.Hide();
    }

    public void Destroy()
    {
        if (customGridAd == null)
        {
            return;
        }

        var destroyMethod = customGridAd.GetType().GetMethod("Destroy");
        destroyMethod?.Invoke(customGridAd, null);
        customGridAd = null;
    }
}
#endif

#if !Ade_WX
public class GridAd
{
    public void Init(GridAdData adData)
    {
        itemData = adData;
    }

    GridAdData itemData;

    public void OnShow()
    {
        LogManager.Log($"展示格子广告:{itemData?.NameId}");
    }

    public void OnHide()
    {
        LogManager.Log($"关闭格子广告:{itemData?.NameId}");
    }

    public void Destroy()
    {
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
            KsRewardedVideo.OnLoad((res) => { LaodSuccessBack(); });
            KsRewardedVideo.OnError((res) => { LaodErrorBack(); });
            KsRewardedVideo.OnClose((res) => { OnClose(res.isEnded); });
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
            KsRewardedVideo.Show((res) => { ShowSuccessBack(); }, (res) => { ShowErrorBack(); });
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
        KsInterstitialAd.OnClose(() => { OnClose(true); });
    }

    /// <summary>
    /// WX 加载失败时调用  其他时机WX自动加载
    /// </summary>
    public override void OnLoad()
    {
        KsInterstitialAd.Load((res) => { LaodSuccessBack(); }, (res) => { LaodErrorBack(); });
    }

    public override void LaodErrorBack()
    {
        //OnLoad();
    }

    public override void OnShow()
    {
        KsInterstitialAd.Show((res) => { ShowSuccessBack(); }, (res) => { ShowErrorBack(); });
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
    KSWASM.BannerAd ksBannerAd;

    public override void Init(AdItemData adid, Action<bool> show, Action<bool> close)
    {
        base.Init(adid, show, close);
        ksBannerAd = KS.CreateFixedBottomMiddleBannerAd(adid.ID, 30, 96);

        AddEvent();

    }

    protected override void AddEvent()
    {
        if (ksBannerAd == null)
        {
            return;
        }

        ksBannerAd.OnLoad((res) => { LaodSuccessBack(); });
        ksBannerAd.OnError((res) => { LaodErrorBack(); });
        ksBannerAd.OnClose(() => { OnClose(true); });
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
        if (ksBannerAd == null)
        {
            ShowErrorBack();
            return;
        }

        ksBannerAd.Show((res) => { ShowSuccessBack(); }, (res) => { ShowErrorBack(); });
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
        ksBannerAd?.Hide();
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


