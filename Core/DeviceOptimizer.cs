using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IPHONE || UNITY_IOS
using UnityEngine.iOS;
#endif

namespace SuperMobs.AssetManager.Core
{
	class DeviceOptimizerController : MonoBehaviour
	{
		void Awake()
		{
			DontDestroyOnLoad(this.gameObject);
			this.gameObject.hideFlags = HideFlags.HideInHierarchy;
		}

		void OnApplicationFocus(bool focusStatus)
		{
			if (!focusStatus)
			{
				DeviceOptimizer.FixResolution();
			}
		}
	}

	/// <summary>
	/// 根据不同设备的GPU & CPU综合性能
	/// 设备自动优化分辨率
	/// </summary>
	public class DeviceOptimizer
	{
		public enum Mode
		{
			Android,
			iOS,
			Both,
			None,
		}
		static Mode mode = Mode.Android; // 默认只fix安卓(安卓太猛了!)
		private static int targetHeight = 640;
		private static int cachedWidth = 0;
		private static int cachedHeight = 0;

		private static bool init = false;

        public static void InitMode(Mode mod)
        {
            mode = mod;
        }

		/// <summary>
		/// 设置屏幕的分辨率，在android达到优化效率
		/// </summary>
		public static void InitDeviceOptimized(Mode mod)
		{
			if (init) return;
			init = true;

			//set render
			cachedWidth = Screen.width;
			cachedHeight = Screen.height;
			mode = mod;

			int gpu = GetGPULevel();
			PlayerPrefs.SetInt("GPU_LEVEL", gpu);

			//android
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
			{
				if (gpu == 1)
				{
					//targetHeight = 600;
					//QualitySettings.SetQualityLevel(0);
				}
				else if (gpu == 2)
				{
					targetHeight = 640;
				}
			}
			else if (Application.platform == RuntimePlatform.IPhonePlayer && !Application.isEditor)
			{
				if (gpu == 3)
				{
					targetHeight = (int) (cachedHeight * 0.7f);
				}
				else
				{
					targetHeight = (int) (cachedHeight * 0.5f);
				}

				if (targetHeight < 640) targetHeight = 640;
			}

			//#if UNITY_IOS || UNITY_IPHONE
			//        if(Application.platform == RuntimePlatform.IPhonePlayer && !Application.isEditor)
			//        {
			//            DeviceGeneration iOSGen = Device.generation;
			//            if (iOSGen == DeviceGeneration.iPad1Gen || iOSGen == DeviceGeneration.iPad2Gen || iOSGen == DeviceGeneration.iPad3Gen
			//               || iOSGen == DeviceGeneration.iPadMini1Gen || iOSGen == DeviceGeneration.iPhone || iOSGen == DeviceGeneration.iPhone3GS
			//               || iOSGen == DeviceGeneration.iPhone3G || iOSGen == DeviceGeneration.iPhone4 || iOSGen == DeviceGeneration.iPhone4S
			//               || iOSGen == DeviceGeneration.iPodTouch1Gen || iOSGen == DeviceGeneration.iPodTouch2Gen || iOSGen == DeviceGeneration.iPodTouch2Gen
			//               || iOSGen == DeviceGeneration.iPodTouch3Gen)
			//            {
			//                //0:Fastest
			//                QualitySettings.SetQualityLevel(0);
			//            }
			//        }
			//#endif

			//调整分辨率适配一下设备的性能
			FixResolution();

			GameObject go = new GameObject("DeviceOptimizerController");
			go.AddComponent<DeviceOptimizerController>();
		}

		/// <summary>
		/// 检查GPU的性能
		/// 1：最差
		/// 2：较好
		/// 3：最好
		/// </summary>
		public static int GetGPULevel()
		{
			if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
			{
				//主流判断
				if (SystemInfo.supportsGyroscope == false) return 1;
				if (SystemInfo.supportedRenderTargetCount < 4) return 1;
				if (SystemInfo.supports3DTextures == false) return 1;
				//OpenGL ES 2.0
				if (SystemInfo.graphicsDeviceVersion.Replace(" ", "").ToLower().Substring(8, 1).Equals("2")) return 1;

				if (!SystemInfo.graphicsDeviceVendor.StartsWith("qual", StringComparison.OrdinalIgnoreCase)) return 2;
				if (!SystemInfo.graphicsDeviceName.StartsWith("adreno", StringComparison.OrdinalIgnoreCase)) return 2;

				//Adreno (TM) 320
				int gdnl = 0;
				if (int.TryParse(SystemInfo.graphicsDeviceName.Replace(" ", "").Substring(10, 3), out gdnl))
				{
					if (gdnl <= 320) return 2;
				}
				else
				{
					return 2;
				}

				//Android OS 5.0.2
				string on = SystemInfo.operatingSystem.Replace(" ", "").Substring(9, 1);
				int oni = 0;
				if (int.TryParse(on, out oni))
				{
					if (oni < 5) return 2;
				}
				else
				{
					return 2;
				}

				if (Screen.dpi < 380)
				{
					return 2;
				}

				return 3;
			}

#if UNITY_IOS || UNITY_IPHONE
			else if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
                if ((int)Device.generation > (int)DeviceGeneration.iPhone6S)
                    return 3;
                else
                    return 2;
			}
#endif

            return 3;
		}

		public static void FixResolution()
		{
			if (AssetPreference.AUTO_FIX_RESOLUTION == false)
			{
                AssetLogger.Log("不需要自动缩放分辨率auto fix resolution = false");
				return;
			}

            if (Application.isEditor)
            {
                AssetLogger.Log("不需要自动缩放分辨率editor");
                return;
            }

			if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
			{
				if(mode != Mode.Android && mode != Mode.Both)	
				{
                    AssetLogger.Log("不需要自动缩放分辨率不支持android");
					return;
				}
			}
			else if (Application.platform == RuntimePlatform.IPhonePlayer && !Application.isEditor)
			{
				if(mode != Mode.iOS && mode != Mode.Both)	
				{
                    AssetLogger.Log("不需要自动缩放分辨率不支持iOS");
					return;
				}	
			}

            if (cachedHeight > targetHeight)
            {
                int width = (int)((cachedWidth * 1.0f / cachedHeight) * targetHeight);
                if (Screen.width != width || Screen.height != targetHeight)
                {
                    AssetLogger.Log("设置新的分辨率:" + width + "," + targetHeight);
                    Screen.SetResolution(width, targetHeight, true);
                }
            }
            else
            {
                AssetLogger.Log("不需要自动缩放分辨率不支持cachedHeight=" + cachedHeight + ",targetHeight="+targetHeight);
            }
		}

	}
}