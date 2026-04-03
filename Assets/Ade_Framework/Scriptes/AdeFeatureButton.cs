using System;
using Ade_Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[AddComponentMenu("Ade/Ade Feature Button")]
[DisallowMultipleComponent]
public class AdeFeatureButton : MonoBehaviour
{
    public enum FeatureType
    {
        None,
        Share,
        Subscribe,
        RewardAd,
        InterstitialAd,
        ShowBanner,
        HideBanner,
        RankList,
        OpenSidebarPanel,
        OpenFeedPanel,
    }

    public enum UnsupportedBehavior
    {
        KeepVisible,
        DisableButton,
        HideInPlayMode,
    }

    [SerializeField] FeatureType feature = FeatureType.None;
    [SerializeField] Button targetButton;
    [SerializeField] bool autoBindButtonClick = true;
    [SerializeField] bool keepAvailableInEditor = true;
    [SerializeField] UnsupportedBehavior unsupportedBehavior = UnsupportedBehavior.DisableButton;
    [SerializeField] UnityEvent onInvoked = new UnityEvent();
    [SerializeField] UnityEvent onSuccess = new UnityEvent();
    [SerializeField] UnityEvent onFailure = new UnityEvent();

    bool isBound;

    void Reset()
    {
        targetButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        EnsureButtonReference();
        BindButton();
        RefreshState();
    }

    void OnDisable()
    {
        UnbindButton();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }

        if (!Application.isPlaying)
        {
            ApplyState(IsAvailableInCurrentContext());
        }
    }
#endif

    public void RefreshState()
    {
        ApplyState(IsAvailableInCurrentContext());
    }

    public void Trigger()
    {
        if (!IsAvailableInCurrentContext())
        {
            LogManager.LogWarning($"AdeFeatureButton: {feature} 当前平台不可用");
            onFailure.Invoke();
            return;
        }

        try
        {
            ExecuteFeature();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            onFailure.Invoke();
        }
    }

    void EnsureButtonReference()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
    }

    void BindButton()
    {
        if (!autoBindButtonClick || targetButton == null || isBound)
        {
            return;
        }

        targetButton.onClick.AddListener(Trigger);
        isBound = true;
    }

    void UnbindButton()
    {
        if (targetButton == null || !isBound)
        {
            return;
        }

        targetButton.onClick.RemoveListener(Trigger);
        isBound = false;
    }

    bool IsAvailableInCurrentContext()
    {
        if (!Application.isPlaying && keepAvailableInEditor)
        {
            return true;
        }

        return SupportsFeature(feature);
    }

    void ApplyState(bool isAvailable)
    {
        if (unsupportedBehavior == UnsupportedBehavior.HideInPlayMode && Application.isPlaying && !isAvailable)
        {
            gameObject.SetActive(false);
            return;
        }

        if (targetButton == null)
        {
            return;
        }

        switch (unsupportedBehavior)
        {
            case UnsupportedBehavior.KeepVisible:
                targetButton.interactable = true;
                break;
            case UnsupportedBehavior.DisableButton:
            case UnsupportedBehavior.HideInPlayMode:
                targetButton.interactable = isAvailable;
                break;
        }
    }

    void ExecuteFeature()
    {
        switch (feature)
        {
            case FeatureType.None:
                LogManager.LogWarning("AdeFeatureButton: 尚未选择功能类型");
                onFailure.Invoke();
                return;

            case FeatureType.Share:
                InvokeInvoked();
#if Ade_TT
                AdeSDK.Instance.OnShare(InvokeSuccess);
#elif Ade_WX || Ade_KS
                AdeSDK.Instance.OnShare();
#endif
                return;

            case FeatureType.Subscribe:
                InvokeInvoked();
#if Ade_TT
                AdeSDK.Instance.OnRequestSubscribeMessage(InvokeSuccess);
#elif Ade_WX
                AdeSDK.Instance.OnRequestSubscribeMessage(null);
#endif
                return;

            case FeatureType.RewardAd:
                InvokeInvoked();
                ADManager.Instance.ShowRewardAD(InvokeSuccess, InvokeFailure);
                return;

            case FeatureType.InterstitialAd:
                InvokeInvoked();
                ADManager.Instance.ShowWhiteAd();
                return;

            case FeatureType.ShowBanner:
                InvokeInvoked();
                ADManager.Instance.ShowBanner();
                return;

            case FeatureType.HideBanner:
                InvokeInvoked();
                ADManager.Instance.HideBanner();
                return;

            case FeatureType.RankList:
                InvokeInvoked();
#if Ade_TT
                AdeSDK.Instance.RefreshLeaderboard();
#endif
                return;

            case FeatureType.OpenSidebarPanel:
                InvokeInvoked();
                SidebarPlane.Instance.Show();
                InvokeSuccess();
                return;

            case FeatureType.OpenFeedPanel:
                InvokeInvoked();
                FeedPlane.Instance.Show();
                InvokeSuccess();
                return;
        }
    }

    void InvokeInvoked()
    {
        onInvoked.Invoke();
    }

    void InvokeSuccess()
    {
        onSuccess.Invoke();
    }

    void InvokeFailure()
    {
        onFailure.Invoke();
    }

    static bool SupportsFeature(FeatureType feature)
    {
        switch (feature)
        {
            case FeatureType.None:
                return false;

            case FeatureType.Share:
#if Ade_TT || Ade_WX || Ade_KS
                return true;
#else
                return false;
#endif

            case FeatureType.Subscribe:
#if Ade_TT || Ade_WX
                return true;
#else
                return false;
#endif

            case FeatureType.RewardAd:
            case FeatureType.InterstitialAd:
            case FeatureType.ShowBanner:
            case FeatureType.HideBanner:
            case FeatureType.OpenSidebarPanel:
            case FeatureType.OpenFeedPanel:
                return true;

            case FeatureType.RankList:
#if Ade_TT
                return true;
#else
                return false;
#endif

            default:
                return false;
        }
    }
}
