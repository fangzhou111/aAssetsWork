namespace SuperMobs.AssetManager.Update
{
   using UnityEngine;
   using System;

   /// <summary>
   /// /// 更新界面UI逻辑绑定器基类
   /// </summary>
   public class UpdateUILogicBase : MonoBehaviour
   {
      /// <summary>
      /// 重置UI界面的初始化
      /// </summary>
      public virtual void Reset()
      {
         throw new Exception("UpdateUILogicBase 请继承该基类,实现接口.");
      }

      /// <summary>
      /// 更新过程中间的文字提示内容
      /// </summary>
      /// <param name="tip">tip</param>
      public virtual void RefreshProcessTip(string tip)
      {
         throw new Exception("UpdateUILogicBase 请继承该基类,实现接口 > " + tip);
      }

      /// <summary>
      /// 刷新进度条的值
      /// </summary>
      /// <param name="val">0.0 - 1.0</param>
      public virtual void SetProcessValue(float val)
      {
         throw new Exception("UpdateUILogicBase 请继承该基类,实现接口 > " + val);
      }

      /// <summary>
      /// 弹出单个按钮提示窗选择 
      /// </summary>
      /// <param name="content"></param>
      /// <param name="confimBtn"></param>
      /// <param name="confim"></param>
      public virtual void PopUpOneBtnTip(string content, string confimBtn, Action confim)
      {
         throw new Exception("UpdateUILogicBase 请继承该基类,实现接口 > " + content);
      }

      /// <summary>
      /// 弹出2个按钮提示窗选择
      /// </summary>
      /// <param name="content"></param>
      /// <param name="cancleBtn"></param>
      /// <param name="confimBtn"></param>
      /// <param name="cancle"></param>
      /// <param name="confim"></param>
      public virtual void PopUpTip(string content, string cancleBtn, string confimBtn, Action cancle, Action confim)
      {
         throw new Exception("UpdateUILogicBase 请继承该基类,实现接口 > " + content);
      }
   }
}