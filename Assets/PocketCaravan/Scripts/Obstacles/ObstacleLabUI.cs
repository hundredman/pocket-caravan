using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    public class ObstacleLabUI : MonoBehaviour
    {
        public ObstacleLibrary library;
        public ObstacleField field;
        public int selected;
        bool expanded,showList;Vector2 scroll;
        void Start(){Select(selected);Supply();var lab=FindFirstObjectByType<ModuleLabUI>();if(lab!=null)lab.visible=false;}
        public void Select(int value)
        {
            if(library==null || library.definitions.Count==0)return;
            selected=(value%library.definitions.Count+library.definitions.Count)%library.definitions.Count;
            var d=library.definitions[selected];field.library=library;
            field.Replace(new[]{new ObstaclePlacement{id="trial",definitionId=d.id,x=18,y=18}});
            Entrance();
        }
        public void Entrance()
        {
            var p=PlayerController.Instance;if(p==null)return;
            p.Motor.Teleport(MapRoot.Current.CellCenter(new Vector3Int(15,20,0))-(Vector3)p.Motor.feetOffset,Vector2Int.right);
            var follow=FindFirstObjectByType<CameraFollow>();if(follow!=null)follow.SnapNow();
        }
        public void Supply()
        {
            if(MapRoot.Current==null || !MapRoot.Current.previewOnly || GameManager.I==null)return;
            var p=GameManager.I.Player;p.money=100;p.stamina=1;p.cargo.Clear();
            var item=GameManager.I.ItemById("firewood");if(item!=null)p.cargo.Add(item,2,4);
        }
        public void ToggleVehicle()
        {
            if(GameManager.I==null)return;
            var p=GameManager.I.Player;string target=p.vehicle!=null && p.vehicle.id!="walk"?"walk":"handcart";
            var v=GameManager.I.Db.vehicles.Find(x=>x!=null && x.id==target);
            if(v!=null && p.TotalWeight<=v.capacity)p.vehicle=v;
        }
        void OnGUI()
        {
            if(library==null || library.definitions.Count==0)return;
            UIStyle.Begin();GUI.depth=-30;float width=Mathf.Min(270,UIStyle.VirtualWidth-16);
            GUILayout.BeginArea(new Rect(8,8,width,expanded?252:66),UIStyle.Box);
            GUILayout.BeginHorizontal();if(GUILayout.Button("◀",UIStyle.Button,GUILayout.Width(30)))Select(selected-1);
            if(GUILayout.Button($"장애 {selected+1}/{library.definitions.Count} · 목록",UIStyle.Button)){showList=!showList;expanded=true;}
            if(GUILayout.Button("▶",UIStyle.Button,GUILayout.Width(30)))Select(selected+1);GUILayout.EndHorizontal();
            if(GUILayout.Button(expanded?"시험 도구 접기":"시험 도구 펼치기",UIStyle.Button))expanded=!expanded;
            if(!expanded){GUILayout.EndArea();return;}
            if(showList)
            {
                scroll=GUILayout.BeginScrollView(scroll);
                for(int i=0;i<library.definitions.Count;i++)if(GUILayout.Button(library.definitions[i].displayName,UIStyle.Button)){Select(i);showList=false;}
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label(library.definitions[selected].displayName+" · "+library.definitions[selected].category,UIStyle.Rich);
                var p=GameManager.I!=null?GameManager.I.Player:null;
                if(p!=null)GUILayout.Label($"{p.vehicle?.displayName} · 짐 {p.TotalWeight:0.#}/{p.Capacity:0.#} · {p.money}냥 · 체력 {p.stamina*100:0}%",UIStyle.Small);
                GUILayout.BeginHorizontal();if(GUILayout.Button("등짐/수레 전환",UIStyle.Button))ToggleVehicle();if(GUILayout.Button("장애 앞",UIStyle.Button))Entrance();GUILayout.EndHorizontal();
                if(GUILayout.Button("시험짐 초기화·보급 (땔감2·100냥)",UIStyle.Button))Supply();
                if(GUILayout.Button("장애 상태 다시 시작 (비용 복구 없음)",UIStyle.Button))field.ResetState();
                bool on=field.States.Count>0 && field.States[0].placement.enabled;
                if(GUILayout.Button(on?"장애 끄고 비교":"장애 켜고 비교",UIStyle.Button))field.SetEnabled("trial",!on);
                GUILayout.Label("WASD 이동 · Shift 달리기 · Space 대응\n아래쪽 다리는 우회로입니다.\n외형 스킨과 실제 장애 상태는 별도입니다.",UIStyle.Wrap);
            }
            GUILayout.EndArea();
        }
    }
}
