/*
 * @Author: chiuan wei 
 * @Date: 2017-06-14 20:17:07 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-15 00:49:57
 */
using System;
using System.Collections;
using System.Collections.Generic;
using SuperMobs.AssetManager.Core;
using UnityEngine;

[Serializable]
public class SpriteJsonObject : SuperJsonObject
{
   [NonSerialized]
   public Texture2D mainTexture;

   [SerializeField]
   public byte[] textureRawData;

   [SerializeField]
   public int textureWidth;
   [SerializeField]
   public int textureHeight;

   [SerializeField]
   public List<string> names;

   [SerializeField]
   public List<Rect> rects;

   [SerializeField]
   public List<Vector2> pivots;

   private Dictionary<int, Sprite> _cachedSprites = new Dictionary<int, Sprite>();

   private Dictionary<string, int> _optimizeDict = new Dictionary<string, int>();
   private bool _is_optimize = false;

   void Optimize()
   {
      if (_is_optimize)
      {
         return;
      }

      if (names == null)
      {
         return;
      }

      _is_optimize = true;

      for (int i = 0; i < names.Count; i++)
      {
         _optimizeDict[names[i].ToLower()] = i;
      }
   }

   public Sprite GetBySpriteName(string name)
   {
      Optimize();

      int index = 0;
      if (_optimizeDict.TryGetValue(name.ToLower(), out index))
      {
         if (_cachedSprites.ContainsKey(index))
         {
            return _cachedSprites[index];
         }
         else
         {
            var sprite = Sprite.Create(mainTexture, rects[index], Vector2.zero);
            _cachedSprites[index] = sprite;
            return sprite;
         }
      }

      return null;
   }

   /// <summary>
   /// load all sprite too slow if big
   /// </summary>
   /// <returns>all sprites</returns>
   public List<Sprite> LoadAll()
   {
      List<Sprite> list = new List<Sprite>();
      foreach (var n in names)
      {
         list.Add(GetBySpriteName(n));
      }
      return list;
   }

   public new void FromBytes(byte[] input)
   {
      base.FromBytes(input);
      var tex = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
      tex.LoadRawTextureData(textureRawData);
      tex.Apply();
      mainTexture = tex;

      // feel the memory
      textureRawData = null;
   }

   public new void FromJson(string json)
   {
      base.FromJson(json);
      var tex = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
      tex.LoadRawTextureData(textureRawData);
      tex.Apply();
      mainTexture = tex;

      // feel the memory
      textureRawData = null;
   }

}