using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperMobs.AssetManager.Assets;

namespace SuperMobs.AssetManager.Core
{
	public static class Extension
	{

		#region UI Components

		readonly static Vector2 CENTER_V2 = new Vector2(0.5f, 0.5f);

		public static Sprite AsSprite(this Object obj)
		{
			if (obj is Texture)
			{
				Texture2D tex = obj as Texture2D;
				return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), CENTER_V2);
			}
			return obj as Sprite;
		}

		public static T GetOrAddComponent<T>(this GameObject go) where T : Component
		{
			if (go == null) return null;
			else
			{
				var com = go.GetComponent<T>();
				return com ?? go.AddComponent<T>();
			}
		}

		#endregion

		#region Color

		// Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
		public static string ColorToHex(this Color32 color)
		{
			string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
			return hex;
		}

		public static string ColorToHex(this Color color)
		{
			return ColorUtility.ToHtmlStringRGBA(color);
		}

		public static Color HexToColor(this string hex)
		{
			hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
			hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
			byte a = 255;//assume fully visible unless specified in hex
			byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			//Only use alpha if the string has enough characters
			if (hex.Length == 8)
			{
				a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			}
			return new Color32(r, g, b, a);
		}

		#endregion

		#region IO

		public static byte[] GetBytes(this BinaryWriter bw)
		{
			byte[] bytes = new byte[bw.BaseStream.Length];
			bw.BaseStream.Read(bytes, 0, bytes.Length);
			return bytes;
		}

		public static void WriteArray(this BinaryWriter bw, ICollection<uint> value)
		{
			bw.Write(value != null ? value.Count : 0);
			foreach (var item in value)
			{
				bw.Write(item);
			}
		}

		public static void WriteArray(this BinaryWriter bw, ICollection<int> value)
		{
			bw.Write(value != null ? value.Count : 0);
			foreach (var item in value)
			{
				bw.Write(item);
			}
		}

		public static void WriteArray(this BinaryWriter bw, ICollection<string> value)
		{
			bw.Write(value != null ? value.Count : 0);
			if (value != null)
			{
				foreach (var item in value)
				{
					bw.Write(item);
				}
			}
		}

		public static void WriteArray(this BinaryWriter bw, ICollection<long> value)
		{
			bw.Write(value != null ? value.Count : 0);
			if (value != null)
			{
				foreach (var item in value)
				{
					bw.Write(item);
				}
			}
		}

		public static void WriteArray(this BinaryWriter bw, ICollection<float> value)
		{
			bw.Write(value != null ? value.Count : 0);

			if (value != null)
			{
				foreach (var item in value)
				{
					bw.Write(item);
				}
			}
		}

		// READ
		public static uint[] ReadArrayUint(this BinaryReader br)
		{
			var length = br.ReadInt32();
			var array = new uint[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = br.ReadUInt32();
			}
			return array;
		}

		public static long[] ReadArrayLong(this BinaryReader br)
		{
			var length = br.ReadInt32();
			var array = new long[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = br.ReadInt64();
			}
			return array;
		}

		public static string[] ReadArrayString(this BinaryReader br)
		{
			var length = br.ReadInt32();
			var array = new string[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = br.ReadString();
			}
			return array;
		}

		public static int[] ReadArrayInt(this BinaryReader br)
		{
			var length = br.ReadInt32();
			var array = new int[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = br.ReadInt32();
			}
			return array;
		}

		public static void SaveStreamAssetToFile(this IStreamAsset sa, string path)
		{
			using (var fs = File.Create(path))
			{
				using (var bw = new BinaryWriter(fs))
				{
					sa.ToStream(bw);
					bw.Flush();
				}
			}
		}

		public static void FromStreamBytes(this IStreamAsset sa, byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				using (var br = new BinaryReader(ms))
				{
					sa.FromStream(br);
				}
			}
		}

		#endregion

	}
}

