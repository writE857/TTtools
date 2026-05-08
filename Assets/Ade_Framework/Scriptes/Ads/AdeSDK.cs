using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;

#if UNITY_EDITOR
using UnityEditor;
#endif




#if Ade_TT
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#elif Ade_WX
using WeChatWASM;
#elif Ade_KS
using KSWASM;
#elif Ade_BiliBili
using WeChatWASM;
#endif


namespace Ade_Framework
{
    public class AdeSDK : SingleMono<AdeSDK>
    {

#if UNITY_EDITOR
#elif Ade_TT
#elif Ade_WX
#elif Ade_KS
#elif Ade_BiliBili
#endif
        Action GameShowAction;
        public void Init(Action action)
        {
            GameShowAction = action;
#if UNITY_EDITOR || Ade_Debug
            LogManager.Log("UNITY_EDITOR/Ade_Debug");
            InitBack();
#elif Ade_TT
            TT.InitSDK((i, data) =>
            {
                containerEnv = data;

                OnLogin((bb) =>
                {
                    InitBack();
                });

            });
            LogManager.Log("Ade_TT");
#elif Ade_WX
            WX.InitSDK((isis) => { InitBack(); });

            LogManager.Log("Ade_WX");
#elif Ade_KS
            KS.InitSDK((index) =>
            {
                InitBack();
                LogManager.Log("Ade_KS");
            });
#elif Ade_BiliBili
            WX.InitSDK((isis) => { InitBack(); });

            LogManager.Log("Ade_WX");
#endif

        }

        AdeDataInfo adeData;
        public AdeDataInfo _AdeDataInfo
        {
            get
            {
                if (adeData == null)
                {
                    adeData = Resources.Load<AdeDataInfo>("ScriptableObject/AdeDataInfo");
                }
                return adeData;
            }
        }
        public void InitBack()
        {
            PlatformSetting();
            TimerManager.Instance.Init();
            AdsControler.Instance.Init();

            GameShow();
        }


        /// <summary>
        /// 平台设置
        /// </summary>
        public void PlatformSetting()
        {
#if UNITY_EDITOR || Ade_Debug
#if Ade_TT
            isSidebar = true;
#endif
            return;
#elif Ade_TT
            m_TTGameRecorder = TT.GetGameRecorder();
            m_TTGameRecorder.IsShowShareVideoToast = true;
            m_TTGameRecorder.SetEnabled(true);

            //监听生命周期 知道是从侧边栏回来的
            TT.GetAppLifeCycle().OnShow += (param) =>
            {
                Debug.Log("OnShow:" + param["scene"].ToString());
                if (param["scene"].ToString() == "021036")
                {
                    SidebarBack?.Invoke();
                }
            };

            //判断平台 抖音
            TT.CheckScene(TTSideBar.SceneEnum.SideBar, b =>
            {
                Debug.Log("check scene success，" + b);
                isSidebar = b;
                SideBarCheck();
                
            }, () =>
            {
                Debug.Log("check scene complete");
            }, (errCode, errMsg) =>
            {
                Debug.Log($"check scene error, errCode:{errCode}, errMsg:{errMsg}");
            });
            tTSystemInfo = TT.GetSystemInfo();

            Dictionary<string, object> keyValues = SDKReportEvent.Templates[ReportName.game_launch];
            keyValues["user_id"] = user_id;
            keyValues["device_type"] = tTSystemInfo.model;
            keyValues["os_version"] = tTSystemInfo.system;
            keyValues["network_type"] = "unknown";
            OnReportEvent(ReportName.game_launch, keyValues);

#elif Ade_WX
            WX.ReportGameStart();

            Wx_windowInfo = WX.GetWindowInfo();

#elif Ade_KS
            KS.ReportGameStart();

            KS_windowInfo = KS.GetWindowInfo();

#elif Ade_BiliBili

            WX.ReportGameStart();

            LogManager.Log("Bl_CheckScene");
            var option = new CheckSceneOption()
            {
                scene = "sidebar",
                success = (res) =>
                {
                    LogManager.Log("Bl_CheckScene success");
                    LogManager.Log(res.errMsg);
                    LogManager.Log(res.isExist.ToString());
                    isSidebar = true;
                    SideBarCheck();
                },
                fail = (res) =>
                {
                    LogManager.Log("Bl_CheckScene fail");
                    LogManager.Log(res.errMsg);
                },
                complete = (res) =>
                {
                    LogManager.Log("Bl_CheckScene complete");
                    LogManager.Log(res.errMsg);
                }
            };
            WX.CheckScene(option);
#endif

        }

