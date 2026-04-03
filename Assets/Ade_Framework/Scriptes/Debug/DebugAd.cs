using Ade_Framework;
using UnityEngine;

public class DebugAd : MonoBehaviour
{
    public void JiLi()
    {
        ADManager.Instance.ShowRewardAD(() => { });
    }

    public void ChaPin()
    {
        ADManager.Instance.ShowWhiteAd();
    }

    public void FenXiang()
    {
        AdeSDK.Instance.OnShare();
    }
}
