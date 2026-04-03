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
        Debug.Log(AdsControler.Instance);
#if UNITY_EDITOR
        
#else
        AdsControler.Instance.ShowInterstitiaAd();
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

    }

    public void ShowBanner() { AdsControler.Instance.ShowBanner(); }
    public void HideBanner() { AdsControler.Instance.HideBanner(); }
}
