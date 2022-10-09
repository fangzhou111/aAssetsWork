using System;
using System.Reflection;
using UnityEngine;

namespace SuperMobs.Core
{
    public enum RuntimeInitializeType
    {
        AfterSceneLoad,
        BeforeSceneLoad,
        AfterAssetsUpdated,
        OnRestart
    }

    public class RuntimeInitialize : Attribute
    {
        public readonly RuntimeInitializeType initType;
        public readonly int order;
        public RuntimeInitialize(RuntimeInitializeType initType, int order)
        {
            this.initType = initType;
            this.order = order;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitializeBeforeSceneLoad()
        {
            Debug.Log("RuntimeInitializeLoadType.BeforeSceneLoad");

            RuntimeInitializeTypes.ReInit();

            if (RuntimeInitializeTypes.instance == null)
            {
                Debug.LogError("RuntimeInitializeTypes.instance is null");
            }

            foreach (RuntimeInitializeTypes.FunctionLocation location in RuntimeInitializeTypes.instance.beforeSceneLoad)
                location.Execute();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInitializeAfterSceneLoad()
        {
            RuntimeInitializeTypes.ReInit();
            foreach (RuntimeInitializeTypes.FunctionLocation location in RuntimeInitializeTypes.instance.afterSceneLoad)
                location.Execute();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Edit/Test-AfterAssetsUpdated %_q")]
        public static void RuntimeInitializeAfterAssetsUpdated()
        {
            if (!Application.isPlaying) return;
#else
        public static void RuntimeInitializeAfterAssetsUpdated()
        {
#endif
            RuntimeInitializeTypes.ReInit();
            foreach (RuntimeInitializeTypes.FunctionLocation location in RuntimeInitializeTypes.instance.afterAssetsUpdated)
                location.Execute();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Edit/Test-OnRestart %_w")]
        public static void RuntimeInitializeOnRestart()
        {
            if (!Application.isPlaying) return;
#else
        public static void RuntimeInitializeOnRestart()
        {
#endif
            RuntimeInitializeTypes.ReInit();
            foreach (RuntimeInitializeTypes.FunctionLocation location in RuntimeInitializeTypes.instance.onRestart)
                location.Execute();
        }
    }

    [Serializable]
    public class RuntimeInitializeTypes : ScriptableObject
    {
        [Serializable]
        public struct FunctionLocation
        {
            public string assembly;
            public string type;
            public string func;
            public bool enable;

            public FunctionLocation(string assembly, string type, string func)
            {
                this.assembly = assembly;
                this.type = type;
                this.func = func;
                this.enable = true;
            }

            public void Execute()
            {
                if (!enable)
                {
                    Debug.Log("[RuntimeInitialize] cancel init " + type + ":" + func);
                    return;
                }

                try
                {
                    Debug.Log("[RuntimeInitialize] init " + type + ":" + func);
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    Assembly.Load(assembly)
                        .GetType(type)
                        .GetMethod(func, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, null);
                    sw.Stop();
                    Debug.Log("[RuntimeInitialize] cost " + sw.ElapsedMilliseconds + "ms");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public FunctionLocation[] beforeSceneLoad = new FunctionLocation[0];
        public FunctionLocation[] afterSceneLoad = new FunctionLocation[0];
        public FunctionLocation[] afterAssetsUpdated = new FunctionLocation[0];
        public FunctionLocation[] onRestart = new FunctionLocation[0];

        public const string SAVE_ASSET_NAME = "runtime_initialize_type_config";
        public static RuntimeInitializeTypes instance { get; private set; }
        public static void ReInit() { if (instance == null) instance = Resources.Load<RuntimeInitializeTypes>(SAVE_ASSET_NAME); }
        public static void Init(RuntimeInitializeTypes types)
        {
            if (instance != null)
            {
                Debug.LogException(new Exception("cant cover the instance of RuntimeInitializeTypes,because is exist already!"));
            }
            else
            {
                instance = types;
            }
        }
        static RuntimeInitializeTypes() { }

        void OnEnable()
        {
            ReInit();
        }
    }
}