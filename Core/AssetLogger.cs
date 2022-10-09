/*
 * @Author: chiuan wei 
 * @Date: 2017-07-06 15:14:34 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-06 15:16:39
 */
#define ENABLE_ASSET_LOG
//#define RUNTIME_LOGGER

using System;
using UnityEngine;

#if RUNTIME_LOGGER
using TinyTeam.Debuger;
#endif

namespace SuperMobs.AssetManager.Core
{
    public class AssetLogger
    {
#if ENABLE_ASSET_LOG
        public static bool enableLog = true;
#else
      public static bool enableLog = false;
#endif

#if RUNTIME_LOGGER
        // static bool _registCMD = false;
        // static void Register() {
        //    if (_registCMD == false) {
        //       _registCMD = true;
        //    } else {
        //       return;
        //    }

        //    TTDebuger.RegisterCommand("am", (arg) => { }, "快捷显示assets的log");
        // }
#endif

        public static void Log(string content, string type = "asset")
        {

            if (enableLog)
            {
#if RUNTIME_LOGGER
                TTDebuger.Log(content, type);
#else
            Debug.Log(content);
#endif
            }
        }

        public static void LogWarning(string content, string type = "asset")
        {
            if (enableLog)
            {
#if RUNTIME_LOGGER
                TTDebuger.LogWarning(content, type);
#else
            Debug.LogWarning(content);
#endif
            }
        }

        public static void LogError(string content, string type = "asset")
        {
            if (enableLog)
            {
#if RUNTIME_LOGGER
                TTDebuger.LogError(content, type);
#else
            Debug.LogError(content);
#endif
            }
        }

        public static void LogException(string content, string type = "asset")
        {
            if (enableLog)
            {
#if RUNTIME_LOGGER
                TTDebuger.LogError(content, type);
#else
            Debug.LogError(content);
#endif
            }
        }

        public static void Log(Color color, string content, string type = "asset")
        {
            if (enableLog)
#if RUNTIME_LOGGER
                TTDebuger.LogError("<color=#" + color.ColorToHex() + ">" + content + "</color>", type);
#else
            Debug.Log("<color=#" + color.ColorToHex() + ">" + content + "</color>");
#endif
        }
    }
}