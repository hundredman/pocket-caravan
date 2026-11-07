using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// A5 scoped labels: nearby places, uncluttered labels; preserve person, interaction and quest UI.
    /// Own-map opt-in only. Common WorldLabels is restored on disable/scene exit.
    /// </summary>
    public sealed class GoryeoPlaceLabels : MonoBehaviour
    {
        static Texture2D arrowTex, downTex;
        static GUIStyle nameStyle, shadowStyle, guideStyle;

        static readonly Color QuestColor = new Color(1f, 0.85f, 0.25f);
        static readonly Color MarketColor = new Color(0.55f, 0.95f, 0.6f);
        static readonly Color PersonColor = new Color(0.95f, 0.95f, 0.95f);
        static readonly Color BoardColor = new Color(0.62f, 0.85f, 1f);
        static readonly Color DangerColor = new Color(1f, 0.42f, 0.38f);

        [System.Serializable]public class Place{public string text;public float x,y;}
        public Place[] places=new Place[0];public bool approvedForProduction;
        public const float Near=4f;
        public readonly System.Collections.Generic.List<Rect> DrawnRects=new System.Collections.Generic.List<Rect>();
        public readonly System.Collections.Generic.List<string> DrawnNames=new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<Rect> BoardRects=new System.Collections.Generic.List<Rect>();
        public readonly System.Collections.Generic.Dictionary<Interactable,Rect> TargetNameRects=new System.Collections.Generic.Dictionary<Interactable,Rect>();
        public readonly System.Collections.Generic.Dictionary<Interactable,Rect> TargetBodyRects=new System.Collections.Generic.Dictionary<Interactable,Rect>();
        public readonly System.Collections.Generic.Dictionary<Interactable,Rect> QuestBadgeRects=new System.Collections.Generic.Dictionary<Interactable,Rect>();
        public readonly System.Collections.Generic.Dictionary<Interactable,QuestMarker> QuestBadgeStates=new System.Collections.Generic.Dictionary<Interactable,QuestMarker>();
        readonly System.Collections.Generic.Dictionary<SpriteRenderer,bool> oldMarkers=new System.Collections.Generic.Dictionary<SpriteRenderer,bool>();
        public Rect PromptRect=>compactPrompt;
        public Interactable PromptTarget { get; private set; }
        Vector2 guiOrigin;
        Rect compactPrompt;
        MapRoot map;WorldLabels original;bool originalEnabled,ownsOriginal;
        public bool OwnsOriginal=>ownsOriginal;
        public bool OriginalEnabled=>originalEnabled;
        public void Refresh()=>Sync();
        bool Allowed=>map!=null&&MapRoot.Current==map&&GoryeoTravelTransition.RegionOf(map.mapId)!=null&&(map.previewOnly||approvedForProduction);
        public static bool Nearby(Vector2 feet,Vector2 at)=>(feet-at).sqrMagnitude<=Near*Near;
        public static bool IsPlace(Interactable it)=>it is TownMarket||it is ContractBoard||it is WorkshopSpot||Inn.IsHost(it)||(it is TalkNpc&&it.GetComponent<CharacterAnimator>()==null);
        public static string LabelFor(Interactable it,Vector2 feet)
        {
            if(it==null)return "";if(IsPlace(it)&&!Nearby(feet,it.transform.position))return "";
            if(it is TownMarket market&&market.town!=null)return market.town.displayName+" 장터";
            return PlainName(it.DisplayName);
        }
        void OnEnable(){map=GetComponent<MapRoot>();Sync();}
        void Update()=>Sync();
        void Sync()
        {
            if(!Allowed){Restore();return;}if(GameManager.I==null)return;
            var next=GameManager.I.GetComponent<WorldLabels>();if(next!=original){Restore();original=next;}
            if(original==null)return;if(!ownsOriginal){originalEnabled=original.enabled;ownsOriginal=true;}original.enabled=false;
            foreach(var it in Interactable.All)
                if(it is QuestNpc npc && npc.marker!=null)
                {
                    if(!oldMarkers.ContainsKey(npc.marker))oldMarkers.Add(npc.marker,npc.marker.enabled);
                    npc.marker.enabled=false;
                }
        }
        void Restore()
        {
            if(ownsOriginal&&original!=null)original.enabled=originalEnabled;ownsOriginal=false;
            foreach(var pair in oldMarkers)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            oldMarkers.Clear();
        }
        void OnDisable(){Restore();DrawnRects.Clear();DrawnNames.Clear();TargetNameRects.Clear();TargetBodyRects.Clear();QuestBadgeRects.Clear();QuestBadgeStates.Clear();PromptTarget=null;compactPrompt=default;}
        Rect Name(Vector2 p,string text,bool fallbackPlace=false)
        {
            if(string.IsNullOrEmpty(text)||DrawnRects.Count>=48)return default;
            float width=Mathf.Min(UIStyle.VirtualWidth-8,Mathf.Ceil(CuteUI.Width(text,true))+2);
            float height=Mathf.Ceil(nameStyle.CalcSize(new GUIContent(text)).y)+2;
            var rect=new Rect(Mathf.Round(p.x-width/2-guiOrigin.x)+guiOrigin.x,Mathf.Round(p.y-height/2-guiOrigin.y)+guiOrigin.y,width,height);
            if(rect.x<4||rect.xMax>UIStyle.VirtualWidth-4||rect.y<25||rect.yMax>UIStyle.VirtualHeight-44)return default;
            if(fallbackPlace)foreach(var existing in DrawnRects)if(existing.Overlaps(rect))return default;
            DrawnRects.Add(rect);DrawnNames.Add(text);
            var ink=UIStyle.Lin(CuteUI.Text);
            nameStyle.normal.textColor=nameStyle.hover.textColor=nameStyle.active.textColor=nameStyle.focused.textColor=ink;
            CuteUI.WorldText(rect.center.x,rect.center.y,text,true);return rect;
        }
        void OnGUI()
        {
            var renderCamera=Camera.main;
            // 에디터 Scene 뷰의 Screen 크기로 게임 카메라 좌표를 다시 계산하지 않는다.
            if(Application.isEditor && renderCamera!=null && (renderCamera.pixelWidth!=Screen.width||renderCamera.pixelHeight!=Screen.height))return;
            DrawnRects.Clear();DrawnNames.Clear();TargetNameRects.Clear();TargetBodyRects.Clear();QuestBadgeRects.Clear();QuestBadgeStates.Clear();BoardRects.Clear();compactPrompt=default;PromptTarget=null;if(!Allowed||!ownsOriginal)return;
            var gm = GameManager.I;
            var cam = Camera.main;
            if (gm == null || cam == null || MapRoot.Current == null || UIState.Blocking) return;
            UIStyle.Begin();
            Init();
            guiOrigin=ToGui(cam,Vector3.zero);
            CollectBoards(cam);

            var player = PlayerController.Instance;
            var nav = Navigator.Compute(gm);

            var feet=player!=null?player.Motor.Feet:Vector2.zero;
            foreach(var it in Interactable.All)DrawName(cam,it,feet,nav.HasValue&&nav.Value.target==it);

            if(player!=null)foreach(var place in places)
            {
                var at=map.CellCenter(Vector3Int.zero)+new Vector3(place.x-.5f,place.y-.5f,0);
                if(!Nearby(player.Motor.Feet,at))continue;bool duplicate=false;
                foreach(var it in Interactable.All)if((it.DisplayName==place.text||(place.text=="객주"&&Inn.IsHost(it)))&&!string.IsNullOrEmpty(LabelFor(it,player.Motor.Feet))&&Vector2.Distance(it.transform.position,at)<4){duplicate=true;break;}
                if(duplicate)continue;var p=ToGui(cam,at+Vector3.up);if(p.x<0||p.x>UIStyle.VirtualWidth||p.y<0||p.y>UIStyle.VirtualHeight)continue;
                Name(p,place.text,true);
            }

            // 2) 말 걸기 안내: 대상 아래 고정, Space 글자만 표시한다.
            if (player != null && player.TalkTarget != null && !UIState.Blocking)
            {
                PromptTarget=player.TalkTarget;
                compactPrompt=DrawSpace(cam,player.TalkTarget);
            }

            // 3) 퀘스트 길 안내
            if (nav.HasValue && player != null && !UIState.Blocking)
            {
                var n = nav.Value;
                Vector2 from = player.transform.position;
                Vector2 d = (Vector2)n.steer - from;                 // 걸을 수 있는 길의 앞쪽을 가리킨다
                float dist = n.walkCells >= 0 ? n.walkCells : ((Vector2)n.position - from).magnitude;
                if (n.exit) DrawDown(ToGui(cam, n.position + new Vector3(0f, 0.9f, 0f)));
                if (dist > 2.2f && d.sqrMagnitude > 0.01f)
                {
                    Vector2 c = ToGui(cam, player.transform.position);
                    float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;   // GUI는 y가 아래로
                    Vector2 dir = new Vector2(d.x, -d.y).normalized;
                    Vector2 a = c + dir * 26f;
                    var m = GUI.matrix;
                    GUIUtility.RotateAroundPivot(ang, a * UIStyle.Scale);
                    GUI.color = QuestColor;
                    GUI.DrawTexture(new Rect(a.x - 6, a.y - 6, 12, 12), arrowTex);
                    GUI.color = Color.white;
                    GUI.matrix = m;
                }
                string guide = n.exit ? $"{n.label}  ({Mathf.RoundToInt(dist)}칸)" : $"{n.label}  {Mathf.RoundToInt(dist)}칸";
                float gw = 220;
                Outlined(new Rect((UIStyle.VirtualWidth - gw) / 2, UIStyle.VirtualHeight - 40, gw, 14), "목표 · " + guide, QuestColor, guideStyle);
            }
        }

        static Vector2 ToGui(Camera cam, Vector3 world)
        {
            if(cam.orthographic)
            {
                float pixels=Screen.height/(2*cam.orthographicSize*UIStyle.Scale);
                var origin=cam.WorldToScreenPoint(Vector3.zero);
                return new Vector2(Mathf.Round(world.x*pixels)+Mathf.Round(origin.x/UIStyle.Scale),
                    Mathf.Round(-world.y*pixels)+Mathf.Round((Screen.height-origin.y)/UIStyle.Scale));
            }
            var s=cam.WorldToScreenPoint(world);
            return new Vector2(Mathf.Round(s.x/UIStyle.Scale),Mathf.Round((Screen.height-s.y)/UIStyle.Scale));
        }

        public static string PlainName(string text)
        {
            if(string.IsNullOrEmpty(text))return text;
            return text.EndsWith(" !")||text.EndsWith(" ?")?text.Substring(0,text.Length-2):text;
        }
        public static QuestMarker BadgeFor(Interactable it)
        {
            if(it==null)return QuestMarker.None;
            if(it is QuestNpc npc && GameManager.I!=null)return GameManager.I.Quests.MarkerFor(npc.npcId);
            string text=it.DisplayName;
            if(text!=null && text.EndsWith(" !"))return QuestMarker.Available;
            if(text!=null && text.EndsWith(" ?"))return QuestMarker.TurnIn;
            return QuestMarker.None;
        }
        static Bounds BodyBounds(Interactable it)
        {
            if(it is ContractBoard && MapRoot.Current!=null)
            {
                var dressing=MapRoot.Current.GetComponent<VillageDressing>();
                if(dressing!=null)foreach(var space in dressing.spaces)
                    if(space.id=="board"&&space.normal!=null&&space.normal.activeInHierarchy)
                        foreach(var sr in space.normal.GetComponentsInChildren<SpriteRenderer>())
                            if(sr.sprite!=null && Vector2.Distance(sr.bounds.center,it.transform.position)<=3f)return sr.bounds;
            }
            var anim=it.GetComponentInChildren<CharacterAnimator>();
            if(anim!=null && anim.frames!=null && anim.frames.Length>0 && anim.frames[0]!=null)
            {
                var local=anim.frames[0].bounds;var scale=anim.transform.lossyScale;
                return new Bounds(anim.transform.TransformPoint(local.center),Vector3.Scale(local.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))));
            }
            foreach(var sr in it.GetComponentsInChildren<SpriteRenderer>())
                if(sr.sprite!=null && (!(it is QuestNpc npc)||sr!=npc.marker))return sr.bounds;
            return new Bounds(it.transform.position+Vector3.up*.5f,new Vector3(.8f,1f,0));
        }
        static Vector2 NamePoint(Camera cam,Bounds body)
        {
            return ToGui(cam,new Vector3(body.center.x,body.max.y,body.center.z))+new Vector2(0,-9);
        }
        void DrawName(Camera cam,Interactable it,Vector2 feet,bool target)
        {
            var body=BodyBounds(it);var p=NamePoint(cam,body);string label=LabelFor(it,feet);
            var rect=Name(p,label);
            if(rect.height>0)
            {
                TargetNameRects[it]=rect;
                var a=ToGui(cam,body.min);var b=ToGui(cam,body.max);
                TargetBodyRects[it]=Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
            }
            var state=BadgeFor(it);
            if(state!=QuestMarker.None && rect.height>0)
            {
                string symbol=state==QuestMarker.Available?"!":"?";
                var badge=new Rect(rect.xMax+3,rect.center.y-7,12,14);
                if(badge.x>=4&&badge.xMax<=UIStyle.VirtualWidth-4&&badge.y>=25&&badge.yMax<=UIStyle.VirtualHeight-44)
                {
                    var gold=state==QuestMarker.InProgress?CuteUI.KeyLine:CuteUI.Deco;
                    CuteUI.Box(badge,3,CuteUI.Ink,gold,1);
                    float glyphHeight=Mathf.Ceil(nameStyle.CalcSize(new GUIContent(symbol)).y);
                    float glyphX=Mathf.Round(badge.x+(badge.width-CuteUI.Width(symbol,true,true))/2-guiOrigin.x)+guiOrigin.x;
                    CuteUI.Label(glyphX,Mathf.Round(badge.y+(badge.height-glyphHeight)/2-guiOrigin.y)+guiOrigin.y+glyphHeight/2,symbol,gold,true,true);
                    QuestBadgeRects[it]=badge;QuestBadgeStates[it]=state;
                }
            }
            else if(state==QuestMarker.None && target && it.Category!=Interactable.Kind.Quest)DrawDown(p+new Vector2(0,-12));
        }
        void CollectBoards(Camera cam)
        {
            var dressing=map.GetComponent<VillageDressing>();if(dressing==null)return;
            foreach(var space in dressing.spaces)
            {
                if(space.id!="board"||space.normal==null||!space.normal.activeInHierarchy)continue;
                foreach(var sr in space.normal.GetComponentsInChildren<SpriteRenderer>())
                {
                    if(!sr.enabled||sr.sprite==null)continue;
                    var a=ToGui(cam,sr.bounds.min);var b=ToGui(cam,sr.bounds.max);
                    var rect=Rect.MinMaxRect(Mathf.Min(a.x,b.x)-2,Mathf.Min(a.y,b.y)-2,Mathf.Max(a.x,b.x)+2,Mathf.Max(a.y,b.y)+2);
                    BoardRects.Add(rect);
                }
            }
        }
        Rect DrawSpace(Camera cam,Interactable target)
        {
            var body=BodyBounds(target);
            var bottom=ToGui(cam,new Vector3(body.center.x,body.min.y,body.center.z));
            float w=Mathf.Ceil(CuteUI.Width("Space",true))+2;
            var rect=new Rect(Mathf.Round(bottom.x-w/2-guiOrigin.x)+guiOrigin.x,Mathf.Round(bottom.y+3-guiOrigin.y)+guiOrigin.y,w,12);
            if(rect.x<4||rect.xMax>UIStyle.VirtualWidth-4||rect.y<25||rect.yMax>UIStyle.VirtualHeight-44)return default;
            CuteUI.WorldText(rect.center.x,rect.center.y,"Space",true);return rect;
        }
        static void Outlined(Rect r, string text, Color c, GUIStyle style)
        {
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            var sr = r; sr.x += 1; sr.y += 1;
            GUI.Label(sr, text, shadowStyle);
            style.normal.textColor = c;
            style.hover.textColor=style.active.textColor=style.focused.textColor=c;
            shadowStyle.hover.textColor=shadowStyle.active.textColor=shadowStyle.focused.textColor=shadowStyle.normal.textColor;
            GUI.Label(r, text, style);
        }

        static void DrawDown(Vector2 p)
        {
            float bob = Mathf.Floor(Mathf.Sin(Time.time * 5f) * 2f + 2f);
            GUI.color = QuestColor;
            GUI.DrawTexture(new Rect(p.x - 5, p.y - 8 + bob, 10, 8), downTex);
            GUI.color = Color.white;
        }

        static void Init()
        {
            if (arrowTex == null)
            {
                arrowTex = Triangle(12, 12, true);
                downTex = Triangle(10, 8, false);
            }
            if (nameStyle == null || nameStyle.font != UIStyle.SmallFont || nameStyle.fontSize != UIStyle.SmallSize)
            {
                nameStyle = new GUIStyle() { font = UIStyle.SmallFont, alignment = TextAnchor.MiddleCenter, fontSize = UIStyle.SmallSize, clipping = TextClipping.Clip, padding = new RectOffset(0,0,0,0), richText = false };
                shadowStyle = new GUIStyle(nameStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                guideStyle = new GUIStyle(nameStyle) { font = GUI.skin.font, fontSize = UIStyle.BodySize };
            }
        }

        /// <summary>오른쪽(right=true) 또는 아래(false)를 가리키는 흰 삼각형</summary>
        static Texture2D Triangle(int w, int h, bool right)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = right
                    ? Mathf.Abs(y - (h - 1) / 2f) <= (w - 1 - x) * 0.5f
                    : Mathf.Abs(x - (w - 1) / 2f) <= y * 0.6f + 0.5f; // 텍스처 y는 아래부터: 아래가 뾰족
                t.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            t.Apply();
            return t;
        }
    }
}
