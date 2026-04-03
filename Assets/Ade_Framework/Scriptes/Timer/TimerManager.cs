using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;


namespace Ade_Framework
{
    public class TimerManager : SingleMono<TimerManager>
    {

#if UNITY_EDITOR
        // 的 Debug属性，用于 Inspector 调试显示（只读）
        public IReadOnlyDictionary<string, Action<float>> DebugUpdataActions => UpdataActions;
        public IReadOnlyDictionary<string, Action<float>> DebugLateUpdataActions => LateUpdataActions;
        public IReadOnlyDictionary<string, Action<float>> DebugFixedUpdataActions => FixedUpdataActions;
        public IReadOnlyDictionary<string, Countdown> DebugControllableCountdowns => controllableCountdowns;
        public IReadOnlyDictionary<string, Countdown> DebugUncontrollableCountdowns => uncontrollableCountdowns;
#endif
        /// <summary>
        /// 总控制器
        /// </summary>
        public bool IsPlaying = false;

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="Scale"></param>
        public void SetTimeScale(float Scale)
        {
            Time.timeScale = Scale;
        }

        public void Init() 
        {
            IsPlaying = true;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        #region Mono事件 updata 

        Action FixedUpdateAction;

        private Dictionary<string, Action<float>> UpdataActions = new();
        /// <summary>
        /// 添加updata事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void OnAddUpdataAction(string id,Action<float> action)
        {
            if (UpdataActions.ContainsKey(id)) return;

            FixedUpdateAction += () => { UpdataActions[id] = action; };
        }
        /// <summary>
        /// 移除updata事件
        /// </summary>
        /// <param name="id"></param>
        public void OnRemoveUpdataAction(string id)
        {
            if (!UpdataActions.ContainsKey(id)) return;

            StopCountdownAction += () => { UpdataActions.Remove(id); };
            
        }

        private Dictionary<string, Action<float>> unUpdataActions = new();
        /// <summary>
        /// 添加不可控updata事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void OnAddUnUpdataAction(string id, Action<float> action)
        {
            if (unUpdataActions.ContainsKey(id)) return;

            FixedUpdateAction += () => { unUpdataActions[id] = action; };
        }
        /// <summary>
        /// 移除不可控updata事件
        /// </summary>
        /// <param name="id"></param>
        public void OnRemoveUnUpdataAction(string id)
        {
            if (!unUpdataActions.ContainsKey(id)) return;

            StopCountdownAction += () => { unUpdataActions.Remove(id); };

        }

        private Dictionary<string, Action<float>> FixedUpdataActions = new();
        /// <summary>
        /// 添加Fixedupdata事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void OnAddFixedUpdataAction(string id, Action<float> action)
        {
            if (FixedUpdataActions.ContainsKey(id)) return;

            FixedUpdateAction += () => { FixedUpdataActions[id] = action; };
        }
        /// <summary>
        /// 移除Fixedupdata事件
        /// </summary>
        /// <param name="id"></param>
        public void OnRemoveFixedUpdataAction(string id)
        {
            if (!FixedUpdataActions.ContainsKey(id)) return;

            StopCountdownAction += () => { FixedUpdataActions.Remove(id); };
            
        }

        private Dictionary<string, Action<float>> LateUpdataActions = new();
        /// <summary>
        /// 添加Lateupdata事件
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        public void OnAddLateUpdataAction(string id, Action<float> action)
        {
            if (LateUpdataActions.ContainsKey(id)) return;
            FixedUpdateAction += () => { LateUpdataActions[id] = action; };
            
        }
        /// <summary>
        /// 移除Lateupdata事件
        /// </summary>
        /// <param name="id"></param>
        public void OnRemoveLateUpdataAction(string id)
        {
            if (!LateUpdataActions.ContainsKey(id)) return;

            StopCountdownAction += () => { LateUpdataActions.Remove(id); };
          
        }
        #endregion

        #region 倒计时
        /// <summary>
        /// 可控的
        /// </summary>
        private Dictionary<string, Countdown> controllableCountdowns = new();
        /// <summary>
        /// 不可控
        /// </summary>
        private Dictionary<string, Countdown> uncontrollableCountdowns = new();


        /// <summary>
        /// 开始倒计时
        /// </summary>
        /// <param name="id"></param>
        /// <param name="duration"></param>
        /// <param name="onTick"></param>
        /// <param name="onFinish"></param>
        /// <param name="iscontrollable">是否受控制 false时不受IsPlaying控制 </param>
        public void StartCountdown(string id, float duration, Action<float> onTick = null, Action onFinish = null, bool iscontrollable = true)
        {
            if (iscontrollable)
            {
                if (controllableCountdowns.ContainsKey(id))
                {
                    controllableCountdowns[id].Reset(duration, onTick, onFinish);
                }
                else
                {
                    var newCountdown = new Countdown(duration, onTick, onFinish);
                    FixedUpdateAction += () => { controllableCountdowns.Add(id, newCountdown); };
                }
            }
            else
            {
                if (uncontrollableCountdowns.ContainsKey(id))
                {
                    uncontrollableCountdowns[id].Reset(duration, onTick, onFinish);
                }
                else
                {
                    var newCountdown = new Countdown(duration, onTick, onFinish);
                    FixedUpdateAction += () => { uncontrollableCountdowns.Add(id, newCountdown); };
                }
            }

        }

        /// <summary>
        /// 添加时间 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="duration"></param>
        public void AddTimeCountdown(string id, float duration)
        {
            if (controllableCountdowns.ContainsKey(id))
            {
                controllableCountdowns[id].AddTime(duration);
            }
            else if (uncontrollableCountdowns.ContainsKey(id))
            {
                uncontrollableCountdowns[id].AddTime(duration);
            }
        }

        /// <summary>
        /// 暂停计时
        /// </summary>
        /// <param name="id"></param>
        public void PauseCountdown(string id)
        {
            if (controllableCountdowns.TryGetValue(id, out var countdown))
            {
                countdown.Pause();
            }
            else if (uncontrollableCountdowns.TryGetValue(id, out var uncountdown))
            {
                countdown.Pause();
            }

        }

        /// <summary>
        /// 恢复计时
        /// </summary>
        /// <param name="id"></param>
        public void ResumeCountdown(string id)
        {
            if (controllableCountdowns.TryGetValue(id, out var countdown))
            {
                countdown.Resume();
            }
            else if (uncontrollableCountdowns.TryGetValue(id, out var uncountdown))
            {
                countdown.Resume();
            }
        }

        /// <summary>
        /// 删除委托
        /// </summary>
        Action StopCountdownAction;
        /// <summary>
        /// 删除计时
        /// </summary>
        /// <param name="id"></param>
        public void StopCountdown(string id)
        {
            if(controllableCountdowns.ContainsKey(id)) StopCountdownAction += ()=> { controllableCountdowns.Remove(id); LogManager.Log("删除计时完毕");};

            if (uncontrollableCountdowns.ContainsKey(id)) StopCountdownAction += () => { uncontrollableCountdowns.Remove(id); };
        }

        /// <summary>
        /// 获取倒计时 剩余时长
        /// </summary>
        /// <param name="id"></param>
        public float GetTimeLeft(string id)
        {
            return controllableCountdowns.TryGetValue(id, out var c) ? c.TimeLeft : uncontrollableCountdowns.TryGetValue(id, out var uc) ? uc.TimeLeft : 0;
        }

        /// <summary>
        /// 获取已用时间
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public float GetGetTimeHasPassed(string id)
        {
            if (controllableCountdowns.TryGetValue(id, out var countdown))
            {
                return countdown.GetTimeHasPassed;
            }
            else if (uncontrollableCountdowns.TryGetValue(id, out var uncountdown))
            {
                return uncountdown.GetTimeHasPassed;
            }
            return 0;
        }
        [SerializeField]
        public class Countdown
        {
            public float TimeLeft { get; private set; }
            public float InitialDuration { get; private set; }
            private Action<float> onTick;
            private Action onFinish;
            private bool isRunning = true;

            public Countdown(float duration, Action<float> onTick, Action onFinish)
            {
                Reset(duration, onTick, onFinish);
            }

            public void Reset(float duration, Action<float> tick, Action finish)
            {
                InitialDuration = duration;
                TimeLeft = duration;
                onTick = tick;
                onFinish = finish;
                isRunning = true;
            }

            public void AddTime(float duration)
            {
                InitialDuration += duration;
                TimeLeft += duration;
                isRunning = true;
            }

            public float GetTimeHasPassed { get => InitialDuration - TimeLeft; }

            /// <summary>
            /// 暂停倒计时
            /// </summary>
            public void Pause() => isRunning = false;
            /// <summary>
            /// 继续倒计时
            /// </summary>
            public void Resume() => isRunning = true;

            public void Update(float delta)
            {
                if (!isRunning) return;
                TimeLeft -= delta;
                onTick?.Invoke(Mathf.Max(TimeLeft, 0f));
                if (TimeLeft <= 0f)
                {
                    isRunning = false;
                    onFinish?.Invoke();
                }
            }
        }
        #endregion

        #region 协程
        public Coroutine RunCoroutine(IEnumerator routine) => StartCoroutine(routine);

        public void StopRoutine(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        public void StartOneWaitFrame(int Frame , Action action) { RunCoroutine(WaitOneFrame(Frame, action)); }

        public IEnumerator WaitOneFrame(int Frame , Action action)
        {
            yield return null;
            action?.Invoke();
            yield break;
        }
        #endregion


        void FixedUpdate()
        {
            FixedUpdateAction?.Invoke();
            FixedUpdateAction = null;

            if (!IsPlaying) return;

            float delta = Time.deltaTime;
            foreach (var item in FixedUpdataActions.Keys)
            {
                FixedUpdataActions[item]?.Invoke(delta);
            }
        }

        void Update()
        {
            float delta = Time.deltaTime;
            foreach (var kvp in uncontrollableCountdowns)
            {
                kvp.Value?.Update(delta);
            }

            foreach (var item in unUpdataActions)
            {
                item.Value?.Invoke(delta);
            }

            if (!IsPlaying) return;

            foreach (var kvp in controllableCountdowns)
            {
                kvp.Value?.Update(delta);
            }

            Dictionary<string, Action<float>> aa = UpdataActions;
            foreach (var item in aa.Keys)
            {
                aa[item]?.Invoke(delta);
            }
        }

        private void LateUpdate()
        {
            if (IsPlaying) 
            {
                float delta = Time.deltaTime;
                foreach (var item in LateUpdataActions.Keys)
                {
                    LateUpdataActions[item]?.Invoke(delta);
                }
            }


            StopCountdownAction?.Invoke();
            StopCountdownAction = null;
        }

    }
}
