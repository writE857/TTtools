#if Ade_Bilibili
using WeChatWASM;
#endif
using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SidebarBut : MonoBehaviour
{
    public static SidebarBut Instance;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(() => 
        {
            SidebarPlane.Instance.Show();
        });
        gameObject.SetActive(false);
        LogManager.Log($"侧边栏：{AdeSDK.Instance.isSidebar}");
        if (AdeSDK.Instance.isSidebar) Show();
    }

    /// <summary>
    /// 显示侧边栏按钮
    /// </summary>
    public void Show()
    {
        LogManager.Log($"侧边栏：打开1");
        if (SidebarData.IsReWard) return;
        LogManager.Log($"侧边栏：打开2");
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 关闭侧边栏按钮
    /// </summary>
    public void Close() 
    {
        gameObject.SetActive(false);
    }

}

public static class SidebarData
{
    public static bool IsReWard 
    {
        get => PlayerPrefs.GetInt("SidebarDataIsReWard", 0) == 1;
        set => PlayerPrefs.SetInt("SidebarDataIsReWard", value ? 1 : 0);
    }
}