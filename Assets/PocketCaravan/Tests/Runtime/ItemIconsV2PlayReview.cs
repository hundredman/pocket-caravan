#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace PocketCaravan.EditorTools
{
    public sealed class ItemIconsV2PlayReview:MonoBehaviour
    {
        const string Folder="Assets/PocketCaravan/Editor/B2Preview/ItemIconsV220261104/",Out="ArtReview/ItemIcons-v2-20261104/Unity";
        [Serializable]class Report{public bool complete;public string error;public List<string> checks=new List<string>();}
        readonly Report report=new Report();readonly List<ItemData> previews=new List<ItemData>();InventoryUI inventory;
        void Check(bool b,string s){if(!b)throw new Exception(s);report.checks.Add(s);}
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool n=false;object v=null;Exception error=null;try{n=stack.Peek().MoveNext();if(n)v=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){Finish(error);yield break;}if(!n){stack.Pop();continue;}if(v is IEnumerator)stack.Push((IEnumerator)v);else yield return v;}Finish(null);
        }
        void Finish(Exception error)
        {
            if(inventory!=null)inventory.Close();if(GameManager.I!=null)GameManager.I.Player.cargo.Clear();foreach(var p in previews)Destroy(p);
            report.complete=error==null;report.error=error==null?"":error.ToString();Directory.CreateDirectory(Out);File.WriteAllText(Out+"/play.json",JsonUtility.ToJson(report,true));Time.timeScale=1;Application.runInBackground=false;EditorApplication.isPlaying=false;
        }
        IEnumerator Photo(string n){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();Check(t.width==960&&t.height==540,"960x540 "+n);File.WriteAllBytes(Out+"/"+n+".png",t.EncodeToPNG());Destroy(t);}
        IEnumerator Run()
        {
            Directory.CreateDirectory(Out);for(int i=0;i<10;i++)yield return null;var gm=GameManager.I;Check(MapRoot.Current.previewOnly&&!gm.AutosaveEnabled,"preview and save isolation");Time.timeScale=0;
            foreach(var g in UnityEngine.Object.FindObjectsByType<StoryGuide>(FindObjectsSortMode.None))g.enabled=false;
            foreach(var d in UnityEngine.Object.FindObjectsByType<DialogueUI>(FindObjectsSortMode.None))typeof(DialogueUI).GetMethod("Close",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);UIState.Blocking=false;Notify.Clear();
            int count=gm.Db.items.Count;var grain=gm.ItemById("grain");var savedIcon=grain.icon;gm.Player.cargo.Clear();
            foreach(var id in new[]{"grain","transit_spice","transit_wool"})
            {
                var p=id=="grain"?Instantiate(grain):ScriptableObject.CreateInstance<ItemData>();p.id=id;p.displayName=id=="grain"?"곡식":id=="transit_spice"?"향신료":"양모";p.weight=id=="grain"?grain.weight:id=="transit_spice"?1:2;
                p.icon=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+id+"-before.png");Check(p.icon!=null&&p.icon.rect.size==new Vector2(16,16),"before native icon "+id);previews.Add(p);gm.Player.cargo.Add(p,1,0);
            }
            var salt=Instantiate(gm.ItemById("salt"));previews.Add(salt);gm.Player.cargo.Add(salt,1,0);
            inventory=UnityEngine.Object.FindFirstObjectByType<InventoryUI>();inventory.Open();Check(inventory.IsOpen,"actual inventory window open");yield return Photo("inventory-before");
            for(int i=0;i<3;i++){var p=previews[i];p.icon=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+p.id+"-after.png");Check(p.icon!=null&&p.icon.rect.size==new Vector2(16,16)&&p.icon.pixelsPerUnit==16,"after native icon "+p.id);}
            yield return Photo("inventory-after");Check(gm.Db.items.Count==count&&grain.icon==savedIcon&&gm.ItemById("transit_spice")==null&&gm.ItemById("transit_wool")==null,"production DB and grain icon unchanged; new goods not registered");Check(!gm.AutosaveEnabled,"saves remain off");
        }
    }
}
#endif
