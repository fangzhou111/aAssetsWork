namespace SuperMobs.AssetManager.Core
{
    using UnityEngine;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using Object = UnityEngine.Object;

    [Serializable]
    public class SuperJsonObject : System.Object
    {
        public string ToJson(bool cool = true)
        {
            return JsonUtility.ToJson(this, cool);
        }

        public byte[] ToBytes()
        {
            string content = ToJson(false);
            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        public void FromBytes(byte[] input)
        {
            FromJson(System.Text.Encoding.UTF8.GetString(input));
        }

        public void FromJson(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }

        public void Save(string path, bool cooljson = true)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, ToJson(cooljson));
        }

        public virtual void FromStream(BinaryReader br)
        {
            // 从二进制流直接读取数据
        }

        public virtual void ToStream(BinaryWriter bw)
        {
            // 直接写入二进制流
        }

    }
}
