using Ade_Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ADManager : Single<ADManager>
{
    const string RewardAdName = "激励";

    /// <summary>
    /// 显示白包广告
    /// </summary>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void ShowWhiteAd()
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，跳过插屏广告");
        return;
#else
        Debug.Log(AdsControler.Instance);
#if UNITY_EDITOR || Ade_Debug
        
#else
        AdsControler.Instance.ShowInterstitiaAd();
#endif
#endif
    }

    public void ShowBox()
    {
        
    }

    public void ShowBlackAd()
    {
        
    }

    public void ShowRewardAD(Action onSuccess, Action onFailure = null)
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，直接视为激励成功");
        onSuccess?.Invoke();
        return;
#else
        if (NoAdTicketManager.Consume(1))
        {
            Debug.Log("ADManager: 使用广告券，直接视为激励成功");
            onSuccess?.Invoke();
            return;
        }

#if UNITY_EDITOR || Ade_Debug
        onSuccess?.Invoke();
#else
        AdsControler.Instance.ShowReward(
            RewardAdName,
            isShow =>
            {
                if (!isShow)
                {
                    onFailure?.Invoke();
                }
            },
            isSuccess =>
            {
                if (isSuccess)
                {
                    onSuccess?.Invoke();
                }
                else
                {
                    onFailure?.Invoke();
                }
            });
#endif
#endif

    }

    public void ShowReward(Action action)
    {
        ShowRewardAD(action);
    }

    public void ShowBanner()
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，跳过显示 Banner");
#else
        AdsControler.Instance.ShowBanner();
#endif
    }

    public void HideBanner()
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，Banner 无需隐藏");
#else
        AdsControler.Instance.HideBanner();
#endif
    }

    public void ShowGridAd(int index)
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，跳过显示格子广告");
#else
        AdsControler.Instance.ShowGridAd(index);
#endif
    }

    public void ShowGridAd(string nameId)
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，跳过显示格子广告");
#else
        AdsControler.Instance.ShowGridAd(nameId);
#endif
    }

    public void HideGridAd(int index)
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，格子广告无需隐藏");
#else
        AdsControler.Instance.HideGridAd(index);
#endif
    }

    public void HideGridAd(string nameId)
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，格子广告无需隐藏");
#else
        AdsControler.Instance.HideGridAd(nameId);
#endif
    }

    public void HideAllGridAds()
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，格子广告无需隐藏");
#else
        AdsControler.Instance.HideAllGridAds();
#endif
    }

    public void ShowMoreGames()
    {
#if ADE_NO_ADS
        Debug.Log("ADManager: 无广模式，跳过更多游戏");
#else
        AdsControler.Instance.ShowMoreGames();
#endif
    }
}
