namespace SuperMobs.AssetManager.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.IO;
    using System;
    using System.Reflection;
    using Object = UnityEngine.Object;
    using SuperMobs.AssetManager.Core;

    /*
	 * 计算某个资源的Meta校验码，检测不变的标准
	 * */

    public class MetaEditor
    {
        public static uint GetAssetMetaCrc(string sourcePath)
        {
            // protected the empty path.
            if (string.IsNullOrEmpty(sourcePath))
            {
                return 0;
            }
            else
            {
                // protected import buildcached
                //AssetDatabase.ImportAsset(sourcePath);
                //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            //Debug.Log(">>>>>>>>>>>>>>>>" + sourcePath);
            // 只有某些对象，才需要遍历身上的依赖
            if (sourcePath.EndsWith(".prefab", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".unity", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".controller", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".overrideController", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".mat", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".mp3", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".mp4", StringComparison.Ordinal) ||
                sourcePath.EndsWith(".ogg", StringComparison.Ordinal) ||
                (sourcePath.EndsWith(".asset", StringComparison.Ordinal) && AssetDatabase.LoadAssetAtPath<ScriptableObject>(sourcePath) != null))
            {
                string meta = "";
                string[] dependencies = AssetDatabase.GetDependencies(sourcePath, true);
                for (int i = 0; i < dependencies.Length; i++)
                {
                    string dep = dependencies[i].ToLower();
                    if (dep.EndsWith(".dll", StringComparison.Ordinal) || dep.EndsWith(".cs", StringComparison.Ordinal) /*|| dep.EndsWith(".shader")*/) continue;

                    // fix dont process 'unity default resources'
                    if (AssetDatabase.AssetPathToGUID(dep).StartsWith("0000000", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    meta += CalculateAssetPathCrc(dep);
                }

                if (string.IsNullOrEmpty(meta))
                {
                    Debug.LogError("GetMetaCrc meta is Empty:" + sourcePath);
                    return 0;
                }

                return Crc32.GetStringCRC32(meta);
            }
            else
            {
                return Crc32.GetStringCRC32(CalculateAssetPathCrc(sourcePath));
            }
        }

        // dont care meta file
        private static string CalculateAssetPathCrc(string assetPath)
        {
            // if texture
            //if (assetPath.EndsWith(".png"))
            {
                // should care for meta.
                return ReadMetaFromOneAssetPath(assetPath);
            }

            //return assetPath + "|" + Crc32.GetFileCRC32(assetPath);
        }

        private static string ReadCommonMetaInfo(string assetPath)
        {
            string meta = string.Empty;

            meta += ReadMetaFromOneAssetPath(assetPath);

            string[] depens = AssetDatabase.GetDependencies(new string[] { assetPath });
            for (int i = 0; depens != null && i < depens.Length; i++)
            {
                if (depens[i].Equals(assetPath)) continue;
                meta += ReadMetaFromOneAssetPath(depens[i]);
            }

            return meta;
        }

        private static string ReadMetaFromOneAssetPath(string assetPath)
        {
            return assetPath + "|" + Crc32.GetFileCRC32(assetPath).ToString() + "|" + ReadImporterInfo(assetPath);
        }

        /// <summary>
        /// 输入一个资源路径，计算它meta设置的crc
        /// </summary>
        public static string ReadImporterInfo(string assetPath)
        {
            string meta = string.Empty;
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);

            if (ai == null)
            {
                //throw new Exception("what ReadImporterInfo AssetImporter.GetAtPath(assetPath) is Null = " + assetPath);
                Debug.LogWarning("what ReadImporterInfo AssetImporter.GetAtPath(assetPath) is Null = " + assetPath);
                return "";
            }

            Type type = ai.GetType();
            PropertyInfo[] propertyInfos = type.GetProperties();

            try
            {
                meta = _ReadImporter(propertyInfos, ai);
            }
            catch (Exception e)
            {
                Debug.LogError("_ReadImporter exception :" + assetPath);
                throw e;
            }

            if (string.IsNullOrEmpty(meta))
            {
                Debug.LogWarning("what _ReadImporter return is nullOrEmpty of " + assetPath);
                return string.Empty;
            }

            //if TextureImporter
            if (ai is TextureImporter)
            {
                // get label.
                string[] labs = AssetDatabase.GetLabels(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
                for (int i = 0; labs != null && i < labs.Length; i++)
                {
                    meta += labs[i];
                }

                // get platform setting.
                TextureImporter ti = ai as TextureImporter;

                int maxSize = 0;
                TextureImporterFormat tif;
                int compressQua = 0;
                ti.GetPlatformTextureSettings("Android", out maxSize, out tif, out compressQua);
                meta += maxSize.ToString();
                meta += tif.ToString();
                meta += compressQua.ToString();

                ti.GetPlatformTextureSettings("iPhone", out maxSize, out tif, out compressQua);
                meta += maxSize.ToString();
                meta += tif.ToString();
                meta += compressQua.ToString();

                // spritesheetsetting
                if(ti.spritesheet != null)
                {
                    foreach (var sheet in ti.spritesheet)
                    {
                        meta += _ReadSpriteSheet(sheet);
                    }
                }
            }

            return Crc32.GetStringCRC32(meta).ToString(); ;
        }

        static string _ReadSpriteSheet(SpriteMetaData sheet)
        {
            return sheet.name + sheet.alignment + sheet.border + sheet.pivot + sheet.rect.xMax + sheet.rect.yMax + sheet.rect.center 
                        + sheet.rect.x + sheet.rect.y + sheet.rect.width + sheet.rect.height;
        }

        static string _ReadImporter(PropertyInfo[] propertyInfos, object obj)
        {
            string meta = string.Empty;

            if (propertyInfos == null) return meta;

            //Debug.Log(type.ToString() + " contain propertyInfo count = "+propertyInfos.Length);
            foreach (PropertyInfo pi in propertyInfos)
            {
                object[] attrObs = pi.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                if (attrObs != null && attrObs.Length > 0)
                {
                    continue;
                }

                if (CheckIfIgnorePropertyInMeta(pi.Name))
                {
                    continue;
                }

                if (pi.PropertyType.BaseType.ToString().Equals("System.Array"))
                {
                    Array arr = pi.GetValue(obj, null) as Array;
                    if (arr != null)
                    {
                        string inmeta = string.Empty;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            object arr_val = arr.GetValue(i);
                            PropertyInfo[] arr_infos = arr_val.GetType().GetProperties();
                            if (arr_val.GetType().ToString().Equals("System.String") ||
                                arr_val.GetType().ToString().Equals("System.Enum") ||
                                arr_val.GetType().ToString().Equals("System.ValueType"))
                            {
                                inmeta += arr_val.ToString();
                            }
                            else
                            {
                                //Debug.Log(pi.Name + ">" + pi.PropertyType.ToString() + ">" + pi.PropertyType.BaseType.ToString());
                                //Debug.Log("arr_info.count = " + arr_infos.Length + ",arr_val = " + arr_val.GetType().ToString());

                                inmeta += _ReadImporter(arr_infos, arr_val);
                            }
                        }
                        meta += inmeta;
                    }
                }
                else if (pi.PropertyType.ToString().Equals("System.String") ||
                    pi.PropertyType.BaseType.ToString().Equals("System.Enum") ||
                    pi.PropertyType.BaseType.ToString().Equals("System.ValueType"))
                {
                    if (pi.PropertyType.ToString().Equals("UnityEngine.AnimatorStateInfo"))
                        continue;
                    else if (pi.PropertyType.ToString().Equals("UnityEngine.AnimatorClipInfo"))
                        continue;

                    meta += pi.GetValue(obj, null).ToString();
                }
                else
                {
                    //Debug.Log(pi.Name + ">" + pi.PropertyType.ToString() + ">" + pi.PropertyType.BaseType.ToString());
                }

            }

            return meta;
        }

        static string[] IGNORE_PROPERTY_NAME = new string[] { "assetPath", "assetTimeStamp", "assetBundleName", "assetBundleVariant", "name", "hideFlags", "licenseType", "guid", "timeCreated", "fileFormatVersion" };
        static bool CheckIfIgnorePropertyInMeta(string proName)
        {
            for (int i = 0; i < IGNORE_PROPERTY_NAME.Length; i++)
            {
                if (IGNORE_PROPERTY_NAME[i].Equals(proName)) return true;
            }
            return false;
        }
    }
}