        public void GameShow()
        {
            OnLogin(null);
#if Ade_TT
            //RefreshLeaderboard();
#endif
            GameShowAction?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        public string user_id
        {
            get 
            {
#if Ade_TT
                return "";
#elif Ade_WX
                return "";
#else
                return "";
#endif
            }
        }


        #region TT端 变量
#if Ade_TT
        /// <summary>
        /// 初始化后 设备信息
        /// </summary>
        public ContainerEnv containerEnv;
        /// <summary>
        /// 系统信息
        /// </summary>
        public TTSystemInfo tTSystemInfo;
        /// <summary>
        /// 录屏组件
        /// </summary>
        private TTGameRecorder m_TTGameRecorder;
 
        /// <summary>
        /// 侧边栏返回回调
        /// </summary>
        public Action SidebarBack;
        public void SideBarCheck()
        {
            if (!isSidebar) return;

            SidebarBut.Instance.Show();
        }   
        public bool isIos 
        {
#if Ade_Debug
            get => false;
#else
            get => TT.GetSystemInfo().platform == "ios";
#endif
        }



        /// <summary>
        /// 是否订阅
        /// </summary>
        public bool IsSubscribe
        {
            get => tTSystemInfo != null && (tTSystemInfo.hostName == "Douyin" || tTSystemInfo.hostName == "douyin_lite");
        }
#endif
        #endregion
        #region WX端 变量
#if Ade_WX
        /// <summary>
        /// 系统信息
        /// </summary>
        public WindowInfo  Wx_windowInfo;
#endif
        #endregion

        #region KS端 变量
#if Ade_KS
        /// <summary>
        /// 系统信息
        /// </summary>
        public WindowInfo  KS_windowInfo;
#endif
        #endregion

        #region BiliBili端 变量
#if Ade_BiliBili
        /// <summary>
        /// 侧边栏返回回调
        /// </summary>
        public Action SidebarBack;
        public void SideBarCheck()
        {
            if (!isSidebar) return;

            SidebarBut.Instance.Show();
        }
#endif
        #endregion

        #region 自定义上报
        /// <summary>
        /// 自定义上报
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="data">事件数据</param>
        /// <summary>
        public static void OnReportEvent(ReportName eventName, Dictionary<string, object> keyValues)
        {

#if Ade_Debug || UNITY_EDITOR && !Ade_TT && !Ade_WX
           
#elif Ade_TT
            TT.ReportAnalytics(eventName.ToString(), keyValues);
#elif Ade_WX
            //Dictionary<string, string> result = keyValues.ToDictionary
            //    (
            //        kvp => kvp.Key,
            //        kvp => kvp.Value?.ToString() ?? string.Empty
            //    );
            //WX.ReportEvent(eventName.ToString(), result);
#endif

            Debug.Log($"上报自定义埋点：{eventName}\n" + keyValues.Values);
        }

        #endregion

        #region 录制 & 分享

        Action<bool> RecordPlay;
        Action<VideoRecordState> RecordEnd;
        /// <summary>
        /// 开始 录屏
        /// </summary>
        /// <param name="play">true=开始成功</param>
        /// <param name="end"></param>
        public void OnVideoRecord(Action<bool> play, Action<VideoRecordState> end)
        {
            return;
            RecordPlay = play;
            RecordEnd = end;
#if UNITY_EDITOR && !Ade_TT && !Ade_WX
            
#elif Ade_TT
            if (m_TTGameRecorder.GetVideoRecordState() != TTGameRecorder.VideoRecordState.RECORD_STARTED)
            {
                m_TTGameRecorder.Start(true, 600, OnRecordStart, (errCode, errMsg) => { OnRecordError(); }, OnRecordTimeout);
            }
            else
            {
                LogManager.Log("Recorder is started");
            }
#elif Ade_WX
            
#endif

            LogManager.Log("开始录制", Color.gray);
        }

        /// <summary>
        /// 停止录屏
        /// </summary>
        public void OnStopButtonTapped()
        {
#if UNITY_EDITOR && !Ade_TT && !Ade_WX
            
#elif Ade_TT
            //m_TTGameRecorder.Stop(OnRecordComplete, (errCode, errMsg) => { OnRecordErrorCallback(); });
#elif Ade_WX
            
#endif
        }

        void OnRecordStart()
        {
            RecordPlay?.Invoke(true);
            RecordPlay = null;
            LogManager.Log("OnRecordStart");
        }
        void OnRecordError()
        {
            RecordPlay?.Invoke(false);
            RecordPlay = null;
            RecordEnd = null;
            LogManager.LogError("OnRecordError");
        }
        void OnRecordTimeout(string videoPath)
        {
            RecordVideoPath = videoPath;
            RecordEnd?.Invoke(VideoRecordState.RECORD_COMPLETED);
            RecordEnd = null;

        }

        void OnRecordComplete(string videoPath)
        {
            RecordVideoPath = videoPath;
            RecordEnd?.Invoke(VideoRecordState.RECORD_COMPLETED);
            RecordEnd = null;

        }

        void OnRecordErrorCallback()
        {
            RecordEnd?.Invoke(VideoRecordState.RECORD_COMPLETED);
            RecordEnd = null;
            Debug.Log("OnRecordErrorCallback");
        }

        /// <summary>
        /// 获取录屏状态
        /// </summary>
        /// <returns></returns>
        public VideoRecordState GetVideoRecordState()
        {
#if Ade_Debug
              return VideoRecordState.RECORD_ERROR;
#elif !Ade_TT && !Ade_WX
              return VideoRecordState.RECORD_ERROR;
#elif Ade_TT
            return (VideoRecordState)m_TTGameRecorder.GetVideoRecordState();

#elif Ade_WX
              return VideoRecordState.RECORD_ERROR;
#endif
        }

        /// <summary>
        /// 获取录屏长度
        /// </summary>
        /// <returns></returns>
        public int GetVideoRecordDuration()
        {
#if Ade_Debug
              return 0;
#elif !Ade_TT && !Ade_WX
              return 0;
#elif Ade_TT
                return 99;

#elif Ade_WX
              return 0;
#endif
        }
        string RecordVideoPath;
        /// <summary>
        /// 分享录屏
        /// </summary>
        /// <param name="action"></param>
        public void RecordVideoTestShare(Action<bool> action)
        {
            RecordVideoTestShareBack = action;
#if Ade_Debug
            action?.Invoke(true);
            RecordVideoTestShareBack = null;
#elif UNITY_EDITOR && !Ade_TT && !Ade_WX

#elif Ade_TT
            LogManager.Log(GetVideoRecordState());
            m_TTGameRecorder.ShareVideo(OnShareVideoSuccess, OnShareVideoFailed, OnShareVideoCancelled);
            Debug.Log("转发id" + _AdeDataInfo.ShareId);
            //var param = new JsonData
            //{
            //    ["query"] = "",
            //    ["channel"] = "video",
            //    ["templateId"] = _AdeDataInfo.ShareId,
            //    ["extra"] = 
            //    {
            //    ["videoPath"] = m_TTGameRecorder.
            //    }
            //}; //m_TTGameRecorder
            //TT.ShareAppMessage(param);
#elif Ade_WX
            
#endif

        }
        Action<bool> RecordVideoTestShareBack;
        private void OnShareVideoCancelled()
        {
            RecordVideoTestShareBack?.Invoke(false);
            RecordVideoTestShareBack = null;
            LogManager.Log("OnShareVideoCancelled");
        }

        private void OnShareVideoFailed<T>(T tt)
        {
            RecordVideoTestShareBack?.Invoke(false);
            RecordVideoTestShareBack = null;
            LogManager.Log($"OnShareVideoFailed - errMsg: {tt}");
        }

        private void OnShareVideoSuccess<T>(T tt)
        {
            RecordVideoTestShareBack?.Invoke(true);
            RecordVideoTestShareBack = null;
            LogManager.Log("OnShareVideoSuccess");
        }


        #endregion

        #region 转发

        /// <summary>
        /// 拉起转发 分享
        /// </summary>
        /// <param name="shareAppMessageAction">转发成功回调</param>
        public void OnShare(Action shareAppMessageAction = null)
        {
#if Ade_Debug
            shareAppMessageAction?.Invoke();
#elif Ade_TT
            Debug.Log("转发id"+_AdeDataInfo.ShareId);
            var param = new JsonData
            {
                ["templateId"] = _AdeDataInfo.ShareId
            };
            TT.ShareAppMessage(param, (msg) => 
            {
                shareAppMessageAction?.Invoke();
            });

#elif Ade_WX
            ShareAppMessageOption samo = new ShareAppMessageOption();
            samo.imageUrlId = _AdeDataInfo.ShareId;
            WX.ShareAppMessage(new ShareAppMessageOption());
#elif Ade_KS
            KS.ShareAppMessage(new ShareAppMessageOption());
#endif


        }

        #endregion

        #region 侧边栏

        /// <summary>
        /// 是否有侧边栏
        /// </summary>
        public bool isSidebar;
#if Ade_TT
        /// <summary>
        /// 拉取侧边栏
        /// </summary>
        public void GetSidebar()
        {
#if Ade_Debug
            SidebarBack?.Invoke();
#else
            var data = new JsonData
            {
                ["scene"] = "sidebar",
            };
            TT.NavigateToScene(data, () =>
            {
                Debug.Log("navigate to scene success");
            }, () =>
            {
                Debug.Log("navigate to scene complete");
            }, (errCode, errMsg) =>
            {
                Debug.Log($"navigate to scene error, errCode:{errCode}, errMsg:{errMsg}");
            });
#endif
        }
#elif Ade_KS
        /// <summary>
        /// 拉取侧边栏
        /// </summary>
        public void GetSidebar()
        {
            NavigateToSceneOption sceneOption = new NavigateToSceneOption();
            sceneOption.scene = "sidebar";
            KS.NavigateToScene(sceneOption);
        }
#elif Ade_BiliBili
        /// <summary>
        /// 拉取侧边栏
        /// </summary>
        public void GetSidebar()
        {
            LogManager.Log("Bl_NavigateToScene");
            var option = new NavigeateToSceneOption()
            {
                scene = "sidebar",
                success = (res) =>
                {
                    LogManager.Log("Bl_NavigateToScene success");
                    LogManager.Log(res.errMsg);
                },
                fail = (res) =>
                {
                    LogManager.Log("Bl_NavigateToScene fail");
                    LogManager.Log(res.errMsg);
                },
                complete = (res) =>
                {
                    LogManager.Log("Bl_NavigateToScene complete");
                    LogManager.Log(res.errMsg);
                }
            };
            WX.NavigateToScene(option);
        }
#endif
        #endregion

        #region 后台加载

        #endregion

        #region 登录

        Action<bool> LoginAction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action">返回是否登录成功</param>
        public void OnLogin(Action<bool> action)
        {
            LoginAction = action;
#if Ade_TT
#if Ade_Debug
            LoginAction?.Invoke(true);
#else
            TT.Login(LoginSuccess, LoginFailed, true);
#endif
#else
            LoginAction?.Invoke(true);
#endif
        }

        void LoginSuccess(string code, string anonymousCode, bool isLogin) 
        {
            LogManager.Log($"code:{code}_anonymousCode:{anonymousCode}");
            LoginAction?.Invoke(true);
        }

        void LoginFailed(string msg)
        {
            LoginAction?.Invoke(false);
        }

#endregion

        #region 排行榜
#if Ade_TT
        /// <summary>
        /// 提交分数
        /// </summary>
        /// <param name="score"></param>
        public void SubmitScore(string score,int level)
        {
            var param = new JsonData
            {
                ["dataType"] = 1,
                ["value"] = score,
                ["zoneId"] = "default",
                ["priority"] = level,
                ["extra"] ="",
            };
            TT.Login((ss, dd, ff) =>
            {
                TT.SetImRankData(param, (ok, err) => {
                    Debug.Log(ok ? "上传成功" : $"上传失败: {err}");
                });
            }, null, true);

        }

        /// <summary>
        /// 刷新排行榜
        /// </summary>
        public void RefreshLeaderboard()
        {
            var param = new JsonData
            {
                ["rankType"] = "all",
                ["dataType"] = 1,
                ["relationType"] = "all",
                ["suffix"] = "等级",
                ["zoneId"] = "default",
                ["rankTitle"] = "星球争霸榜"
            };

            TT.Login((ss, dd, ff) =>
            {
                TT.GetImRankList(param, (ok, err) =>
                {
                    if (ok)
                    {
                        Debug.Log("拉取成功，解析 UI");
                    }
                    else
                    {
                        Debug.LogError($"拉取失败: {err}");
                    }
                });
            }, null, true);
        }


#endif
        #endregion

        #region 订阅

        public Action SubscribeSuccessAction;

        /// <summary>
        /// 拉起订阅
        /// </summary>
        /// <param name="SuccessAction">成功回调</param>
        public void OnRequestSubscribeMessage(Action SuccessAction) 
        {
            LogManager.Log("拉起订阅");
#if Ade_Debug
            SuccessAction?.Invoke();
#elif UNITY_EDITOR && !Ade_TT && !Ade_WX
#elif Ade_TT
            TT.RequestSubscribeMessage(_AdeDataInfo.SubscribeTmplIds, (msg) => 
            {
                SuccessAction?.Invoke();
            });
#elif Ade_WX
          
            RequestSubscribeMessageOption request = new RequestSubscribeMessageOption();
            request.tmplIds = _AdeDataInfo.SubscribeTmplIds.ToArray();
            WX.RequestSubscribeMessage(request);
#endif
        }
        #endregion

        #region 推荐流

#if Ade_TT
        /// <summary>
        /// 推荐流启动场景类型
        /// </summary>
        public enum FeedSceneType
        {
            /// <summary>
            /// 非推荐流启动
            /// </summary>
            None,
            /// <summary>
            /// 复访版 - 用户召回场景
            /// </summary>
            FeedDirectPlay,
            /// <summary>
            /// 获客版 - 新用户获取场景
            /// </summary>
            FeedAcquisition
        }

#if UNITY_EDITOR
        public const string EditorFeedLaunchModeEditorPrefsKey = "Ade.Editor.FeedLaunchMode";
#endif

        public bool IsFeedSubscribe;

        public bool IsFeedPlay
        {
            get => CurrentFeedScene != FeedSceneType.None && isFeedPlay;
        }

        bool isFeedPlay;

        /// <summary>
        /// 当前推荐流启动场景类型
        /// </summary>
        public FeedSceneType CurrentFeedScene { get; private set; } = FeedSceneType.None;

        /// <summary>
        /// 检查并处理推荐流启动场景
        /// 应在游戏启动时调用
        /// </summary>
        /// <param name="onFeedDirectPlay">复访版启动回调(场景ID, 场景数据)</param>
        /// <param name="onFeedAcquisition">获客版启动回调</param>
        public void CheckFeedLaunchScene(Action onFeedDirectPlay, Action onFeedAcquisition, Action onNone)
        {
#if UNITY_EDITOR
            if (TryHandleEditorFeedLaunchScene(onFeedDirectPlay, onFeedAcquisition, onNone))
            {
                return;
            }
#endif
            var launchOptions = TT.GetLaunchOptionsSync();

            if (launchOptions == null)
            {
                CurrentFeedScene = FeedSceneType.None;
                onNone?.Invoke();
                return;
            }

            string launchScene = launchOptions.Scene ?? string.Empty;
            Debug.Log($"启动数据GetLaunchOptionsSync.launchOptions.Scene:" + launchScene);
            // 使用LaunchOption的scene属性
            string scene = launchScene.Length > 4 ? launchScene.Substring(launchScene.Length - 4) : string.Empty;
            string feedGameChannel = string.Empty;
            if (launchOptions.Query != null && launchOptions.Query.ContainsKey("feed_game_channel"))
            {
                feedGameChannel = launchOptions.Query["feed_game_channel"]?.ToString();
            }

            if (scene == "3041")
            {
                isFeedPlay = true;
                // 判断是否为推荐流直出场景(复访版)
                if (feedGameChannel == "1")
                {
                    CurrentFeedScene = FeedSceneType.FeedDirectPlay;

                    Debug.Log($"[推荐流-复访版] 启动");

                    // 触发回调
                    onFeedDirectPlay?.Invoke();
                }
                // 判断是否为获客版场景
                else
                {
                    CurrentFeedScene = FeedSceneType.FeedAcquisition;

                    Debug.Log("[推荐流-获客版] 启动");

                    // 触发回调
                    onFeedAcquisition?.Invoke();
                }
            }
            else
            {
                Debug.Log("正常 启动");
                CurrentFeedScene = FeedSceneType.None;
                onNone?.Invoke();
            }

        }

#if UNITY_EDITOR
        private bool TryHandleEditorFeedLaunchScene(Action onFeedDirectPlay, Action onFeedAcquisition, Action onNone)
        {
            FeedSceneType editorLaunchMode =
                (FeedSceneType)EditorPrefs.GetInt(EditorFeedLaunchModeEditorPrefsKey, (int)FeedSceneType.None);
            CurrentFeedScene = editorLaunchMode;
            isFeedPlay = editorLaunchMode != FeedSceneType.None;

            switch (editorLaunchMode)
            {
                case FeedSceneType.FeedDirectPlay:
                    Debug.Log("[推荐流-编辑器模拟] 复访版启动");
                    onFeedDirectPlay?.Invoke();
                    return true;
                case FeedSceneType.FeedAcquisition:
                    Debug.Log("[推荐流-编辑器模拟] 获客版启动");
                    onFeedAcquisition?.Invoke();
                    return true;
                default:
                    Debug.Log("[推荐流-编辑器模拟] 正常启动");
                    onNone?.Invoke();
                    return true;
            }
        }
#endif

        /// <summary>
        /// 上报推荐流场景加载完成
        /// 场景加载完成后必须调用,否则游戏不会在推荐流中展示
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onFail">失败回调</param>
        public void ReportFeedSceneReady(Action onSuccess = null, Action<string> onFail = null)
        {
            isFeedPlay = false;
            //TimerManager.Instance.SetTimeScale(0);
            var param = new JsonData
            {
                ["costTime"] = Time.time,

                ["sceneId"] = 7001
            };
            Debug.Log("上报推荐流场景加载完成");
            TT.ReportScene(param);
        }

        /// <summary>
        /// 请求订阅推荐流提醒
        /// </summary>
        /// <param name="onSuccess">订阅成功回调</param>
        /// <param name="onFail">订阅失败回调</param>
        public void RequestFeedSubscribe(Action onSuccess = null, Action<string> onFail = null)
        {
#if Ade_Debug
            IsFeedSubscribe = true;
            onSuccess?.Invoke();
            return;
#endif
            // 检查是否配置了contentIDs
            if (_AdeDataInfo.FeedContentIDs == null || _AdeDataInfo.FeedContentIDs.Count == 0)
            {
                Debug.LogError("[推荐流] FeedContentIDs未配置! 请在AdeDataInfo中配置推荐流内容ID");
                onFail?.Invoke("FeedContentIDs未配置");
                return;
            }

            // 构建contentIDs数组
            var contentIDsArray = new JsonData();
            contentIDsArray.SetJsonType(JsonType.Array);
            foreach (var id in _AdeDataInfo.FeedContentIDs)
            {
                contentIDsArray.Add(id);
            }

            //var param = new JsonData
            //{
            //    ["type"] = "play",              // 订阅类型: play=直玩
            //    ["scene"] = 3,                  // 场景类型: 3=推荐流
            //    ["contentIDs"] = contentIDsArray  // 内容ID数组,需在抖音开放平台配置
            //};

            var param = new JsonData
            {
                ["type"] = "play",
                ["allScene"] = true,
            };

            Debug.Log($"[推荐流] 请求订阅 - ContentIDs: {contentIDsArray}");

            TT.RequestFeedSubscribe(param,
                (result) =>
                {
                    FeedResponse res = JsonUtility.FromJson<FeedResponse>(result.ToString());
                    if (res.success)
                    {
                        Debug.Log($"[推荐流] 订阅成功" + result);
                        IsFeedSubscribe = true;
                        onSuccess?.Invoke();
                    }
                },
                (errCode, errMsg) =>
                {
                    Debug.LogWarning($"[推荐流] 订阅失败 - 错误码:{errCode}, 错误信息:{errMsg}");
                    onFail?.Invoke(errMsg);
                });

        }
        public class FeedResponse
        {
            public bool success;
            public string errMsg;
        }

        /// <summary>
        /// 查询推荐流订阅状态
        /// </summary>
        /// <param name="onResult">结果回调(是否已订阅)</param>
        public void CheckFeedSubscribeStatus(Action<bool> onResult)
        {
#if UNITY_EDITOR || Ade_Debug
            onResult?.Invoke(IsFeedSubscribe);
            return;
#endif
            var param = new JsonData
            {
                ["type"] = "play",      // 订阅类型: play=直玩
                ["scene"] = 2,          // 场景类型: 3=推荐流
            };

            TT.CheckFeedSubscribeStatus(param,
                (result) =>
                {
                    bool isSubscribed = result.ContainsKey("status") && (bool)result["status"];
                    Debug.Log($"[推荐流] 订阅状态查询: {(isSubscribed ? "已订阅" : "未订阅")}");
                    IsFeedSubscribe = isSubscribed;
                    onResult?.Invoke(isSubscribed);
                },
                (errCode, errMsg) =>
                {
                    Debug.LogError($"[推荐流] 订阅状态查询失败 - 错误码:{errCode}, 错误信息:{errMsg}");
                    onResult?.Invoke(false);
                });
        }

        /// <summary>
        /// 查询推荐流未订阅则拉起订阅
        /// </summary>
        public void CheckAndRequestFeed()
        {
            LogManager.Log("查询推荐流未订阅则拉起订阅");
            CheckFeedSubscribeStatus((ison) =>
            {
                if (!ison)
                {
                    RequestFeedSubscribe();
                }
            });
        }


        /// <summary>
        /// 注册从feed流进入和退出
        /// </summary>
        public void OnFeedStatusChange(Action<FeedStatusEnum> FeedStatusChangeEnumAction)
        {

            TT.OnFeedStatusChange((result) =>
            {
                LogManager.Log($"从Feed流中:{(result.Type.ToString() == "FeedEnter" ? "进入" : "退出")}");
                FeedStatusChangeEnumAction?.Invoke(result.Type);
                switch (result.Type)
                {
                    case FeedStatusEnum.FeedEnter:
                        
                        break;
                    case FeedStatusEnum.FeedExit:
                        break;
                    default:
                        break;
                }
            });
        }

        /// <summary>
        /// 复访推荐流
        /// </summary>
        public void StoreFeedData() 
        {
            LogManager.Log("复访推荐流单独上报");
            StoreFeedDataParam storeFeedData = new StoreFeedDataParam();
            // 期望 30s 后出卡
            var currentTime = DateTimeOffset.UtcNow;
            var milliseconds = currentTime.ToUnixTimeMilliseconds();
            var targetMilliSeconds = milliseconds + 30 * 1000;
            storeFeedData.ContentID = _AdeDataInfo.FeedRepeatContentID;
            storeFeedData.Scene = 3;
            storeFeedData.Status = 1;
            storeFeedData.Operator = ">=";
            storeFeedData.Extra = "";
            storeFeedData.RightValue = targetMilliSeconds.ToString();
            storeFeedData.LeftValue = "timeStampMs";

            storeFeedData.Fail = (info) =>
            {
                LogManager.Log("复访推荐流单独上报错误：" + info.ErrMsg);
            };
#if !UNITY_EDITOR && !Ade_Debug
            TT.StoreFeedData(storeFeedData);
#endif

        }
        /// <summary>
        /// 体力恢复复访推荐流
        /// </summary>
        public void StoreFeedDataforVit()
        {
            LogManager.Log("复访推荐流单独上报");
            StoreFeedDataParam storeFeedData = new StoreFeedDataParam();
            // 期望 30s 后出卡
            var currentTime = DateTimeOffset.UtcNow;
            var milliseconds = currentTime.ToUnixTimeMilliseconds();
            var targetMilliSeconds = milliseconds + 30 * 1000;
            storeFeedData.ContentID = _AdeDataInfo.FeedRepeatContentID;
            storeFeedData.Scene = 2;
            storeFeedData.Status = 1;
            storeFeedData.Operator = ">=";
            storeFeedData.Extra = "";
            storeFeedData.RightValue = targetMilliSeconds.ToString();
            storeFeedData.LeftValue = "timeStampMs";

            storeFeedData.Fail = (info) =>
            {
                LogManager.Log("复访推荐流单独上报错误：" + info.ErrMsg);
            };
#if !UNITY_EDITOR && !Ade_Debug
            TT.StoreFeedData(storeFeedData);
#endif

        }

        /// <summary>
        /// 推荐流数据统计 - 启动
        /// </summary>
        /// <param name="sceneType">场景类型</param>
        private void ReportFeedLaunch(FeedSceneType sceneType)
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>
            {
                ["scene_type"] = sceneType.ToString(),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            OnReportEvent(ReportName.feed_direct_launch, eventData);
        }

        /// <summary>
        /// 推荐流数据统计 - 场景加载
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="loadTime">加载时长(毫秒)</param>
        public void ReportFeedSceneLoad(string sceneId, long loadTime)
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>
            {
                ["scene_id"] = sceneId,
                ["load_time"] = loadTime
            };

            OnReportEvent(ReportName.feed_scene_load, eventData);
        }

