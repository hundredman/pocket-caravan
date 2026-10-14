using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 추적 중인 퀘스트의 지금 단계가 어디인지 찾는다.
    /// 같은 맵이면 그 사람(또는 장터), 다른 맵이면 그쪽으로 가는 출구를 가리킨다 (WorldIndex로 가장 짧은 길).
    /// </summary>
    public static class Navigator
    {
        public struct Result
        {
            public Vector3 position;
            public string label;
            public bool exit;               // 출구를 가리키는 중
            public Interactable target;     // 같은 맵에 있는 대상
        }

        public static Result? Compute(GameManager gm)
        {
            var map = MapRoot.Current;
            if (map == null) return null;
            var p = gm.Quests.Tracked;
            (string, string)? t = p != null && p.Current != null ? gm.Quests.TargetOf(p.Current) : null;
            // 퀘스트 안내가 없으면 맡은 의뢰의 목적지 게시판으로
            if (!t.HasValue && gm.Player.contracts.Count > 0) t = ("board", gm.Player.contracts[0].toTown);
            if (!t.HasValue) return null;
            var index = WorldIndex.Get();
            var (kind, id) = t.Value;

            string targetMap = kind == "map" ? id : index != null ? index.MapOf(kind, id) : null;
            if (targetMap == null) return null;

            if (targetMap == map.mapId)
            {
                if (kind == "map") return null; // 이미 도착
                foreach (var it in Interactable.All)
                {
                    bool match = (kind == "npc" && it is QuestNpc q && q.npcId == id)
                              || (kind == "market" && it is TownMarket m && m.town != null && m.town.id == id)
                              || (kind == "board" && it is ContractBoard b && b.town == id);
                    if (match) return new Result { position = it.transform.position, label = it.DisplayName, target = it };
                }
                return null;
            }

            string next = index.NextStep(map.mapId, targetMap);
            if (next == null) return null;
            foreach (var e in MapExit.All)
            {
                if (e.targetScene != next) continue;
                var center = e.transform.position + new Vector3(e.size.x * 0.5f, e.size.y * 0.5f, 0f);
                string where = next == targetMap ? index.MapName(targetMap) : $"{index.MapName(next)} 지나 {index.MapName(targetMap)}";
                return new Result { position = center, label = $"{where} 방면", exit = true };
            }
            return null;
        }
    }
}
