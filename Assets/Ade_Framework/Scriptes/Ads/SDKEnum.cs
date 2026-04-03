using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ade_Framework;

public static class SDKReportEvent 
{
    /// <summary>
    /// 所有事件模板定义 
    /// </summary>
    public static readonly Dictionary<ReportName, Dictionary<string, object>> Templates = new Dictionary<ReportName, Dictionary<string, object>>
    {
        { ReportName.game_launch, new Dictionary<string, object> {
            { "user_id", "" },
            { "device_type", "" },
            { "os_version", "" },
            { "network_type", "" }
        }},
        { ReportName.game_exit, new Dictionary<string, object> {
            { "user_id", "" },
            { "session_duration", "" }
        }},
        { ReportName.level_start, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" }
        }},
        { ReportName.level_success, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" },
            { "time_spent", "" },
            { "num_remain", "" },
            { "success_count", "" },
            { "ads_count", "" },
            { "reborn_count", "" }
        }},
        { ReportName.level_fail, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" },
            { "time_spent", "" },
            { "target_complete", "" },
            { "fail_reason", "" },
            { "ads_count", "" },
            { "reborn_count", "" }
        }},
        { ReportName.ad_unlock_click, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" },
            { "unlock_typ", "" },
            { "ad_watched", "" }
        }},
        { ReportName.ad_reborn, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" },
            { "ad_watched", "" }
        }},
        { ReportName.ad_item, new Dictionary<string, object> {
            { "user_id", "" },
            { "stage_name", "" },
            { "item_id", "" },
            { "ad_watched", "" }
        }},
        { ReportName.ad_energy_refill, new Dictionary<string, object> {
            { "user_id", "" },
            { "refill_type", "" },
            { "ad_watched", "" }
        }}
    };
}
/// <summary>
/// 事件名
/// </summary>
public enum ReportName
{
    /// <summary>
    /// 游戏启动时上报
    /// </summary>
    game_launch,

    /// <summary>
    /// 游戏退出时上报
    /// </summary>
    game_exit,

    /// <summary>
    /// 关卡开始时上报
    /// </summary>
    level_start,

    /// <summary>
    /// 关卡成功完成时上报
    /// </summary>
    level_success,

    /// <summary>
    /// 关卡失败时上报
    /// </summary>
    level_fail,

    /// <summary>
    /// 点击观看广告解锁时上报
    /// </summary>
    ad_unlock_click,

    /// <summary>
    /// 观看广告复活时上报
    /// </summary>
    ad_reborn,

    /// <summary>
    /// 广告获取道具 上报
    /// </summary>
    ad_item,

    /// <summary>
    /// 广告补充体力 上报
    /// </summary>
    ad_energy_refill,

    /// <summary>
    /// 推荐流直出启动 上报
    /// </summary>
    feed_direct_launch,

    /// <summary>
    /// 推荐流场景加载 上报
    /// </summary>
    feed_scene_load,

    /// <summary>
    /// 推荐流用户留存 上报
    /// </summary>
    feed_retention,

    /// <summary>
    /// 推荐流获客转化 上报
    /// </summary>
    acquisition_conversion
}


