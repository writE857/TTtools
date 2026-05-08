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
public enum GridAdType
{
    [InspectorName("单格子")]
    Single,
    [InspectorName("竖格子")]
    Vertical,
    [InspectorName("矩阵格子")]
    Matrix,
    [InspectorName("横格子")]
    Horizontal
}

[System.Serializable]
public enum GridAnchorType
{
    [InspectorName("左上")]
    TopLeft,
    [InspectorName("上")]
    Top,
    [InspectorName("右上")]
    TopRight,
    [InspectorName("左")]
    Left,
    [InspectorName("中")]
    Center,
    [InspectorName("右")]
    Right,
    [InspectorName("左下")]
    BottomLeft,
    [InspectorName("下")]
    Bottom,
    [InspectorName("右下")]
    BottomRight
}

[System.Serializable]
public class GridAdData
{
    [InspectorName("名称ID")]
    public string NameId;

    [InspectorName("格子类型")]
    public GridAdType Type;

    [InspectorName("格子广告ID")]
    public string AdUnitId;

    [InspectorName("格子锚点")]
    public GridAnchorType Anchor;

    [InspectorName("格子位置")]
    public Vector2 Position;
}

[System.Serializable]
public class AdsPlatformData
{
    public string ID;
    public AdItemData InterstitialID;
    public AdItemData BannerID;
    public AdItemData[] RewardID;
    [InspectorName("格子广告列表")]
    public List<GridAdData> GridAdList = new List<GridAdData>();
}
