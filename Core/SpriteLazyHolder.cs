/*
 * @Author: chiuan wei 
 * @Date: 2017-06-14 21:39:00 
 * @Last Modified by:   chiuan wei 
 * @Last Modified time: 2017-06-14 21:39:00 
 */
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 序列化的sprites对象
/// 支持惰性加载某张贴图里面的某个sprite对象(实时构造,加快初始化速度)
/// </summary>
[Serializable]
public class SpriteLazyHolder : ScriptableObject
{
   [NonSerialized]
   public Texture2D mainTexture;

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

}