        /// <summary>
        /// 推荐流数据统计 - 用户留存(复访版)
        /// </summary>
        /// <param name="day">留存天数(1=次日, 7=7日)</param>
        public void ReportFeedRetention(int day)
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>
            {
                ["day"] = day,
                ["source"] = "feed_direct"
            };

            OnReportEvent(ReportName.feed_retention, eventData);
        }

        /// <summary>
        /// 推荐流数据统计 - 获客转化(获客版)
        /// </summary>
        public void ReportFeedAcquisitionConversion()
        {
            Dictionary<string, object> eventData = new Dictionary<string, object>
            {
                ["source"] = "feed_acquisition",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            OnReportEvent(ReportName.acquisition_conversion, eventData);
        }
#endif

        #endregion


        #region 添加桌面
#if Ade_BiliBili
    /// <summary>
    /// 添加桌面快捷方式
    /// </summary>
    public void AddShortcut()
    {
        LogManager.Log("Bl_AddShortcut");
        var option = new AddShortcutOption()
        {
            success = (res) =>
            {
                LogManager.Log("Bl_AddShortcut success");
                LogManager.Log(res.errMsg);
            },
            fail = (res) =>
            {
                LogManager.Log("Bl_AddShortcut fail");
                LogManager.Log(res.errMsg);
            },
            complete = (res) =>
            {
                LogManager.Log("Bl_AddShortcut complete");
                LogManager.Log(res.errMsg);
            }
        };
        WX.AddShortcut(option);
    }
    
    /// <summary>
    /// 检查是否已添加桌面快捷方式
    /// </summary>
    public void CheckShortcut(Action<bool> ShortcutAction)
    {
        LogManager.Log("Bl_CheckShortcut");
        var option = new CheckShortcutOption()
        {
            success = (res) =>
            {
                ShortcutAction?.Invoke(true);
            },
            fail = (res) =>
            {
                ShortcutAction?.Invoke(false);
            },
            complete = (res) =>
            {
                LogManager.Log("Bl_CheckShortcut complete");
                LogManager.Log(res.errMsg);
            }
        };
        WX.CheckShortcut(option);
    }
#endif
        #endregion
    }

}

//
// 摘要:
/// <summary>
///    录屏状态枚举
/// </summary>
public enum VideoRecordState
{
    /// <summary>
    ///    录制开始中
    /// </summary>
    RECORD_STARTING,
    //
    // 摘要:
    /// <summary>
    ///     录制已开始
    /// </summary>
    RECORD_STARTED,
    //
    // 摘要:
    /// <summary>
    ///     录制暂停中
    /// </summary>
    RECORD_PAUSING,
    //
    // 摘要:
    /// <summary>
    ///     录制已暂停
    /// </summary>
    RECORD_PAUSED,
    //
    // 摘要:
    /// <summary>
    ///     录制停止中
    /// </summary>
    RECORD_STOPING,
    //
    // 摘要:
    /// <summary>
    ///     录制已停止
    /// </summary>
    RECORD_STOPED,
    //
    // 摘要:
    /// <summary>
    ///     录制结束
    /// </summary>
    RECORD_COMPLETED,
    //
    // 摘要:
    /// <summary>
    ///     录制错误
    /// </summary>
    RECORD_ERROR,
    //
    // 摘要:
    /// <summary>
    ///     录制的视频时长太短
    /// </summary>
    RECORD_VIDEO_TOO_SHORT
}
