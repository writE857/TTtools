using Ade_Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Entry : MonoBehaviour
{
    private const int MainSceneBuildIndex = 1;

    private void Awake()
    {

    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }

    public void Init()
    {
        AdeSDK.Instance.Init(ShowScene);
    }

    public void ShowScene() 
    {
#if Ade_TT
        // 当前模板只有一个主场景，推荐流分支先共用同一入口。
        AdeSDK.Instance.CheckFeedLaunchScene(LoadFeedDirectPlayScene, LoadFeedAcquisitionScene, LoadDefaultScene);
#else
        LoadDefaultScene();
#endif
    }

    private void LoadDefaultScene()
    {
        SceneManager.LoadScene(MainSceneBuildIndex);
    }

    private void LoadFeedDirectPlayScene()
    {
        LoadDefaultScene();
    }

    private void LoadFeedAcquisitionScene()
    {
        LoadDefaultScene();
    }
}
