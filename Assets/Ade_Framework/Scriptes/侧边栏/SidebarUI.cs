using System;
#if Ade_Bilibili
using WeChatWASM;
#endif
using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SidebarUI : MonoBehaviour
{
    public Button GoBut;
    public Button LinQuBut;
    public Button ClockBut;

    [Header("Auto Bind")]
    [HideInInspector]
    [SerializeField] bool autoBindByName = false;

    [HideInInspector]
    [SerializeField] List<AdePopupImageReplaceItem> replaceImageEntries = new List<AdePopupImageReplaceItem>();

    [Header("Events")]
    [SerializeField] UnityEvent onRewardClaimed = new UnityEvent();

    bool canClaimReward;
    Action rewardClaimCallback;

    void Reset()
    {
        AutoBindControls(force: true);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoBindControls(force: false);
        }
    }
#endif

    void Awake()
    {
        AutoBindControls(force: false);
    }

    void Start()
    {
        BindButtonListeners();
        RefreshButtonState();

#if Ade_TT
        AdeSDK.Instance.SidebarBack = HandleSidebarBack;
#elif Ade_Bilibili
        gameObject.SetActive(false);
#else
        gameObject.SetActive(false);
#endif

    }

    void OnDestroy()
    {
        UnbindButtonListeners();
    }

    [ContextMenu("Auto Bind Buttons")]
    void AutoBindButtonsContext()
    {
        AutoBindControls(force: true);
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
        AdeSDK.Instance.GetSidebar();
#elif Ade_BiliBili
        AdeSDK.Instance.GetSidebar();
#endif
    }

    void HandleSidebarBack()
    {
        canClaimReward = true;
        RefreshButtonState();
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
        SidebarBut.Instance?.Close();
        SidebarData.IsReWard = true;
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

    void AutoBindControls(bool force)
    {
        AssignIfNeeded(ref GoBut, AdePopupButtonSlot.SlotType.Go, "GO", force, autoBindByName);
        AssignIfNeeded(ref LinQuBut, AdePopupButtonSlot.SlotType.Claim, "LinQu", force, autoBindByName);
        AssignIfNeeded(ref ClockBut, AdePopupButtonSlot.SlotType.Close, "Click", force, autoBindByName);
    }

    void AssignIfNeeded(ref Button target, AdePopupButtonSlot.SlotType slotType, string fallbackName, bool force, bool allowNameFallback)
    {
        if (!force && target != null)
        {
            return;
        }

        target = FindButtonBySlot(slotType);
        if (target == null && allowNameFallback)
        {
            target = AdeUIBindingUtility.FindInChildrenByName<Button>(transform, fallbackName);
        }
    }

    Button FindButtonBySlot(AdePopupButtonSlot.SlotType slotType)
    {
        AdePopupButtonSlot[] slots = GetComponentsInChildren<AdePopupButtonSlot>(true);
        foreach (AdePopupButtonSlot slot in slots)
        {
            if (slot != null && slot.Slot == slotType)
            {
                return slot.Button;
            }
        }

        return null;
    }

    /// <summary>
    /// 显示侧边栏
    /// </summary>
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

        AutoBindControls(force: false);
        RefreshButtonState();
        gameObject.SetActive(true);
    }
}
