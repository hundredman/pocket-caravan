#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public sealed class A7dUIPlayRunner:MonoBehaviour
    {
        public static string Output;public static Action<int,int> Resize;public static Action<Exception> Complete;
        static readonly BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Fields).SetValue(target,value);
        IEnumerator Start()
        {
            var run=Run();while(true){bool more=false;Exception error=null;try{more=run.MoveNext();}catch(Exception e){error=e;}if(error!=null){Complete(error);yield break;}if(!more){Complete(null);yield break;}yield return run.Current;}
        }
        IEnumerator Capture(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(Output);for(int i=0;i<15;i++)yield return null;
            if(!MapRoot.Current.previewOnly||GameManager.I.AutosaveEnabled)throw new Exception("Unsafe UI test session");
            Time.timeScale=0;PlayerController.Instance.enabled=false;
            var map=FindFirstObjectByType<WorldMapUI>();var market=FindFirstObjectByType<TownMarket>();
            foreach(var size in new[]{new Vector2Int(480,270),new Vector2Int(960,540),new Vector2Int(1280,720)})
            {
                Resize(size.x,size.y);for(int i=0;i<5;i++)yield return null;Notify.Clear();
                if(Screen.width!=size.x||Screen.height!=size.y)throw new Exception("Wrong capture resolution");
                if(UIStyle.Scale<1||UIStyle.Scale!=Mathf.Floor(UIStyle.Scale)||UIStyle.BodySize!=12||UIStyle.SmallSize!=10)
                    throw new Exception("Goryeo pixel UI scale/font size regression");
                var panel=WorldMapUI.PanelRect(UIStyle.VirtualWidth,UIStyle.VirtualHeight);
                if(panel.xMin<0||panel.yMin<0||panel.xMax>UIStyle.VirtualWidth||panel.yMax>UIStyle.VirtualHeight)
                    throw new Exception("Map panel clipped");
                string original=MapRoot.Current.mapId;
                try{MapRoot.Current.mapId="venice_city";if(UIStyle.VirtualHeight!=360||!Mathf.Approximately(UIStyle.Scale,Screen.height/360f))throw new Exception("Other region UI scaling changed");}
                finally{MapRoot.Current.mapId=original;}
                string prefix=size.x+"x"+size.y;
                yield return Capture(prefix+"-hud");
                typeof(HUD).GetField("expandedUntil",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,Time.unscaledTime+90);
                yield return Capture(prefix+"-status");
                typeof(HUD).GetField("expandedUntil",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,-1f);
                Set(map,"open",true);Set(map,"regional",false);UIState.Blocking=true;
                yield return Capture(prefix+"-map");Set(map,"regional",true);yield return Capture(prefix+"-regional-map");
                Set(map,"open",false);UIState.Release();
                market.Interact(PlayerController.Instance);yield return Capture(prefix+"-market");
                Set(market,"contentScroll",new Vector2(0,10000));yield return Capture(prefix+"-market-scrolled");
                market.GetType().GetMethod("Close",Fields|BindingFlags.Public).Invoke(market,null);UIState.Release();
                TownFacilitiesUI.Show(market.town);yield return Capture(prefix+"-facilities");
                var facilities=FindFirstObjectByType<TownFacilitiesUI>();
                facilities.GetType().GetMethod("Close",Fields).Invoke(facilities,null);UIState.Release();
            }
            if(GameManager.I.AutosaveEnabled)throw new Exception("Autosave was enabled");
        }
    }
}
#endif
