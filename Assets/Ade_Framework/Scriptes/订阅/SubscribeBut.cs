using Ade_Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if Ade_WX
using WeChatWASM;
#endif

public class SubscribeBut : MonoBehaviour
{
    Button subscribeButton;
    bool isRequesting;

#if Ade_WX && (UNITY_WEBGL || WEIXINMINIGAME) && !UNITY_EDITOR
    readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    bool isWechatTouchEndRegistered;
    bool isWechatTouchEndRequest;
#endif

    void Start()
    {
        subscribeButton = GetComponent<Button>();
        if (subscribeButton == null)
        {
            LogManager.LogError("订阅按钮缺少 Button 组件");
            gameObject.SetActive(false);
            return;
        }

        if (!HasValidSubscribeTmplId())
        {
            LogManager.LogWarning("订阅按钮隐藏：AdeDataInfo 未配置订阅模板ID");
            gameObject.SetActive(false);
            return;
        }

#if Ade_TT
        gameObject.SetActive(AdeSDK.Instance.IsSubscribe);
        subscribeButton.onClick.AddListener(RequestSubscribe);
#elif Ade_WX
        EnsureWechatSubscribeTouchEndBridge();

        bool isAvailable = AdeSDK.Instance.IsSubscribeAvailable;
        LogManager.Log($"微信订阅按钮初始化：available={isAvailable}");
        gameObject.SetActive(isAvailable);
#if UNITY_EDITOR || Ade_Debug
        subscribeButton.onClick.AddListener(RequestSubscribe);
#else
        if (subscribeButton.onClick.GetPersistentEventCount() == 0)
        {
            LogManager.LogWarning("订阅按钮缺少持久化OnClick，已临时添加运行时监听");
            subscribeButton.onClick.AddListener(RequestSubscribe);
        }
#endif
#else
        gameObject.SetActive(false);
#endif
    }

    public void RequestSubscribe()
    {
#if Ade_WX && (UNITY_WEBGL || WEIXINMINIGAME) && !UNITY_EDITOR && !Ade_Debug
        if (!isWechatTouchEndRequest)
        {
            LogManager.Log("忽略非微信 touchend 触发的订阅请求");
            return;
        }
#endif

        if (isRequesting)
        {
            return;
        }

        isRequesting = true;
        LogManager.Log("订阅按钮点击");
        AdeSDK.Instance.OnRequestSubscribeMessage(() =>
        {
            isRequesting = false;
            gameObject.SetActive(false);
        }, (reason) =>
        {
            isRequesting = false;
            LogManager.LogWarning("订阅未完成：" + reason);
        });
    }

    bool HasValidSubscribeTmplId()
    {
        if (AdeSDK.Instance._AdeDataInfo == null ||
            AdeSDK.Instance._AdeDataInfo.SubscribeTmplIds == null)
        {
            return false;
        }

        foreach (string tmplId in AdeSDK.Instance._AdeDataInfo.SubscribeTmplIds)
        {
            if (!string.IsNullOrEmpty(tmplId) && tmplId.Trim().Length > 0)
            {
                return true;
            }
        }

        return false;
    }

#if Ade_WX && (UNITY_WEBGL || WEIXINMINIGAME) && !UNITY_EDITOR
    void EnsureWechatSubscribeTouchEndBridge()
    {
        if (isWechatTouchEndRegistered)
        {
            return;
        }

        WX.InitSDK((code) =>
        {
            if (this == null || !isActiveAndEnabled || isWechatTouchEndRegistered)
            {
                return;
            }

            WX.OnTouchEnd(OnWechatTouchEnd);
            isWechatTouchEndRegistered = true;
            LogManager.Log("已启用微信 touchend 订阅桥");
        });
    }

    void OnWechatTouchEnd(OnTouchStartListenerResult touchEvent)
    {
        if (subscribeButton == null ||
            !subscribeButton.IsActive() ||
            !subscribeButton.IsInteractable() ||
            !IsTouchOnSubscribeButton(touchEvent))
        {
            return;
        }

        isWechatTouchEndRequest = true;
        try
        {
            RequestSubscribe();
        }
        finally
        {
            isWechatTouchEndRequest = false;
        }
    }

    bool IsTouchOnSubscribeButton(OnTouchStartListenerResult touchEvent)
    {
        if (EventSystem.current == null || touchEvent.changedTouches == null)
        {
            return false;
        }

        foreach (var wxTouch in touchEvent.changedTouches)
        {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(wxTouch.clientX, wxTouch.clientY)
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);

            foreach (RaycastResult raycastResult in raycastResults)
            {
                Button touchedButton = raycastResult.gameObject.GetComponentInParent<Button>();
                if (touchedButton == subscribeButton)
                {
                    raycastResults.Clear();
                    return true;
                }
            }
        }

        raycastResults.Clear();
        return false;
    }

    void OnDisable()
    {
        UnregisterWechatSubscribeTouchEndBridge();
    }

    void OnDestroy()
    {
        UnregisterWechatSubscribeTouchEndBridge();
    }

    void UnregisterWechatSubscribeTouchEndBridge()
    {
        if (!isWechatTouchEndRegistered)
        {
            return;
        }

        WX.OffTouchEnd(OnWechatTouchEnd);
        isWechatTouchEndRegistered = false;
    }
#else
    void EnsureWechatSubscribeTouchEndBridge()
    {
    }
#endif
}
