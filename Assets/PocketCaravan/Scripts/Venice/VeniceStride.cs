using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 걷기 느낌 (2026-10-29, 북유럽 썰매·눈 자국에 대응). 보이기·들리기만 하고 이동·충돌·저장은 바꾸지 않는다.
    /// 발밑 바닥 타일 이름으로 바닥을 나눠 한 칸 걸을 때마다:
    /// - 나무 다리·잔교: 통통 울리는 낮은 발소리      - 돌길·광장: 또각또각 높은 발소리
    /// - 모래·젖은 모래: 고양이 발자국이 남았다 사라진다 - 흙길을 달리면 먼지
    /// - 아쿠아 알타로 물 찬 칸: 찰박 소리 + 물결 동그라미
    /// 공용 PlayerController·AudioManager는 고치지 않는다(소리 크기는 AudioManager.Muted만 따른다).
    /// </summary>
    public class VeniceStride : MonoBehaviour
    {
        enum Ground { Other, Wood, Stone, Sand, Dirt, Grass }

        AudioSource src;
        AudioClip step, splash;
        Vector3Int last;
        string lastMap;
        Sprite paw, ring;
        readonly List<(SpriteRenderer r, float born, float life, bool grow)> marks = new List<(SpriteRenderer, float, float, bool)>();
        int count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceStride>() == null)
                GameManager.I.gameObject.AddComponent<VeniceStride>();
        }

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            step = Resources.Load<AudioClip>("Audio/Sfx/step");
            splash = Resources.Load<AudioClip>("Venice/Audio/water");
            paw = Dots(new[] { ".x.x.", "x.x.x", ".xxx.", ".xxx." }, new Color(.35f, .25f, .18f, .55f));
            ring = Dots(new[] { "..xxxx..", ".x....x.", "x......x", ".x....x.", "..xxxx.." }, new Color(.85f, .95f, 1f, .85f));
        }

        static Sprite Dots(string[] rows, Color c)
        {
            int h = rows.Length, w = rows[0].Length;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, h - 1 - y, rows[y][x] == 'x' ? c : Color.clear);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 16f);
        }

        static Ground Classify(string tile)
        {
            if (string.IsNullOrEmpty(tile)) return Ground.Other;
            if (tile.Contains("wood")) return Ground.Wood;
            if (tile.Contains("slab") || tile.Contains("granite") || tile.Contains("brick") || tile.Contains("cobble") || tile.Contains("cap_edge")) return Ground.Stone;
            if (tile.Contains("sand")) return Ground.Sand;
            if (tile.Contains("dirt") || tile.Contains("earth") || tile.Contains("ruts") || tile.Contains("soot")) return Ground.Dirt;
            if (tile.Contains("grass") || tile.Contains("meadow") || tile.Contains("petal")) return Ground.Grass;
            return Ground.Other;
        }

        void Update()
        {
            Fade();
            var gm = GameManager.I; var pc = PlayerController.Instance; var map = MapRoot.Current;
            if (gm == null || pc == null || map == null || !VeniceUtil.InVenice || gm.IsTravelling || UIState.Blocking) return;
            var cell = pc.Motor.Cell;
            if (map.mapId != lastMap) { lastMap = map.mapId; last = cell; Clear(); return; }
            if (cell == last) return;
            last = cell;
            count++;
            var tile = map.ground != null ? map.ground.GetTile(cell) : null;
            var kind = Classify(tile != null ? tile.name : null);
            Vector3 feet = map.CellCenter(cell) + new Vector3(0f, -.35f, 0f);
            bool wet = VeniceEvents.AcquaAlta && VeniceEvents.IsFlooded(cell);
            bool running = pc.Running;
            if (wet)
            {
                Play(splash, .9f + Random.value * .2f, .35f);
                Mark(ring, feet, 0f, .6f, true);
                return;
            }
            switch (kind)
            {
                case Ground.Wood: Play(step, .72f + Random.value * .06f, .32f); break;   // 나무 다리: 낮게 통통
                case Ground.Stone: if (count % 2 == 0) Play(step, 1.25f + Random.value * .1f, .16f); break;   // 돌길: 또각 (두 걸음에 한 번)
                case Ground.Sand:
                    Mark(paw, feet + new Vector3((count % 2 == 0 ? .12f : -.12f), 0f, 0f), 0f, tile != null && tile.name.Contains("wet") ? 1.6f : 3f, false);
                    break;
                case Ground.Dirt:
                    if (running) Mark(VeniceUtil.Sprite("fx_dust"), feet, 0f, .45f, true);
                    break;
            }
        }

        void Play(AudioClip clip, float pitch, float volume)
        {
            if (clip == null || AudioManager.Muted) return;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        void Mark(Sprite sprite, Vector3 at, float rot, float life, bool grow)
        {
            if (sprite == null) return;
            var r = new GameObject("걸음 자국").AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.sortingOrder = 2;
            r.transform.position = at;
            marks.Add((r, Time.time, life, grow));
            if (marks.Count > 40) { if (marks[0].r != null) Destroy(marks[0].r.gameObject); marks.RemoveAt(0); }
        }

        void Fade()
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var (r, born, life, grow) = marks[i];
                float k = (Time.time - born) / life;
                if (r == null || k >= 1f) { if (r != null) Destroy(r.gameObject); marks.RemoveAt(i); continue; }
                var c = r.color; c.a = 1f - k * k; r.color = c;
                if (grow) r.transform.localScale = Vector3.one * (1f + k * .8f);
            }
        }

        void Clear()
        {
            foreach (var m in marks) if (m.r != null) Destroy(m.r.gameObject);
            marks.Clear();
        }
    }
}
