using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubscribeBut : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (AdeSDK.Instance._AdeDataInfo == null ||
            AdeSDK.Instance._AdeDataInfo.SubscribeTmplIds == null ||
            AdeSDK.Instance._AdeDataInfo.SubscribeTmplIds.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

#if Ade_TT
        gameObject.SetActive(AdeSDK.Instance.IsSubscribe);
        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            AdeSDK.Instance.OnRequestSubscribeMessage(() =>
            {
                gameObject.SetActive(false);
            });
        });
#else
        gameObject.SetActive(false);
#endif


    }

}
