using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeedBut : MonoBehaviour
{
    public static FeedBut Instance;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
#if !Ade_TT
        Close();
#else
        if (AdeSDK.Instance._AdeDataInfo == null || !AdeSDK.Instance.HasAnyFeedContentId())
        {
            Close();
            return;
        }

        Close();

        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            FeedPlane.Instance.Show();
        });

        AdeSDK.Instance.CheckFeedSubscribeStatus((ison) =>
        {
            gameObject.SetActive(!ison);
        });
#endif
    }

    public void Show()
    {
#if Ade_TT
        gameObject.SetActive(true);
#endif
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
