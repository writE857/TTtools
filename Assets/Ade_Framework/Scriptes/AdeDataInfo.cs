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

    [Header("推荐流ID")]
    public List<string> FeedContentIDs = new List<string>();

    [Header("推荐流复访ID")]
    public string FeedRepeatContentID;
}
