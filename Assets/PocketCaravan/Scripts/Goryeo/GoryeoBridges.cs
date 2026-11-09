using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/08~09 은샘: 고려 맵에 걸어서 건너는 징검다리를 놓는다 (장단 강 건너 논 = 장승 자리, 장터에서 바로 말고 남쪽으로 돌아서).
    /// 맵을 열 때 돌 그림을 세우고, 건너는 물 칸의 막힘(Blocked 타일)만 지운다. 맵 파일·씬은 바꾸지 않는다.
    /// 자리: Resources/Goryeo/Bridges/Config.json, 그림: Resources/Goryeo/Bridges/Art (Tools/ArtGen/goryeo_stepstones.py).
    /// </summary>
    public sealed class GoryeoBridges : MonoBehaviour
    {
        [Serializable] public class Piece { public string art; public float x, y; }
        [Serializable] public class Bridge { public string mapId; public Piece[] pieces; public int[] walk; }
        [Serializable] public class Config { public int schemaVersion; public string status; public Bridge[] bridges; }

        static Config config;
        static GoryeoBridges instance;
        MapRoot applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { config = null; instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || FindFirstObjectByType<GoryeoBridges>() != null) return;
            var go = new GameObject("Goryeo bridges");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GoryeoBridges>();
        }

        void Update()
        {
            var map = MapRoot.Current;
            if (map != null && map != applied) { applied = map; Install(map); }
        }

        static Config Load()
        {
            if (config != null) return config;
            var text = Resources.Load<TextAsset>("Goryeo/Bridges/Config");
            config = text != null ? JsonUtility.FromJson<Config>(text.text) : new Config();
            return config;
        }

        public static void Install(MapRoot map)
        {
            if (map == null || map.previewOnly) return;
            var cfg = Load();
            if (cfg.bridges == null) return;
            foreach (var b in cfg.bridges)
            {
                if (b == null || b.mapId != map.mapId || map.transform.Find("Goryeo crossing") != null) continue;
                var root = new GameObject("Goryeo crossing").transform;
                root.SetParent(map.transform, false);
                if (b.pieces != null)
                    foreach (var p in b.pieces)
                    {
                        var sprite = p != null ? Resources.Load<Sprite>("Goryeo/Bridges/Art/" + p.art) : null;
                        if (sprite == null) { Debug.LogWarning("[GoryeoBridges] 그림 없음: " + p?.art); continue; }
                        var go = new GameObject(p.art);
                        go.transform.SetParent(root, false);
                        go.transform.position = new Vector3(p.x, p.y, 0f);   // 그림 기준점 = 왼쪽 아래
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = sprite;
                        sr.sortingOrder = 2;   // 땅(0)·꾸밈(1) 위, 사람(그림자 9·몸 11) 아래 → 돌 위를 밟고 지나간다
                    }
                if (b.walk != null && map.blocked != null)
                    for (int i = 0; i + 1 < b.walk.Length; i += 2)
                        map.blocked.SetTile(new Vector3Int(b.walk[i], b.walk[i + 1], 0), null);
            }
        }
    }
}
