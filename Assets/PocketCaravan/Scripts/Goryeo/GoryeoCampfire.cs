using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PocketCaravan
{
    /// <summary>Approved or explicit-review campfire: whole frames, three pooled embers and one soft local light.</summary>
    public sealed class GoryeoCampfire : MonoBehaviour
    {
        public const int EmberLimit=3;
        MapRoot map;
        Transform owned;
        Vector3 center;
        SpriteRenderer flame;
        readonly Sprite[] frames=new Sprite[4];
        readonly SpriteRenderer[] embers=new SpriteRenderer[EmberLimit];
        Sprite dot;
        Texture2D dotTexture;
        Light2D glow;
        float elapsed;
        bool approvedForProduction;
        public int FrameIndex { get; private set; }
        public bool Visible { get; private set; }
        public float LightIntensity=>glow!=null&&glow.enabled?glow.intensity:0;
        public Transform Root=>owned;
        public Vector3 Center=>center;
        public int VisibleEmbers { get { int n=0;foreach(var e in embers)if(e!=null&&e.enabled)n++;return n; } }

        public void Configure(MapRoot owner,Vector3 at,bool approved=false)
        {
            if(owned!=null)throw new System.InvalidOperationException("Campfire already configured");
            map=owner;center=at;approvedForProduction=approved;
            owned=new GameObject("Campfire motion review").transform;owned.SetParent(transform,false);
            owned.position=center;
            for(int i=0;i<frames.Length;i++)
            {
                var texture=Resources.Load<Texture2D>("North/flame_"+i);
                if(texture==null)throw new System.InvalidOperationException("Missing original flame frame "+i);
                // Full, unchanged original frame at native PPU; no crop, recolour or nonuniform scale.
                frames[i]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.1f),16);
            }
            flame=MakeRenderer("Whole flame",7);flame.sprite=frames[0];
            flame.transform.position=Snap(center+Vector3.up*.08f);
            dotTexture=new Texture2D(1,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            dotTexture.SetPixel(0,0,Color.white);dotTexture.Apply();
            dot=Sprite.Create(dotTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),16);
            for(int i=0;i<embers.Length;i++){embers[i]=MakeRenderer("Pooled ember "+i,11);embers[i].sprite=dot;}
            glow=owned.gameObject.AddComponent<Light2D>();glow.lightType=Light2D.LightType.Point;
            glow.targetSortingLayers=new[]{SortingLayer.NameToID("Default")};
            glow.color=new Color(1,.68f,.30f);glow.pointLightOuterRadius=2.2f;
            glow.pointLightInnerRadius=.25f;glow.falloffIntensity=.8f;
            Hide();
        }
        SpriteRenderer MakeRenderer(string name,int order)
        {
            var child=new GameObject(name);child.transform.SetParent(owned,false);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sortingOrder=order;return renderer;
        }
        static Vector3 Snap(Vector3 p)=>new Vector3(Mathf.Round(p.x*16)/16,Mathf.Round(p.y*16)/16,p.z);
        bool Allowed=>map!=null&&MapRoot.Current==map&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&GameManager.I!=null&&
            (map.previewOnly?!GameManager.I.AutosaveEnabled:approvedForProduction&&map.mapId=="transit_central_approach");
        bool OnScreen(Camera camera)
        {
            if(camera==null)return false;
            // Orthographic pixel-perfect rendering can retain a previous projection matrix.
            // Use the camera transform, as GoryeoVillageLife does, and include the light's radius.
            if(camera.orthographic)
            {
                var local=camera.transform.InverseTransformPoint(center);
                return local.z>0&&Mathf.Abs(local.x)<=camera.orthographicSize*camera.aspect+2.2f&&
                    Mathf.Abs(local.y)<=camera.orthographicSize+2.2f;
            }
            var point=camera.WorldToViewportPoint(center);
            return point.z>0&&point.x>=-.15f&&point.x<=1.15f&&point.y>=-.15f&&point.y<=1.15f;
        }
        void Update()
        {
            var camera=Camera.main;
            if(!Allowed||!OnScreen(camera)){Hide();return;}
            Visible=true;elapsed+=Time.unscaledDeltaTime;
            FrameIndex=Mathf.FloorToInt(elapsed*7)%frames.Length;
            flame.sprite=frames[FrameIndex];flame.enabled=true;
            for(int i=0;i<embers.Length;i++)
            {
                float age=Mathf.Repeat(elapsed*.55f+i*.37f,1);
                embers[i].transform.position=Snap(center+new Vector3(Mathf.Sin(age*5+i)*.12f,age*.85f+.2f,0));
                embers[i].color=new Color(1,.72f,.32f,(1-age)*.65f);
                embers[i].enabled=age>.08f&&age<.85f;
            }
            float night=1-Mathf.Clamp01(GameManager.I.Clock.Daylight);
            glow.enabled=true;glow.intensity=Mathf.Lerp(.08f,.34f,night)*(1+.075f*Mathf.Sin(elapsed*4.7f)*Mathf.Sin(elapsed*2.9f));
        }
        void Hide()
        {
            Visible=false;if(flame!=null)flame.enabled=false;
            foreach(var ember in embers)if(ember!=null)ember.enabled=false;
            if(glow!=null)glow.enabled=false;
        }
        void OnDisable()=>Hide();
        void OnDestroy()
        {
            foreach(var frame in frames)if(frame!=null)Destroy(frame);
            if(dot!=null)Destroy(dot);if(dotTexture!=null)Destroy(dotTexture);
        }
    }
}
