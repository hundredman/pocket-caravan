using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public static class ObstacleChecks
    {
        [Serializable] class Report {public int presets,checks;public List<string> passed=new List<string>();public string scope="로컬 제작 시험장. 서버/저장/전투 검증 아님.";}
        [MenuItem("Pocket Caravan/Tests/장애물 Play 검사")]
        public static string Run()
        {
            if(!EditorApplication.isPlaying)throw new Exception("장애물 시험장 Play에서 실행하세요.");
            var lab=UnityEngine.Object.FindFirstObjectByType<ObstacleLabUI>();if(lab==null)throw new Exception("시험장 없음");
            var field=lab.field;var pc=PlayerController.Instance;var map=MapRoot.Current;var p=GameManager.I.Player;
            var report=new Report{presets=lab.library.definitions.Count};
            void Check(bool value,string label){if(!value)throw new Exception(label);report.passed.Add(label);}
            void Select(string id){lab.Select(lab.library.definitions.FindIndex(d=>d.id==id));pc.Motor.Teleport(map.CellCenter(new Vector3Int(16,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);}
            void Tick(float seconds){for(float t=0;t<seconds;t+=.25f)field.Advance(Mathf.Min(.25f,seconds-t));}
            bool Bypass()
            {
                var start=new Vector3Int(2,20,0);var end=new Vector3Int(38,20,0);var seen=new HashSet<Vector3Int>{start};var queue=new Queue<Vector3Int>();queue.Enqueue(start);
                while(queue.Count>0){var c=queue.Dequeue();if(c==end)return true;foreach(var d in new[]{Vector3Int.left,Vector3Int.right,Vector3Int.up,Vector3Int.down}){var q=c+d;if(!seen.Contains(q) && map.IsWalkable(q) && RoadGrid.Current.GradeAt(q)>0){seen.Add(q);queue.Enqueue(q);}}}return false;
            }
            Check(map.previewOnly && !GameManager.I.SaveNow(),"시험장 저장 차단");
            int money=p.money;var wood=GameManager.I.ItemById("firewood");int cargo=p.cargo.Count(wood);float cost=p.cargo.TotalCost(wood);
            pc.enabled=false;
            try
            {
                foreach(var d in lab.library.definitions)
                {
                    Select(d.id);Check(field.States.Count==1 && field.States[0].definition==d,d.id+" 정의/배치 연결");
                    Check(Bypass(),d.id+" 활성 상태의 우회 교역로");Tick(.1f);
                    Check(p.money==money && p.cargo.Count(wood)==cargo && Mathf.Approximately(p.cargo.TotalCost(wood),cost),d.id+" 접촉만으로 재산 차감 없음");
                }
                Select("fallen_log");Check(!map.IsWalkable(new Vector3Int(19,20,0)),"통나무 실제 차단");
                Check(field.TryWork("trial",pc),"제거 작업 시작");Tick(1);Check(!field.States[0].resolved,"완료 전 길 유지");
                pc.Motor.Teleport(map.CellCenter(new Vector3Int(2,20,0)),Vector2Int.right);Tick(.25f);Check(!field.States[0].resolved,"멀어지면 작업 취소");
                Select("fallen_log");Check(field.TryWork("trial",pc),"제거 재시작");Tick(3.1f);Check(field.States[0].resolved && map.IsWalkable(new Vector3Int(19,20,0)),"완료 후 통행 복원");
                Select("broken_bridge");p.cargo.Clear();p.cargo.Add(wood,1,4);Check(!field.TryWork("trial",pc),"교량 재료 부족 거절");p.cargo.Add(wood,1,4);
                Check(field.TryWork("trial",pc),"교량 수리 시작");Tick(1);Check(p.cargo.Count(wood)==2,"수리 진행 중 재료 보존");
                pc.Motor.Teleport(map.CellCenter(new Vector3Int(2,20,0)),Vector2Int.right);Tick(.25f);Check(p.cargo.Count(wood)==2 && !field.States[0].resolved,"수리 취소 재료/상태 보존");
                Select("broken_bridge");Check(field.TryWork("trial",pc),"교량 수리 재개");Tick(5.1f);Check(field.States[0].resolved && p.cargo.Count(wood)==0,"교량 수리 재료 정확히2개 소비");
                Check(!field.TryWork("trial",pc),"수리 중복 소비 방지");
                Select("toll_gate");p.money=7;Check(!field.TryWork("trial",pc) && p.money==7,"통행료 부족 거절");p.money=100;
                Check(field.TryWork("trial",pc) && p.money==92,"통행료8냥 선택 지불");Check(!field.TryWork("trial",pc) && p.money==92,"통행료 중복 차감 방지");
                Select("toll_gate");field.SetEnabled("trial",false);Check(!field.TryWork("trial",pc) && p.money==92,"비활성 장애물 비용 거절");
                Select("toll_gate");map.previewOnly=false;Check(!field.TryWork("trial",pc) && p.money==92,"본편 저장 연결 전 비용 보호");map.previewOnly=true;
                Select("river_flood");Check(!map.IsWalkable(new Vector3Int(19,20,0)),"범람 활성 차단");Tick(9.1f);Check(map.IsWalkable(new Vector3Int(19,20,0)),"범람 소강 통행");
                pc.Motor.Teleport(map.CellCenter(new Vector3Int(19,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);Tick(6);
                Check(!map.IsWalkable(new Vector3Int(19,20,0)) && map.IsWalkable(new Vector3Int(19,20,0),pc.Motor),"재범람 시 내부 플레이어 탈출 유예");
                pc.Motor.Teleport(map.CellCenter(new Vector3Int(16,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);Tick(.1f);Check(!map.IsWalkable(new Vector3Int(19,20,0),pc.Motor),"탈출 후 재진입 차단");
                Select("narrow_boardwalk");p.vehicle=GameManager.I.Db.vehicles.Find(v=>v.id=="walk");Check(map.IsWalkable(new Vector3Int(19,20,0)),"좁은 목도 등짐 통행");p.vehicle=GameManager.I.Db.vehicles.Find(v=>v.id=="handcart");Check(!map.IsWalkable(new Vector3Int(19,20,0)),"좁은 목도 수레 제한");
                Select("thin_ice");p.cargo.Clear();Check(map.IsWalkable(new Vector3Int(19,20,0)),"얇은 얼음 빈짐 통행");p.cargo.Add(wood,2,4);Check(!map.IsWalkable(new Vector3Int(19,20,0)),"얇은 얼음 적재 제한");
                Select("mud");pc.Motor.Teleport(map.CellCenter(new Vector3Int(19,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);
                foreach(var v in new[]{"walk","handcart"}){p.vehicle=GameManager.I.Db.vehicles.Find(x=>x.id==v);field.enabled=false;float baseline=pc.CurrentSpeed();field.enabled=true;Check(Mathf.Abs(pc.CurrentSpeed()/baseline-(v=="walk"?.8f:.4f))<.001f,"진흙 "+v+" 실제 속도 비율");}
                Select("ice");p.cargo.Clear();p.vehicle=GameManager.I.Db.vehicles.Find(v=>v.id=="walk");pc.Motor.Teleport(map.CellCenter(new Vector3Int(19,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);
                for(int n=0;n<8;n++)pc.Motor.Tick(Vector2.right,4,.1f);pc.Motor.Tick(Vector2.zero,4,.1f);Check(pc.Motor.Velocity.x>1,"빙판 제동 거리 발생");
                Select("ford_current");pc.Motor.Teleport(map.CellCenter(new Vector3Int(19,20,0))-(Vector3)pc.Motor.feetOffset,Vector2Int.right);float y=pc.transform.position.y;pc.Motor.Tick(Vector2.zero,4,.1f);Check(pc.transform.position.y<y-.05f,"여울 물살 실제 밀림");
                Select("bandit");float distance=Vector2.Distance(field.States[0].actor,pc.Motor.Feet);Tick(.5f);Check(Vector2.Distance(field.States[0].actor,pc.Motor.Feet)<distance,"산적 접근 추적");
                Select("water_pirate");var home=field.States[0].actor;Tick(1);Check(field.States[0].actor==home,"수적 정박형 강변 견제");
                Check(!GameManager.I.SaveNow(),"검사 후에도 자동 저장 차단");
                lab.Supply();Select("fallen_log");
            }
            finally{field.enabled=true;map.previewOnly=true;pc.enabled=true;}
            report.checks=report.passed.Count;string json=JsonUtility.ToJson(report,true);Directory.CreateDirectory("output/obstacles");File.WriteAllText("output/obstacles/unity-checks.json",json);return json;
        }
    }
}
