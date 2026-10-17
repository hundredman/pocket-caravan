using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>맵별 장애 상태/표현. 기존 타일과 노면 배열을 수정하지 않는다. 기본 상태는 씬 세션 한정.</summary>
    public class ObstacleField : MonoBehaviour
    {
        public ObstacleLibrary library;
        public List<ObstaclePlacement> placements=new List<ObstaclePlacement>();
        [Tooltip("해결 상태 저장/서버 연결 전 본편 재산 차감을 방지. previewOnly 시험장은 별도 허용")]
        public bool allowEconomyCosts;
        public sealed class State
        {
            public ObstaclePlacement placement;public ObstacleDefinition definition;public RectInt area;
            public bool resolved,active,escapeGrace,wasBlocking;public Vector2 actor,home;
            public SpriteRenderer renderer,footprint;public ObstacleHandle handle;
        }
        readonly List<State> states=new List<State>();
        readonly Dictionary<Vector3Int,List<State>> cells=new Dictionary<Vector3Int,List<State>>();
        readonly List<State> threats=new List<State>();
        Transform visualRoot;MapRoot map;RoadGrid road;Sprite footprintSprite;
        State working,focus;PlayerController worker;float progress;
        public float Elapsed { get; private set; }
        public IReadOnlyList<State> States=>states;
        public string Message {get;private set;}="";
        public float Progress=>working!=null?Mathf.Clamp01(progress/Mathf.Max(.01f,working.definition.workSeconds)):0;
        void Start(){Rebuild();}
        public void Replace(ObstaclePlacement[] values)
        {
            placements=new List<ObstaclePlacement>(values ?? Array.Empty<ObstaclePlacement>());Elapsed=0;Rebuild();
        }
        public void Rebuild()
        {
            map=GetComponent<MapRoot>();road=GetComponent<RoadGrid>();
            if(map==null || road==null)return;
            if(library==null)library=Resources.Load<ObstacleLibrary>("Obstacles/ObstacleLibrary");
            if(visualRoot!=null){visualRoot.gameObject.SetActive(false);Destroy(visualRoot.gameObject);}
            states.Clear();cells.Clear();threats.Clear();working=null;focus=null;progress=0;Message="";showDetails=false;
            if(library==null)return;
            visualRoot=new GameObject("Obstacle visuals").transform;visualRoot.SetParent(transform,false);
            var ids=new HashSet<string>();
            foreach(var p in placements)
            {
                if(p==null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id)){Debug.LogError("장애 배치 ID 누락/중복");continue;}
                var d=library.Find(p.definitionId);if(d==null){Debug.LogError("장애 정의 없음: "+p.definitionId);continue;}
                var rect=p.Rect(road.height,d);
                if(rect.width<=0 || rect.height<=0 || rect.xMin<0 || rect.yMin<0 || rect.xMax>road.width || rect.yMax>road.height){Debug.LogError("장애 범위 이탈: "+p.id);continue;}
                var s=new State{placement=p,definition=d,area=rect,active=p.enabled && d.Active(Elapsed,p.phaseOffset)};
                s.home=map.CellCenter(new Vector3Int(rect.x,rect.y,0))+(Vector3)new Vector2((rect.width-1)*.5f,(rect.height-1)*.5f);s.actor=s.home;
                states.Add(s);if(d.mode==ObstacleMode.Threat)threats.Add(s);
                for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
                {var c=new Vector3Int(x,y,0);if(!cells.TryGetValue(c,out var list)){list=new List<State>();cells[c]=list;}list.Add(s);}
                var go=new GameObject(d.displayName);go.transform.SetParent(visualRoot,false);go.transform.position=s.home;
                s.renderer=go.AddComponent<SpriteRenderer>();s.renderer.sortingOrder=12;
                if(footprintSprite==null)footprintSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
                var areaVisual=new GameObject("Declared area");areaVisual.transform.SetParent(visualRoot,false);areaVisual.transform.position=s.home;areaVisual.transform.localScale=new Vector3(rect.width,rect.height,1);
                s.footprint=areaVisual.AddComponent<SpriteRenderer>();s.footprint.sprite=footprintSprite;s.footprint.sortingOrder=3;
                var marker=new GameObject("Action marker");marker.transform.SetParent(visualRoot,false);
                marker.transform.position=map.CellCenter(new Vector3Int(rect.x-1,Mathf.Clamp(rect.y+rect.height/2,0,road.height-1),0));
                s.handle=marker.AddComponent<ObstacleHandle>();s.handle.blocksMovement=false;s.handle.field=this;s.handle.placementId=p.id;
                s.escapeGrace=s.active && OverlapsPlayer(s);RefreshVisual(s);
            }
        }
        public string Label(string id)=>states.Find(s=>s.placement.id==id)?.definition.displayName ?? "장애물";
        bool OverlapsPlayer(State s)
        {
            var p=PlayerController.Instance;if(p==null || MapRoot.Current!=map)return false;
            Vector2 a=p.Motor.Feet-p.Motor.feetSize*.5f,b=p.Motor.Feet+p.Motor.feetSize*.5f;
            var lo=map.WorldToCell(a);var hi=map.WorldToCell(b);
            return lo.x<s.area.xMax && hi.x>=s.area.xMin && lo.y<s.area.yMax && hi.y>=s.area.yMin;
        }
        public bool Blocks(Vector3Int cell,PlayerMotor actor=null)
        {
            if(!enabled || !cells.TryGetValue(cell,out var list))return false;
            var p=GameManager.I!=null?GameManager.I.Player:null;
            foreach(var s in list)
                if(s.active && !s.resolved && s.definition.Blocks(p) && !(s.escapeGrace && PlayerController.Instance!=null && actor==PlayerController.Instance.Motor))return true;
            return false;
        }
        public void Effects(Vector3Int cell,out float speed,out float traction,out Vector2 drift,out float drain,out float visibility)
        {
            speed=1;traction=1;drift=Vector2.zero;drain=0;visibility=1;if(!enabled)return;
            var p=GameManager.I!=null?GameManager.I.Player:null;
            float speedLocal=1,tractionLocal=1,drainLocal=0,visibilityLocal=1;Vector2 driftLocal=Vector2.zero;
            void Include(State s)
            { speedLocal=Mathf.Min(speedLocal,s.definition.Speed(p));tractionLocal=Mathf.Min(tractionLocal,s.definition.traction);driftLocal+=s.definition.drift;drainLocal=Mathf.Max(drainLocal,s.definition.staminaDrain);visibilityLocal=Mathf.Min(visibilityLocal,s.definition.visibility); }
            // Locals rather than out captures keep Mono/C# compatibility.
            if(cells.TryGetValue(cell,out var list))foreach(var s in list)if(s.active && !s.resolved && s.definition.mode!=ObstacleMode.Threat)Include(s);
            Vector2 point=map.CellCenter(cell);
            foreach(var s in threats)if(s.active && !s.resolved && Vector2.Distance(point,s.actor)<1.6f)Include(s);
            speed=Mathf.Clamp(speedLocal,.1f,1);traction=Mathf.Clamp(tractionLocal,.05f,1);drift=Vector2.ClampMagnitude(driftLocal,2);drain=drainLocal;visibility=visibilityLocal;
        }
        void Update()
        {
            if(map==null || MapRoot.Current!=map || UIState.Blocking)return;
            Advance(Time.deltaTime);
        }
        public void Advance(float dt)
        {
            dt=Mathf.Clamp(dt,0,1);Elapsed+=dt;
            foreach(var s in states)
            {
                bool active=s.placement.enabled && !s.resolved && s.definition.Active(Elapsed,s.placement.phaseOffset);
                if(active && !s.active)s.escapeGrace=OverlapsPlayer(s);
                s.active=active;
                bool blocking=s.active && s.definition.Blocks(GameManager.I!=null?GameManager.I.Player:null);
                if(blocking && !s.wasBlocking)s.escapeGrace=OverlapsPlayer(s);
                s.wasBlocking=blocking;
                if(s.escapeGrace && !OverlapsPlayer(s))s.escapeGrace=false;
                if(s.active && s.definition.mode==ObstacleMode.Threat)MoveThreat(s,dt);
                RefreshVisual(s);
            }
            var pc=PlayerController.Instance;
            if(pc!=null && GameManager.I!=null)
            {
                Effects(pc.Motor.Cell,out _,out _,out _,out float drain,out _);
                GameManager.I.Player.stamina=Mathf.Max(0,GameManager.I.Player.stamina-drain*dt);
            }
            if(working!=null)
            {
                if(worker==null || !Near(working,worker) || worker.Motor.IsMoving || (worker==PlayerController.Instance && InputReader.MoveVector().sqrMagnitude>.01f))
                {working=null;progress=0;Message="이동해 작업을 취소했습니다. 재료와 돈은 사용하지 않았습니다.";return;}
                progress+=dt;if(progress>=working.definition.workSeconds)Finish();
            }
        }
        void MoveThreat(State s,float dt)
        {
            var pc=PlayerController.Instance;if(pc==null || s.definition.threatSpeed<=0)return;
            Vector2 target=s.home;
            if(Vector2.Distance(pc.Motor.Feet,s.actor)<s.definition.threatRadius && Vector2.Distance(pc.Motor.Feet,s.home)<s.definition.leash)target=pc.Motor.Feet;
            else target=s.home+new Vector2(Mathf.Sin(Elapsed*.7f),Mathf.Cos(Elapsed*.7f))*.6f;
            Vector2 delta=Vector2.ClampMagnitude(target-s.actor,s.definition.threatSpeed*dt);
            // Bounded direct pursuit with wall checks; not combat/pathfinding AI.
            for(int axis=0;axis<2;axis++)
            {var step=axis==0?new Vector2(delta.x,0):new Vector2(0,delta.y);var candidate=s.actor+step;if(map.IsWalkable(map.WorldToCell(candidate)))s.actor=candidate;}
        }
        void RefreshVisual(State s)
        {
            if(s.renderer==null)return;
            if(s.footprint!=null)
            {
                bool block=s.definition.Blocks(GameManager.I!=null?GameManager.I.Player:null);
                s.footprint.color=block?new Color(.83f,.35f,.18f,s.active?.17f:.02f):new Color(.2f,.55f,.65f,s.active?.12f:.02f);
                s.footprint.enabled=s.definition.mode!=ObstacleMode.Threat;
            }
            var appearance=GetComponent<MapAppearance>();var env=appearance!=null && appearance.environment!=null?appearance.environment.id:"";
            s.renderer.sprite=s.definition.SpriteFor(env);
            if(s.renderer.sprite!=null)
            {
                var size=s.renderer.sprite.bounds.size;
                float w=s.definition.mode==ObstacleMode.Threat?2.2f:s.area.width,h=s.definition.mode==ObstacleMode.Threat?1.8f:s.area.height;
                s.renderer.transform.localScale=new Vector3(w/Mathf.Max(.01f,size.x),h/Mathf.Max(.01f,size.y),1);
            }
            s.renderer.transform.position=s.definition.mode==ObstacleMode.Threat?s.actor:s.home;
            var tint=appearance!=null && appearance.weather!=null?appearance.weather.sceneTint:Color.white;
            tint.a=s.active && !s.resolved?.9f:.16f;s.renderer.color=tint;
        }
        public bool Near(State s,PlayerController p)
        {
            Vector2 lo=map.CellCenter(new Vector3Int(s.area.x,s.area.y,0))-(Vector3)Vector2.one*.5f;
            Vector2 hi=lo+new Vector2(s.area.width,s.area.height);var feet=p.Motor.Feet;
            return Vector2.Distance(feet,new Vector2(Mathf.Clamp(feet.x,lo.x,hi.x),Mathf.Clamp(feet.y,lo.y,hi.y)))<=2.1f;
        }
        public string Denial(State s,PlayerController actor)
        {
            if(!isActiveAndEnabled || !s.placement.enabled)return "비활성 장애물입니다.";
            if(actor==null || GameManager.I==null || MapRoot.Current!=map || !Near(s,actor))return "장애물 가까이에서 작업하세요.";
            if(s.resolved)return "이미 해결했습니다.";
            var d=s.definition;if(d.action==ObstacleAction.None)return "직접 해결할 수 없습니다. 우회하거나 통행 가능 시점을 기다리세요.";
            if((d.money>0 || d.itemCount>0) && !map.previewOnly && !allowEconomyCosts)return "본편 저장/서버 연결 전에는 재산을 사용하는 해결을 잠가 둡니다.";
            var p=GameManager.I.Player;if(p.money<d.money)return "돈이 부족합니다.";
            if(d.itemCount>0){var item=GameManager.I.ItemById(d.itemId);if(item==null || item.questItem || p.cargo.Count(item)<d.itemCount)return "필요 재료: "+(item!=null?item.displayName:"미등록 재료")+" "+d.itemCount+"개";}
            return null;
        }
        public bool TryWork(string id,PlayerController actor)
        {
            var s=states.Find(v=>v.placement.id==id);if(s==null)return false;focus=s;
            if(working!=null){Message="작업 중입니다. 움직이면 취소됩니다.";return false;}
            Message=Denial(s,actor);if(Message!=null)return false;
            showDetails=true;working=s;worker=actor;progress=0;Message="제자리에서 작업 중… 이동하면 취소됩니다.";
            if(s.definition.workSeconds<=0)Finish();return true;
        }
        void Finish()
        {
            var s=working;if(s==null)return;
            Message=Denial(s,worker);if(Message!=null){working=null;progress=0;return;}
            var d=s.definition;var p=GameManager.I.Player;
            if(d.itemCount>0 && !p.cargo.Remove(GameManager.I.ItemById(d.itemId),d.itemCount)){working=null;Message="재료가 변경되어 작업을 취소했습니다.";return;}
            p.money-=d.money;s.resolved=true;s.active=false;s.escapeGrace=false;working=null;progress=0;Message=d.displayName+" 해결 완료";
        }
        public void SetEnabled(string id,bool value)
        {var s=states.Find(v=>v.placement.id==id);if(s==null)return;s.placement.enabled=value;bool active=value && !s.resolved && s.definition.Active(Elapsed,s.placement.phaseOffset);if(active && !s.active)s.escapeGrace=OverlapsPlayer(s);s.active=active;RefreshVisual(s);}
        public void ResetState(){Elapsed=0;Message="장애 상태 초기화";Rebuild();}
        bool showDetails;
        void OnGUI()
        {
            if(map==null || MapRoot.Current!=map || states.Count==0)return;
            UIStyle.Begin();GUI.depth=-24;
            var pc=PlayerController.Instance;
            if(pc!=null)
            {
                Effects(pc.Motor.Cell,out float speed,out _,out _,out _,out float visibility);
                if(visibility<1){GUI.depth=10;var color=GUI.color;GUI.color=new Color(.32f,.37f,.4f,(1-visibility)*.65f);GUI.DrawTexture(new Rect(0,0,UIStyle.VirtualWidth,UIStyle.VirtualHeight),Texture2D.whiteTexture);GUI.color=color;GUI.depth=-24;}
            }
            var s=focus ?? states[0];float w=Mathf.Min(300,UIStyle.VirtualWidth-16);
            GUILayout.BeginArea(new Rect(UIStyle.VirtualWidth-w-8,8,w,showDetails?204:64),UIStyle.Box);
            GUILayout.Label(s.definition.displayName+" · "+(s.resolved?"해결":s.active?"활성":"통행 가능"),UIStyle.Rich);
            if(GUILayout.Button(showDetails?"상세 접기":"상세 · 대응 방법",UIStyle.Button))showDetails=!showDetails;
            if(!showDetails){GUILayout.EndArea();return;}
            string detail=string.IsNullOrWhiteSpace(s.definition.description)?s.definition.category+" · 우회로와 대응 방법을 비교하세요.":s.definition.description;
            GUILayout.Label(detail,UIStyle.Wrap);
            var d=s.definition;
            GUILayout.Label($"등짐×{d.walkSpeed:0.##} / 수레×{d.cartSpeed:0.##} · 비용 {d.money}냥"+(d.itemCount>0?$" · {GameManager.I?.ItemById(d.itemId)?.displayName ?? "재료"} {d.itemCount}개":""),UIStyle.Small);
            if(d.Timed)GUILayout.Label("현재 단계 남은 시간 "+PhaseRemaining(s).ToString("0.0")+"초 · 우회 가능",UIStyle.Small);
            if(working!=null)GUILayout.Label("작업 "+(Progress*100).ToString("0")+"% · 이동하면 취소",UIStyle.Small);
            GUI.enabled=pc!=null && Near(s,pc) && !s.resolved && d.action!=ObstacleAction.None;
            if(GUILayout.Button(d.action==ObstacleAction.Pay?"비용 지불 / Space":d.action==ObstacleAction.Repair?"보수 시작 / Space":"제거 시작 / Space",UIStyle.Button))TryWork(s.placement.id,pc);
            GUI.enabled=true;GUILayout.Label(Message ?? "가까이 가서 Space · 우회로도 비교하세요.",UIStyle.Wrap);
            GUILayout.EndArea();
        }
        void OnDestroy(){if(footprintSprite!=null)Destroy(footprintSprite);}
        public float PhaseRemaining(State s)
        {float cycle=Mathf.Max(.1f,s.definition.activeSeconds+s.definition.openSeconds);float p=Mathf.Repeat(Elapsed+s.placement.phaseOffset,cycle);return s.active?Mathf.Max(0,s.definition.activeSeconds-p):cycle-p;}
    }
}
