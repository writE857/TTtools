using System;
using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FeedUI : MonoBehaviour
{
    public Button GoBut;
    public Button LinQuBut;
    public Button ClockBut;

    [Header("Events")]
    [SerializeField] UnityEvent onRewardClaimed = new UnityEvent();

    bool canClaimReward;
    Action rewardClaimCallback;

    void Start()
    {
#if Ade_TT
        BindButtonListeners();
        RefreshButtonState();
#else
        BindButtonListeners();
        gameObject.SetActive(false);
#endif
    }

    void OnDestroy()
    {
        UnbindButtonListeners();
    }

    void BindButtonListeners()
    {
        if (ClockBut != null)
        {
            ClockBut.onClick.RemoveListener(HandleCloseClicked);
            ClockBut.onClick.AddListener(HandleCloseClicked);
        }

        if (GoBut != null)
        {
            GoBut.onClick.RemoveListener(HandleGoClicked);
            GoBut.onClick.AddListener(HandleGoClicked);
        }

        if (LinQuBut != null)
        {
            LinQuBut.onClick.RemoveListener(HandleClaimClicked);
            LinQuBut.onClick.AddListener(HandleClaimClicked);
        }
    }

    void UnbindButtonListeners()
    {
        if (ClockBut != null)
        {
            ClockBut.onClick.RemoveListener(HandleCloseClicked);
        }

        if (GoBut != null)
        {
            GoBut.onClick.RemoveListener(HandleGoClicked);
        }

        if (LinQuBut != null)
        {
            LinQuBut.onClick.RemoveListener(HandleClaimClicked);
        }
    }

    void HandleCloseClicked()
    {
        gameObject.SetActive(false);
    }

    void HandleGoClicked()
    {
#if Ade_TT
        AdeSDK.Instance.RequestFeedSubscribe(HandleRequestSuccess, HandleRequestFailure);
#endif
    }

    void HandleRequestSuccess()
    {
        canClaimReward = true;
        RefreshButtonState();
    }

    void HandleRequestFailure(string _)
    {
    }

    void HandleClaimClicked()
    {
        if (!canClaimReward)
        {
            return;
        }

        rewardClaimCallback?.Invoke();
        rewardClaimCallback = null;
        onRewardClaimed.Invoke();
        gameObject.SetActive(false);
        FeedBut.Instance?.Close();
        canClaimReward = false;
        RefreshButtonState();
    }

    void RefreshButtonState()
    {
        if (GoBut != null)
        {
            GoBut.gameObject.SetActive(!canClaimReward);
        }

        if (LinQuBut != null)
        {
            LinQuBut.gameObject.SetActive(canClaimReward);
        }
    }

    public void SetRewardClaimCallback(Action callback)
    {
        rewardClaimCallback = callback;
    }

    public void Show(Action onRewardClaimed = null)
    {
        if (onRewardClaimed != null)
        {
            rewardClaimCallback = onRewardClaimed;
        }

        RefreshButtonState();
        gameObject.SetActive(true);
    }
}


public class FeedPlane : SingleMono<FeedPlane>
{
    bool IsInit;
    FeedUI feedUI;
    private void Init()
    {
        if (IsInit) return;
        IsInit = true;
        feedUI = GameObject.Instantiate(Resources.Load<GameObject>("FeedUI")).GetComponent<FeedUI>();
        DontDestroyOnLoad(feedUI.gameObject);
    }

    public void Show(Action onRewardClaimed = null)
    {
        if (!IsInit) Init();
        feedUI.Show(onRewardClaimed);
    }
}
