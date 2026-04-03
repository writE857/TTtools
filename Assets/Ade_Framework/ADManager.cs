using Ade_Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ADManager : Single<ADManager>
{
    const string RewardAdName = "激励";

    private ADManager()
    {
    }

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
#if UNITY_EDITOR
        
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
#if UNITY_EDITOR
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
}
