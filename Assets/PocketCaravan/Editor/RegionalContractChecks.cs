using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    /// <summary>실제 DB/저장을 변경하지 않는 게시판 지역·경로 회귀 검사.</summary>
    public static class RegionalContractChecks
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(); }
        [MenuItem("Pocket Caravan/Tests/게시판 지역·경로 검사")]
        public static void RunMenu() => Debug.Log(JsonUtility.ToJson(Run(), true));
        public static Report Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 종료 후 실행하세요.");
            var assets = new List<UnityEngine.Object>();
            T Temp<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); assets.Add(o); return o; }
            var report = new Report();
            void Check(bool ok, string label) { if (!ok) throw new Exception(label); report.passed.Add(label); }
            try
            {
                var aRegion = Temp<RegionData>(); aRegion.id = "home";
                var bRegion = Temp<RegionData>(); bRegion.id = "foreign";
                var db = Temp<GameDatabase>(); var world = Temp<WorldIndex>();
                TownData Town(string id, RegionData region, string map)
                {
                    var t = Temp<TownData>(); t.id = id; t.region = region; db.towns.Add(t);
                    world.maps.Add(new WorldIndex.MapInfo { mapId = map });
                    world.places.Add(new WorldIndex.Place { kind = "market", id = id, mapId = map }); return t;
                }
                var a = Town("a", aRegion, "a_map"); var b = Town("b", aRegion, "b_map");
                var foreign = Town("foreign", bRegion, "foreign_map"); var isolated = Town("isolated", aRegion, "isolated_map");
                world.Map("a_map").neighbors.Add("b_map"); world.Map("b_map").neighbors.Add("a_map");
                // Foreign is connected but still gated before the first inter-region chapter.
                world.Map("b_map").neighbors.Add("foreign_map"); world.Map("foreign_map").neighbors.Add("b_map");
                var p = new PlayerState(); var clock = new GameClock(); var cm = new ContractManager(db, clock, p, world);
                Check(cm.CanOffer(a, b), "같은 지역 연결된 마을 허용");
                Check(!cm.CanOffer(a, foreign), "연결되어도 관문 전 다른 지역 신규 의뢰 차단");
                Check(!cm.CanOffer(a, isolated), "같은 지역 단절 마을 차단");
                Check(!cm.CanOffer(a, a) && !cm.CanOffer(a, null), "자기 마을/미등록 목적지 차단");
                b.region = null; Check(!cm.CanOffer(a, b), "지역 미지정 차단"); b.region = aRegion;
                var origin = Temp<OriginData>(); origin.homeRegion = bRegion; p.origin = origin;
                Check(cm.CanOffer(a, b), "출신이 달라도 현재 출발 지역 기준");
                cm.InitBoards();
                Check(cm.Available(a.id).Count == ContractManager.BoardSize && cm.Available(a.id).TrueForAll(c => c.toTown == b.id), "신규 생성 목적지 필터");
                Check(cm.Available(foreign.id).Count == 0 && cm.Available(isolated.id).Count == 0, "적합한 목적지 없으면 빈 게시판");
                var c = cm.Available(a.id).Find(v => !v.Premium); Check(c != null, "일반 의뢰 보장 유지");
                int id = c.id; cm.InitBoards(); Check(cm.Available(a.id).Exists(v => v.id == id), "유효한 게시판 재초기화 시 재추첨 없음");
                var invalid = new Contract { id = 500, fromTown = a.id, toTown = foreign.id, weight = 1 };
                cm.BoardOf(a.id).list.Add(invalid);
                Check(!cm.Available(a.id).Contains(invalid) && cm.CanAccept(invalid) != null && !cm.Accept(a.id, invalid), "오래된 잘못된 제안 숨김/수락 차단");
                cm.InitBoards(); Check(!cm.BoardOf(a.id).list.Contains(invalid), "불러온 미수락 제안 정리");
                // One invalid slot is replaced, not the whole board or any accepted cargo.
                Check(cm.Available(a.id).Exists(v => v.id == id), "게시판 정리 시 기존 유효 제안 보존");
                world.Map("a_map").neighbors.Clear();
                Check(cm.Available(a.id).Count == 0 && !cm.Accept(a.id, c), "경로 변경 후 수락 재검사");
                world.Map("a_map").neighbors.Add("b_map");
                Check(cm.Accept(a.id, c), "유효한 의뢰 수락");
                var legacy = new Contract { id = 501, fromTown = a.id, toTown = foreign.id, weight = 1, reward = 100, condition = 1, hours = 10, deadline = 10, finalDeadline = 15 };
                p.contracts.Add(legacy); int active = p.contracts.Count;
                cm.InitBoards(); Check(p.contracts.Count == active && p.contracts.Contains(legacy), "기수락 타 지역 의뢰/짐 보존");
                Check(cm.Deliver(legacy, foreign.id).success, "기수락 타 지역 의뢰 기존 납품 유지");
                var saved = JsonUtility.FromJson<ContractManager.Board>(JsonUtility.ToJson(cm.BoardOf(a.id)));
                cm.boards = new List<ContractManager.Board> { saved }; cm.InitBoards();
                Check(cm.Available(a.id).TrueForAll(v => v.toTown == b.id), "기존 게시판 JSON 형식 호환");
                return report;
            }
            finally { foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset); Notify.Clear(); }
        }
    }
}
