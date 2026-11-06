using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace PocketCaravan
{
    /// <summary>A7 approved production or preview visual life. Whole sprites, bounded renderers, no collision or save writes.</summary>
    public sealed class GoryeoVillageLife:MonoBehaviour
    {
        [Serializable]public class Point{public string kind,reason;public float x,y,bx,by;}
        [Serializable]public class Art{public string key;public Sprite sprite;}
        public Point[] points;public Art[] art;public Sprite[] traderFrames;public bool approvedForProduction;
        public const int RendererLimit=64;
        class Item{public Point point;public SpriteRenderer renderer;public float phase;public int puff=-1;public GoryeoVisualWalker walker;public Light2D light;}
        readonly List<Item> items=new List<Item>();Transform owned;MapRoot map;
        public Transform Root=>owned;
        public int RendererCount=>items.Count;
        public int VisibleCount{get{int n=0;foreach(var item in items)if(item.renderer.enabled)n++;return n;}}
        Vector3 At(float x,float y)=>map.CellCenter(Vector3Int.zero)+new Vector3(x-.5f,y-.5f,0);
        Sprite SpriteFor(string key){if(art!=null)foreach(var a in art)if(a.key==key)return a.sprite;return null;}
        public void Configure(Point[] data,Art[] assets,Sprite[] trader){points=data;art=assets;traderFrames=trader;Rebuild();}
        void OnEnable(){if(points!=null)Rebuild();}
        bool Allowed=>map!=null&&MapRoot.Current==map&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&(map.previewOnly||approvedForProduction);
        public void Rebuild()
        {
            Clear();map=GetComponent<MapRoot>();if(!Allowed||points==null)return;
            owned=new GameObject("Goryeo village life").transform;owned.SetParent(transform,false);
            foreach(var point in points)
            {
                int count=point.kind=="smoke"?4:1;
                for(int p=0;p<count&&items.Count<RendererLimit;p++)
                {
                    var go=new GameObject(point.kind);go.transform.SetParent(owned,false);go.transform.position=At(point.x,point.y);
                    var sr=go.AddComponent<SpriteRenderer>();sr.sprite=SpriteFor(point.kind=="chicken"||point.kind=="dog"||point.kind=="bird"||point.kind=="gull"?point.kind+"_0":point.kind);sr.sortingOrder=point.kind=="smoke"||point.kind=="gull"||point.kind=="bird"?22:point.kind=="boat"||point.kind=="lamp"?4:10;
                    var item=new Item{point=point,renderer=sr,phase=items.Count*.73f,puff=point.kind=="smoke"?p:-1};items.Add(item);
                    if(point.kind=="lamp")
                    {
                        var light=go.AddComponent<Light2D>();light.lightType=Light2D.LightType.Point;light.targetSortingLayers=new[]{SortingLayer.NameToID("Default")};light.color=new Color(1,.73f,.38f);light.pointLightOuterRadius=2.6f;light.pointLightInnerRadius=.3f;
                        var glow=go.AddComponent<GoryeoLantern>();glow.lamp=light;glow.phase=item.phase;item.light=light;
                    }
                    if(point.kind=="walker")
                    {
                        var anim=go.AddComponent<CharacterAnimator>();anim.frames=traderFrames;sr.sprite=traderFrames!=null&&traderFrames.Length==16?traderFrames[12]:null;
                        var walker=go.AddComponent<GoryeoVisualWalker>();walker.path=new List<Vector3>{At(point.x,point.y),At(point.bx,point.by)};walker.anim=anim;walker.speed=.7f;walker.blocksMovement=false;item.walker=walker;
                    }
                    sr.enabled=false;
                }
            }
        }
        bool Visible(Vector3 p){var camera=Camera.main;if(camera==null)return false;if(camera.orthographic){var local=camera.transform.InverseTransformPoint(p);return local.z>0&&Mathf.Abs(local.x)<=camera.orthographicSize*camera.aspect&&Mathf.Abs(local.y)<=camera.orthographicSize;}var v=camera.WorldToViewportPoint(p);return v.z>0&&v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;}
        void Update()=>Tick(Time.time);
        public void Tick(float now)
        {
            if(owned==null||!Allowed)return;
            int hour=GameManager.I!=null?GameManager.I.Clock.Hour:12;
            foreach(var item in items)
            {
                var p=item.point;var at=At(p.x,p.y);bool show=true;var sr=item.renderer;sr.color=Color.white;
                switch(p.kind)
                {
                    case"smoke":float age=Mathf.Repeat(now*.32f+item.puff*.25f+item.phase,1);at+=new Vector3(age*.28f,age*1.7f,0);sr.color=new Color(.9f,.88f,.80f,.42f*(1-age));show=(hour>=5&&hour<9)||(hour>=17&&hour<20);break;
                    case"chicken":case"dog":sr.sprite=SpriteFor(p.kind+"_"+((int)(now*.8f+item.phase)%2));show=hour>=5&&hour<21;break;
                    case"bird":case"gull":at+=new Vector3(Mathf.Sin(now*.35f+item.phase)*1.5f,Mathf.Sin(now*.65f+item.phase)*.35f+.35f,0);sr.sprite=SpriteFor(p.kind+"_"+((int)(now*3+item.phase)%2));show=hour>=5&&hour<20;break;
                    case"boat":at=Vector3.Lerp(at,At(p.bx,p.by),.5f+.5f*Mathf.Sin(now*.12f+item.phase));break;
                    case"cart":at=Vector3.Lerp(at,At(p.bx,p.by),Mathf.PingPong(now*.035f+.65f,1));show=hour>=6&&hour<19;break;
                    case"walker":at=item.walker.transform.position;show=hour>=6&&hour<19;break;
                }
                if(item.walker==null){at.x=Mathf.Round(at.x*16)/16;at.y=Mathf.Round(at.y*16)/16;sr.transform.position=at;}show=show&&Visible(at);sr.enabled=show;
                if(item.walker!=null){item.walker.enabled=show;item.walker.anim.enabled=show;}
                if(item.light!=null)item.light.enabled=show;
            }
        }
        void Clear(){if(owned!=null)Destroy(owned.gameObject);owned=null;items.Clear();}
        void OnDisable()=>Clear();
    }
}
