using Ade_Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShareBut : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if Ade_TT
        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            AdeSDK.Instance.OnShare();
        });
#elif Ade_WX
        gameObject.GetComponent<Button>().onClick.AddListener(() =>
        {
            AdeSDK.Instance.OnShare();
        });
#else
        gameObject.SetActive(false);
#endif
    }

}
