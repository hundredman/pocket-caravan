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
            return it.DisplayName;
        }
        void OnEnable(){map=GetComponent<MapRoot>();Sync();}
        void Update()=>Sync();
        void Sync()
        {
            if(!Allowed){Restore();return;}if(GameManager.I==null)return;
            var next=GameManager.I.GetComponent<WorldLabels>();if(next!=original){Restore();original=next;}
            if(original==null)return;if(!ownsOriginal){originalEnabled=original.enabled;ownsOriginal=true;}original.enabled=false;
        }
        void Restore(){if(ownsOriginal&&original!=null)original.enabled=originalEnabled;ownsOriginal=false;}
        void OnDisable(){Restore();DrawnRects.Clear();DrawnNames.Clear();}
        void Name(Vector2 p,string text,Color color,bool place)
        {
            if(string.IsNullOrEmpty(text)||DrawnRects.Count>=48)return;
            float width=Mathf.Min(UIStyle.VirtualWidth-8,Mathf.Ceil(nameStyle.CalcSize(new GUIContent(text)).x)+8);
            var rect=new Rect(Mathf.Round(Mathf.Clamp(p.x-width/2,4,UIStyle.VirtualWidth-width-4)),Mathf.Round(Mathf.Clamp(p.y-8,25,UIStyle.VirtualHeight-58)),width,14);
            for(int shift=0;shift<12;shift++)
            {
                bool overlap=false;foreach(var other in DrawnRects)if(other.Overlaps(rect)){overlap=true;break;}
                if(!overlap)break;rect.y-=15;if(rect.y<25)return;
            }
            foreach(var other in DrawnRects)if(other.Overlaps(rect))return;
            DrawnRects.Add(rect);DrawnNames.Add(text);
            if(place){var old=GUI.color;GUI.color=new Color(.10f,.08f,.13f,.88f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
            Outlined(rect,text,color,nameStyle);
        }
        void OnGUI()
        {
            DrawnRects.Clear();DrawnNames.Clear();if(!Allowed||!ownsOriginal)return;
            var gm = GameManager.I;
            var cam = Camera.main;
            if (gm == null || cam == null || MapRoot.Current == null || UIState.Blocking) return;
            UIStyle.Begin();
            Init();

            var player = PlayerController.Instance;
            var nav = Navigator.Compute(gm);

            // 1) 이름표
            foreach (var it in Interactable.All)
            {
                Vector2 p = ToGui(cam, it.transform.position + new Vector3(0f, 1.0f, 0f));
                if (p.x < -50 || p.y < -20 || p.x > UIStyle.VirtualWidth + 50 || p.y > UIStyle.VirtualHeight + 20) continue;
                bool place=IsPlace(it);string label=LabelFor(it,player!=null?player.Motor.Feet:(Vector2)it.transform.position);
                if (string.IsNullOrEmpty(label)) {if(nav.HasValue&&nav.Value.target==it&&it.Category!=Interactable.Kind.Quest)DrawDown(p+new Vector2(0,-12));continue;}   // 이름 없는 자리(같은 모닥불의 두 번째 의자 등)는 이름표를 띄우지 않는다
                Color c = it.Category switch
                {
                    Interactable.Kind.Quest => QuestColor,
                    Interactable.Kind.Market => MarketColor,
                    Interactable.Kind.Board => BoardColor,
                    Interactable.Kind.Danger => DangerColor,
                    _ => PersonColor,
                };
                bool target = nav.HasValue && nav.Value.target == it;
                Name(p,label,c,place);
                // 퀘스트 NPC는 머리 위 !·? 표시가 있으니, 나머지(장터 등)만 화살표로 짚어 준다
                if (target && it.Category != Interactable.Kind.Quest) DrawDown(p + new Vector2(0, -12));
            }

            if(player!=null)foreach(var place in places)
            {
                var at=map.CellCenter(Vector3Int.zero)+new Vector3(place.x-.5f,place.y-.5f,0);
                if(!Nearby(player.Motor.Feet,at))continue;bool duplicate=false;
                foreach(var it in Interactable.All)if((it.DisplayName==place.text||(place.text=="객주"&&Inn.IsHost(it)))&&!string.IsNullOrEmpty(LabelFor(it,player.Motor.Feet))&&Vector2.Distance(it.transform.position,at)<4){duplicate=true;break;}
                if(duplicate)continue;var p=ToGui(cam,at+Vector3.up);if(p.x<0||p.x>UIStyle.VirtualWidth||p.y<0||p.y>UIStyle.VirtualHeight)continue;
                Name(p,place.text,BoardColor,true);
            }

            // 2) 말 걸기 안내
            if (player != null && player.TalkTarget != null && !UIState.Blocking)
            {
                Vector2 p = ToGui(cam, player.TalkTarget.transform.position + new Vector3(0f, -0.7f, 0f));
                string verb = !string.IsNullOrEmpty(player.TalkTarget.ActionVerb) ? player.TalkTarget.ActionVerb + " (Space)"
                    : player.TalkTarget is DroppedCargo ? "줍기 (Space)" : player.TalkTarget is ContractBoard ? "게시판 보기 (Space)" : player.TalkTarget is TownMarket ? "사고팔기 (Space)" : "말 걸기 (Space)";
                Outlined(new Rect(p.x - 70, p.y, 140, 12), verb, Color.white, nameStyle);
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
            var s = cam.WorldToScreenPoint(world);
            return new Vector2(s.x / UIStyle.Scale, (Screen.height - s.y) / UIStyle.Scale);
        }

        static void Outlined(Rect r, string text, Color c, GUIStyle style)
        {
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            var sr = r; sr.x += 1; sr.y += 1;
            GUI.Label(sr, text, shadowStyle);
            style.normal.textColor = c;
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
            if (nameStyle == null || nameStyle.font != UIStyle.SmallFont)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.SmallFont, alignment = TextAnchor.MiddleCenter, fontSize = UIStyle.SmallSize, clipping = TextClipping.Overflow };
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
