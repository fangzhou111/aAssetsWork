namespace SuperMobs.AssetManager.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections;

    public class GenBuildMapAttribute : System.Attribute
    {
        public int order;
        public GenBuildMapAttribute(int order)
        {
            this.order = order;
        }
    }

    public interface IGenBuildmap
    {
        bool IsVaild(string assetPath);
        AssetBundleBuild GenAssetBundleBuild(string assetPath);
    }
}