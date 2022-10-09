/*
 * @Author: chiuan wei 
 * @Date: 2017-06-15 11:44:26 
 * @Last Modified by: chiuan wei
 * @Last Modified time: 2017-06-15 12:00:32
 */
namespace SuperMobs.AssetManager.Update
{
   using System;
   using UniRx;
   using UnityEngine.UI;
   using UnityEngine;

   /// <summary>
   /// /// UGUI的更新界面逻辑
   /// </summary>
   public class UpdateUGUILogic : UpdateUILogicBase
   {
      // update ui msg components
      public Text lb_tip = null;
      public Text lb_process = null;
      public Slider pb_process = null;

      // tip object child path
      public GameObject tipPrefab = null;
      public string tipContentText = "lb_tip";
      public string tipConfimBtnText = "btn_confim/Text";
      public string tipCancleBtnText = "btn_cancle/Text";
      public string tipConfimBtn = "btn_confim";
      public string tipCancleBtn = "btn_cancle";

      public override void Reset()
      {
         if (pb_process != null)
            pb_process.value = 0f;
         if (lb_tip != null)
            lb_tip.text = "";
         if (lb_process != null)
            lb_process.text = "";
      }

      public override void RefreshProcessTip(string tip)
      {
         if (lb_tip != null)
            lb_tip.text = tip;
      }

      public override void SetProcessValue(float val)
      {
         if (pb_process != null)
         {
            pb_process.value = Mathf.Clamp01(val);
         }
      }

      public override void PopUpOneBtnTip(string content, string confimBtn, Action confim)
      {
         if (tipPrefab == null)
         {
            throw new Exception("不能弹出提示窗,tipPrefab为空!");
         }
         GameObject tip = GameObject.Instantiate(tipPrefab) as GameObject;
         Text lbcontent = tip.transform.Find(tipContentText).GetComponent<Text>();
         lbcontent.text = content;

         GameObject confimGo = tip.transform.Find(tipConfimBtn).gameObject;
         tip.transform.Find(tipConfimBtnText).GetComponent<Text>().text = confimBtn;
         tip.transform.Find(tipCancleBtn).gameObject.SetActive(false);

         // set center position
         confimGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, confimGo.GetComponent<RectTransform>().anchoredPosition.y);

         confimGo.GetComponent<Button>().OnClickAsObservable()
            .Do(_ =>
            {
               if (confim != null)
                  confim();
               Destroy(tip);
            })
            .Subscribe()
            .AddTo(tip);
      }

      public override void PopUpTip(string content, string cancleBtn, string confimBtn, Action cancle, Action confim)
      {
         if (tipPrefab == null)
         {
            throw new Exception("不能弹出提示窗,tipPrefab为空!");
         }
         GameObject tip = GameObject.Instantiate(tipPrefab) as GameObject;
         Text lbcontent = tip.transform.Find(tipContentText).GetComponent<Text>();
         lbcontent.text = content;

         tip.transform.Find(tipConfimBtnText).GetComponent<Text>().text = confimBtn;
         tip.transform.Find(tipCancleBtnText).GetComponent<Text>().text = cancleBtn;

         var confimGo = tip.transform.Find(tipConfimBtn).gameObject;
         var cancleGo = tip.transform.Find(tipCancleBtn).gameObject;

         confimGo.GetComponent<Button>().OnClickAsObservable()
            .Do(_ =>
            {
               if (confim != null) confim();
               Destroy(tip);
            })
            .Subscribe()
            .AddTo(tip);

         cancleGo.GetComponent<Button>().OnClickAsObservable()
            .Do(_ =>
            {
               if (cancle != null) cancle();
               Destroy(tip);
            })
            .Subscribe()
            .AddTo(tip);
      }
   }
}