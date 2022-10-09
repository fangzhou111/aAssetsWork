/*
 * @Author: chiuan wei 
 * @Date: 2017-07-06 11:42:01 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-07-06 13:13:32
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMobs.AssetManager.Assets
{
    /// <summary>
    /// 场景光照信息
    /// 
    /// 用于还原
    /// </summary>
    [Serializable]
    public class SceneLightingData : ScriptableObject
    {
        public Texture2D[] lightmapFar;
        public Texture2D[] lightmapNear;

        public LightmapsMode lightmapsMode;

        public int[] renderLightIndexs;
        public int[] renderRealtimeIndexs;
        public Vector4[] renderLightScaleOffset;
        public Vector4[] renderRealtimeScaleOffset;

        // 缓存场景的光照天空盒信息
        public Material skyboxMaterial;
        public int ambientMode;
        public float ambientIntensity;
        public Color ambientSkyColor;
        public Color ambientEquatorColor;
        public Color ambientGroundColor;

        public bool fog;
        public Color fogColor;
        public float fogDensity;
        public float fogEndDistance;
        public FogMode fogMode;
        public float fogStartDistance;

        public void Apply(Renderer[] renders)
        {
            for (int i = 0; i < renders.Length; i++)
            {
                // maybe delete when build over.
                if (renders[i] == null)
                {
                    continue;
                }

                renders[i].lightmapIndex = renderLightIndexs[i];
                renders[i].lightmapScaleOffset = renderLightScaleOffset[i];
                //renders[i].realtimeLightmapIndex = -1;
                //renders[i].realtimeLightmapScaleOffset = renderRealtimeScaleOffset[i];
            }

            // render setting for skycolor.
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.ambientMode = (UnityEngine.Rendering.AmbientMode)ambientMode;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;

            RenderSettings.skybox = skyboxMaterial;
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

            // 直接设置最新的
            LightmapData[] data = new LightmapData[lightmapFar.Length];
            LightmapSettings.lightmaps = null;

            for (int i = 0; i < lightmapFar.Length; i++)
            {
                data[i] = new LightmapData();
#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
				data[i].lightmapColor = lightmapFar[i];
#else
                data[i].lightmapFar = lightmapFar[i];
#endif
            }
            LightmapSettings.lightmaps = data;

            RenderSettings.fog = fog;
            if (fog)
            {
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = fogMode;
                if (fogMode == FogMode.Linear)
                {
                    RenderSettings.fogStartDistance = fogStartDistance;
                    RenderSettings.fogEndDistance = fogEndDistance;
                }
                else
                {
                    RenderSettings.fogDensity = fogDensity;
                }
            }
        }

        public void Record(Renderer[] renders)
        {
            // render setting
            skyboxMaterial = RenderSettings.skybox;
            ambientMode = (int)RenderSettings.ambientMode;
            ambientIntensity = RenderSettings.ambientIntensity;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;

            // lightingmap
            lightmapsMode = LightmapSettings.lightmapsMode;
            lightmapFar = new Texture2D[LightmapSettings.lightmaps.Length];
            lightmapNear = new Texture2D[LightmapSettings.lightmaps.Length];
            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                if (LightmapSettings.lightmaps[i] == null)
                    continue;

#if UNITY_5_6_OR_NEWER || UNITY_2017_2_OR_NEWER
				lightmapFar[i] = LightmapSettings.lightmaps[i].lightmapColor;
                lightmapNear[i] = LightmapSettings.lightmaps[i].lightmapDir;
#else
            lightmapFar[i] = LightmapSettings.lightmaps[i].lightmapFar;
            lightmapNear[i] = LightmapSettings.lightmaps[i].lightmapNear;
#endif
            }

            renderLightIndexs = new int[renders.Length];
            renderRealtimeIndexs = new int[renders.Length];
            renderLightScaleOffset = new Vector4[renders.Length];
            renderRealtimeScaleOffset = new Vector4[renders.Length];

            for (int i = 0; i < renders.Length; i++)
            {
                if (renders[i] == null)
                {
                    continue;
                }

                renderLightIndexs[i] = renders[i].lightmapIndex;
                renderRealtimeIndexs[i] = renders[i].realtimeLightmapIndex;
                renderLightScaleOffset[i] = renders[i].lightmapScaleOffset;
                renderRealtimeScaleOffset[i] = renders[i].realtimeLightmapScaleOffset;
            }

            fog = RenderSettings.fog;
            if (fog)
            {
                fogMode = RenderSettings.fogMode;
                fogColor = RenderSettings.fogColor;
                fogStartDistance = RenderSettings.fogStartDistance;
                fogEndDistance = RenderSettings.fogEndDistance;
                fogDensity = RenderSettings.fogDensity;
            }
        }
    }

}