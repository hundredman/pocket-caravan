using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 2026-11-08 공통화 4단계: 지역 작은 진행(오늘 한 미니게임, 찾은 것 등)을 공용 GameManager 진행 메모에 "접두어+키"로 남기는 도우미.
    /// VeniceSave("venice:")·NorthSave("north:")·아라비아 놀거리("arabia:pastime:")·고려("goryeo:")가 같이 쓴다. 저장 형식(키 이름)은 그대로다.
    /// </summary>
    public sealed class RegionMemo
    {
        readonly string prefix;
        public RegionMemo(string prefix) { this.prefix = prefix ?? ""; }
        public string Key(string key) => prefix + key;
        public float Get(string key, float fallback = 0f) => GameManager.I != null ? GameManager.I.GetMemo(prefix + key, fallback) : fallback;
        public bool Has(string key) => GameManager.I != null && GameManager.I.HasMemo(prefix + key);
        public void Set(string key, float value) { if (GameManager.I != null) GameManager.I.SetMemo(prefix + key, value); }
        public float Add(string key, float delta) { float v = Get(key) + delta; Set(key, v); return v; }
        public void Clear(string key) { if (GameManager.I != null) GameManager.I.ClearMemo(prefix + key); }

        /// <summary>오늘(게임 날짜) 이미 했는지. dayKey에는 날짜가 적힌다.</summary>
        public bool DoneToday(string dayKey) => GameManager.I != null && Mathf.RoundToInt(Get(dayKey, -1f)) == GameManager.I.Clock.day;
        public void MarkToday(string dayKey) { if (GameManager.I != null) Set(dayKey, GameManager.I.Clock.day); }
    }
}
