using Ade_Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SidebarPlane : SingleMono<SidebarPlane>
{
    bool IsInit;
    SidebarUI sidebarUI;
    private void Init()
    {
        if (IsInit) return;
        IsInit = true;
        sidebarUI = GameObject.Instantiate(Resources.Load<GameObject>("SidebarUI")).GetComponent<SidebarUI>();
        DontDestroyOnLoad(sidebarUI.gameObject);
    }

    public void Show(Action onRewardClaimed = null) 
    {
        if (!IsInit) Init();
        sidebarUI.Show(onRewardClaimed);
    }
}
