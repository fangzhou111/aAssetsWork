namespace SuperMobs.Core
{

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Wrapper template class for services, for optimal setting and getting.
    /// </summary>
    public static class Service
    {
        [ThreadStatic]
        private static List<IServiceWrapper> serviceWrapperList;

        public static void Set<T>(T instance)
        {
            if (ServiceWrapper<T>.instance != null)
            {
                throw new Exception("An instance of this service class has already been set!");
            }

            ServiceWrapper<T>.instance = instance;

            if (serviceWrapperList == null)
            {
                serviceWrapperList = new List<IServiceWrapper>();
            }

            serviceWrapperList.Add(new ServiceWrapper<T>());
        }

        public static T Get<T>()
        {
            return ServiceWrapper<T>.instance;
        }

        public static bool IsSet<T>()
        {
            return ServiceWrapper<T>.instance != null;
        }

        public static void Reset<T>()
        {
            if(serviceWrapperList == null)
            {
                return;
            }

            ServiceWrapper<T>.instance = default(T);
        }

        // Resets references to all services back to null so that they can go out of scope and
        // be subjected to garbage collection.
        // * Services that reference each other will be garbage collected.
        // * AssetBundles should be manually unloaded by an asset manager.
        // * GameObjects will be destroyed by the next level load done by the caller.
        // * Any application statics should be reset by the caller as well.
        // * If there are any unmanaged objects, those need to be released by the caller, too.
        public static void ResetAll()
        {
            if (serviceWrapperList == null)
            {
                return;
            }

            // Unset in the reverse order in which services were set.  Probably doesn't matter.
            for (int i = serviceWrapperList.Count - 1; i >= 0; i--)
            {
                serviceWrapperList[i].Unset();
            }

            serviceWrapperList = null;
        }
    }

    internal class ServiceWrapper<T> : IServiceWrapper
    {
        [ThreadStatic]
        public static T instance = default(T);

        public void Unset()
        {
            ServiceWrapper<T>.instance = default(T);
        }
    }

    internal interface IServiceWrapper
    {
        void Unset();
    }
}
