/*
 * @Author: chiuan wei 
 * @Date: 2017-06-14 20:17:12 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-15 00:51:23
 */
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SuperMobs.Core;

/// <summary>
/// 负责把一个Sprite的贴图转变成SpriteJsonObject
/// </summary>
public class SpriteJsonObjectEditor
{
   static SpriteLazyHolder ConvertSpriteTextureToSpriteLazyHolder(Texture2D texture)
   {
      var path = AssetDatabase.GetAssetPath(texture);
      if (string.IsNullOrEmpty(path))
      {
         Debug.LogError("cant convert this texture,havnt assetPath.maybe runtime texture not support.");
         return null;
      }

      var sps = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

      if (sps.Length <= 0)
      {
         Debug.LogError("cant convert this texture, no sprite under this texture.");
         return null;
      }

      var lazy = ScriptableObject.CreateInstance<SpriteLazyHolder>();
      lazy.mainTexture = texture;
      lazy.names = new List<string>();
      lazy.pivots = new List<Vector2>();
      lazy.rects = new List<Rect>();

      foreach (var sp in sps)
      {
         lazy.names.Add(sp.name);
         lazy.rects.Add(sp.rect);
         lazy.pivots.Add(sp.pivot);
      }

      return lazy;
   }

   [MenuItem("Assets/AssetManager/Texture/Convert SpriteTexture To LazyObject", false, int.MaxValue - 10001)]
   static void ConvertSpriteTextureToLazy()
   {
      var obj = Selection.activeObject;
      if (obj is Texture2D)
      {
         var texture = obj as Texture2D;
         var holder = ConvertSpriteTextureToSpriteLazyHolder(texture);
         if (holder != null)
         {
            var path = AssetDatabase.GetAssetPath(obj);
            AssetDatabase.CreateAsset(holder, path + ".asset");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
         }
      }
      else
      {
         Debug.LogError("cant convert this texture to sprite lazy holder,not Texture2D but " + obj.GetType().ToString());
      }
   }

   [MenuItem("Assets/AssetManager/Texture/Convert SpriteTexture To JsonObject", false, int.MaxValue - 10000)]
   static void ConvertSpriteTextureToObject()
   {
      var obj = Selection.activeObject;
      if (obj is Texture2D)
      {
         var texture = obj as Texture2D;
         var path = AssetDatabase.GetAssetPath(obj);
         var sps = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
         var holder = ConvertSpriteTextureToSpriteLazyHolder(texture);
         if (holder == null)
         {
            return;
         }

         var spj = new SpriteJsonObject();
         spj.textureRawData = texture.GetRawTextureData();
         spj.textureWidth = texture.width;
         spj.textureHeight = texture.height;
         spj.names = holder.names;
         spj.rects = holder.rects;
         spj.pivots = holder.pivots;
         File.WriteAllBytes(path + ".bytes", spj.ToBytes());
         AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
      }
   }

  //  [MenuItem("Assets/AssetManager/Convert SpriteTexture To RawDataFile", false, int.MaxValue - 10000)]
  //  static void ConvertSpriteTextureToObjectRaw()
  //  {
  //     var obj = Selection.activeObject;
  //     if (obj is Texture2D)
  //     {
  //        var texture = obj as Texture2D;
  //        var path = AssetDatabase.GetAssetPath(obj);
  //        File.WriteAllBytes(path + ".bytes", texture.GetRawTextureData());
  //        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  //     }
  //  }

   [MenuItem("Assets/AssetManager/Texture/Convert SpriteTexture To JsonObject", true)]
   [MenuItem("Assets/AssetManager/Texture/Convert SpriteTexture To LazyObject", true)]
  //  [MenuItem("Assets/AssetManager/Convert SpriteTexture To RawDataFile",true)]
   static bool ValidateConvertSpriteTextureToObject()
   {
      var obj = Selection.activeObject;
      if (obj is Texture2D)
      {
         var path = AssetDatabase.GetAssetPath(obj);
         var sps = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
         return sps.Length > 0;
      }

      return false;
   }
}