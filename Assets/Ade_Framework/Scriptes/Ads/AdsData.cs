using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New AdsData", menuName = "MyTools/AdsData")]
public class AdsData: ScriptableObject
{
    public AdsPlatformData AdData;
}

[System.Serializable]
public class AdItemData 
{
    public string name;
    public string ID;
}

[System.Serializable]
public class AdsPlatformData
{
    public string ID;
    public AdItemData InterstitialID;
    public AdItemData BannerID;
    public AdItemData[] RewardID;
}
