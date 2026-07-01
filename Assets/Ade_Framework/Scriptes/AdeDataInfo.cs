using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New AdeDataInfo", menuName = "MyTools/AdeDataInfo")]
public class AdeDataInfo : ScriptableObject
{
    [Header("分享ID")]
    public string ShareId;

    [Header("订阅ID")]
    [Tooltip("最多3个")]
    public List<string> SubscribeTmplIds = new List<string>(3);

    [Header("推荐流复访ID")]
    public List<string> FeedRepeatContentIDs = new List<string>();

    [Header("推荐流获客ID")]
    public List<string> FeedAcquisitionContentIDs = new List<string>();

    [HideInInspector]
    public List<string> FeedContentIDs = new List<string>();

    [HideInInspector]
    public string FeedRepeatContentID;
}
