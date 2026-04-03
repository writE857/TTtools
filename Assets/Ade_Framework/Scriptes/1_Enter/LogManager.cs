using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ade_Framework 
{
    public static class LogManager
    {
        private static bool isDebug = true;

        /// <summary>
        /// 常规输出
        /// </summary>
        /// <param name="message">输出信息</param> <summary>
        public static void Log<T>(T message)
        {
            if (!isDebug)
            {
                return;
            }
            Debug.Log(message.ToString());
        }

        /// <summary>
        /// 常规输出（带颜色）
        /// </summary>
        /// <param name="message">输出信息</param> <summary>
        public static void Log<T>(T message, Color color)
        {
            if (!isDebug)
            {
                return;
            }
            Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + message.ToString() + "</color>");
        }

        /// <summary>
        /// 警告输出
        /// </summary>
        /// <param name="message">输出信息</param> <summary>
        public static void LogWarning<T>(T message)
        {
            if (!isDebug)
            {
                return;
            }
            Debug.LogWarning(message);
        }

        /// <summary>
        /// 错误输出
        /// </summary>
        /// <param name="message">输出信息</param> <summary>
        public static void LogError<T>(T message)
        {
            if (!isDebug)
            {
                return;
            }
            Debug.LogError(message);
        }
    }
}

