using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 사나 향료 밤시장 연출 (2026-11-07 기우). 언제 열리는지·덤·알림은 공용 NightFestival(날짜%7==6, 19~23시, ownFx=true로 공용 불꽃은 끔).
    /// 여기는 사나 장터 둘레에 등불(빛 번짐)·향로(연기)·좌판·향료 쟁반·향유 병을 세우고, 열려 있는 동안만 둔다.
    /// 그림: Resources/Arabia/Fair (은인 Art/Arabia/Sprites/v1 복사). 장터 ±2~3칸(전시실·행상)·큰 장 좌판 자리와 안 겹치게 바깥쪽.
    /// </summary>
    public class ArabiaNightMarket : MonoBehaviour
    {
        Transform deco;
        readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
        readonly List<(Transform t, float born, float x)> smoke = new List<(Transform, float, float)>();
        Vector3 incenseA, incenseB; float nextPuff;
        static Material unlit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<ArabiaNightMarket>() != null) return;
            var go = new GameObject("ArabiaNightMarket");
            DontDestroyOnLoad(go);
            go.AddComponent<ArabiaNightMarket>();
        }

        static Material Unlit
        {
            get
            {
                if (unlit != null) return unlit;
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                return unlit = sh != null ? new Material(sh) : null;
            }
        }

        void Update()
        {
            bool on = NightFestival.HereNow(out var f) && f.region == Reactions.Region.Arabia && MapRoot.Current != null && !MapRoot.Current.previewOnly;
            if (on && deco == null) Build();
            if (!on && deco != null) { Destroy(deco.gameObject); deco = null; glows.Clear(); smoke.Clear(); }
            if (deco == null) return;
            float now = Time.time;
            // 등불 빛 일렁임
            for (int i = 0; i < glows.Count; i++)
            {
                var g = glows[i]; if (g == null) continue;
                float a = .55f + .15f * Mathf.Sin(now * 2.3f + i * 1.7f);
                g.color = new Color(1f, .78f, .45f, a);
            }
            // 향 연기: 작은 동그라미가 떠오르며 옅어진다
            if (now > nextPuff)
            {
                nextPuff = now + Random.Range(.35f, .6f);
                var at = Random.value < .5f ? incenseA : incenseB;
                var puff = new GameObject("향 연기").transform; puff.SetParent(deco, false);
                puff.position = at + new Vector3(0f, .9f, 0f);
                var sr = puff.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Arabia/Fair/glow"); sr.sortingOrder = 22; sr.color = new Color(.9f, .85f, .8f, .35f);
                puff.localScale = Vector3.one * .35f;
                smoke.Add((puff, now, Random.Range(-.3f, .3f)));
            }
            for (int i = smoke.Count - 1; i >= 0; i--)
            {
                var (tr, born, dx) = smoke[i];
                if (tr == null) { smoke.RemoveAt(i); continue; }
                float age = now - born;
                if (age > 2.2f) { Destroy(tr.gameObject); smoke.RemoveAt(i); continue; }
                tr.position += new Vector3(dx * Time.deltaTime + Mathf.Sin((now + dx) * 3f) * .004f, .35f * Time.deltaTime, 0f);
                tr.localScale = Vector3.one * (.35f + age * .25f);
                var sr = tr.GetComponent<SpriteRenderer>(); if (sr != null) sr.color = new Color(.9f, .85f, .8f, Mathf.Lerp(.35f, 0f, age / 2.2f));
            }
        }

        void Build()
        {
            var market = FindAnyObjectByType<TownMarket>();
            if (market == null) return;
            deco = new GameObject("향료 밤시장").transform;
            var at = market.transform.position;
            SpriteRenderer Put(string sprite, float dx, float dy, int order, float scale = 1f)
            {
                var sp = Resources.Load<Sprite>("Arabia/Fair/" + sprite);
                if (sp == null) { Debug.LogWarning("[아라비아 밤시장] 그림 없음: " + sprite); return null; }
                var go = new GameObject(sprite); go.transform.SetParent(deco, false);
                go.transform.position = at + new Vector3(dx, dy, 0f); go.transform.localScale = Vector3.one * scale;
                var r = go.AddComponent<SpriteRenderer>(); r.sprite = sp; r.sortingOrder = order; return r;
            }
            void Lantern(float dx, float dy)
            {
                Put("prop_lantern", dx, dy, 10);
                var g = Put("glow", dx, dy + .1f, 9, 3.2f);
                if (g != null) { if (Unlit != null) g.sharedMaterial = Unlit; glows.Add(g); }
            }
            // 좌판 둘 (양옆 바깥), 그 앞에 향료 쟁반·향유 병
            Put("building_stall", -8.5f, 1.4f, 8); Put("building_stall", 7f, 1.4f, 8);
            Put("prop_spiceTray", -7.2f, -0.4f, 9); Put("prop_perfume", 7.6f, -0.4f, 9);
            // 등불 넷
            Lantern(-6f, 3.2f); Lantern(6f, 3.2f); Lantern(-9.5f, -1.2f); Lantern(9.2f, -1.2f);
            // 향로 둘 (연기 나는 곳)
            incenseA = at + new Vector3(-2.6f, -2.4f, 0f); incenseB = at + new Vector3(2.6f, -2.4f, 0f);
            Put("prop_incense", -2.6f, -2.4f + .75f, 9); Put("prop_incense", 2.6f, -2.4f + .75f, 9);
            // 바닥 양탄자 (장터 앞)
            Put("prop_carpet", -1.2f, -1.0f, 4); Put("prop_carpet", 1.2f, -1.0f, 4);
        }
    }
}
