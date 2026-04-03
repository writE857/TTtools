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
        Close();

        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            FeedPlane.Instance.Show();
        });

#if Ade_TT
        AdeSDK.Instance.CheckFeedSubscribeStatus((ison) =>
        {
            gameObject.SetActive(!ison);
        });
#endif
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
