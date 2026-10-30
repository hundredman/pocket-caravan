using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 시작 화면에서 고르는 네 문명(출신·출발 마을·카드 그림). Resources/StartChoices.asset 한 개.
    /// 메뉴 Pocket Caravan/통합/주인공 그림 + 시작 화면 연결이 만든다. 저장 불러오기 때 출신 id → 출신 데이터 찾기에도 쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Caravan/Start Choices", fileName = "StartChoices")]
    public class StartChoices : ScriptableObject
    {
        [Serializable]
        public class Choice
        {
            public string region;        // "북유럽"
            public OriginData origin;
            public string animal;        // "순록"
            public string strength;      // 한 줄 강점
            public string startScene;    // 출발 마을 씬 이름 (= mapId)
            public string startTown;     // "비르카"
            public string startSpawn = SpawnPoint.StartId;
            public string preview;       // Resources 안 카드 그림 경로 (StartScreen/start_north)
        }

        public List<Choice> choices = new List<Choice>();

        static StartChoices cached;
        public static StartChoices Get() => cached != null ? cached : (cached = Resources.Load<StartChoices>("StartChoices"));

        public static OriginData FindOrigin(string id)
        {
            var all = Get();
            if (all == null || string.IsNullOrEmpty(id)) return null;
            foreach (var c in all.choices) if (c.origin != null && c.origin.id == id) return c.origin;
            return null;
        }
    }
}
