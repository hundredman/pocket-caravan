using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 2장 "석호를 넘어" 장면 연출 (2026-10-29, 북유럽 NorthQuestDirector에 대응, 제안).
    /// 공용 퀘스트·저장 형식은 그대로 두고 GameEvents를 듣기만 한다. 한 번만 보일 연출은 VeniceSave 메모로 표시한다.
    /// - V8 포도주 길: 포도밭에서 포도주를 사면 양조장 사람이 깡총 뛰며 하트, 리알토에 팔면 광장에 음표가 흩날린다.
    /// - V9 큰 장: 퀘스트 중 대상 숙소에 들어서면 장터 위에 깃발 줄이 걸리고, 넷째 물건을 팔면 꽃종이가 터진다.
    /// - V10 유리 장식: 장식을 손에 넣는 순간 고양이 둘레에 별이 반짝인다.
    /// - V11 동문 너머: 동문 큰길에 처음 닿으면 성벽 쪽에 날개 달린 사자 깃발이 올라간다. 2장을 마친 뒤에도 깃발은 그대로 걸려 있다.
    /// </summary>
    public class VeniceQuestDirector : MonoBehaviour
    {
        const string Q8 = "venice_q8_wine", Q9 = "venice_q9_fair", Q10 = "venice_q10_glassart", Q11 = "venice_q11_gate";

        readonly List<(SpriteRenderer r, Vector3 v, float born, float life)> bits = new List<(SpriteRenderer, Vector3, float, float)>();
        Transform flag;
        float flagRaise = -1f;
        bool hadGlassArt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceQuestDirector>() == null)
                GameManager.I.gameObject.AddComponent<VeniceQuestDirector>();
        }

        void OnEnable()
        {
            GameEvents.MapEntered += OnMap;
            GameEvents.ItemBought += OnBought;
            GameEvents.ItemSold += OnSold;
        }

        void OnDisable()
        {
            GameEvents.MapEntered -= OnMap;
            GameEvents.ItemBought -= OnBought;
            GameEvents.ItemSold -= OnSold;
        }

        static QuestManager.Progress Active(string id)
        {
            var gm = GameManager.I;
            if (gm == null || gm.Quests == null) return null;
            foreach (var p in gm.Quests.Active) if (p.quest != null && p.quest.id == id) return p;
            return null;
        }

        static bool Finished(string id)
        {
            var gm = GameManager.I;
            var q = gm != null ? gm.Db.quests.Find(x => x != null && x.id == id) : null;
            return q != null && gm.Quests.IsFinished(q);
        }

        static bool Once(string key)
        {
            if (VeniceSave.Get(key) > 0f) return false;
            VeniceSave.Set(key, 1f);
            return true;
        }

        void OnBought(ItemData item, TownData town, int price)
        {
            if (item == null || town == null || Active(Q8) == null) return;
            if (item.id == "venice_wine" && town.id == "venice_c_vineyard" && GameManager.I.Player.cargo.Count(item) >= 3 && Once("qd_q8_buy"))
            {
                VeniceReactions.NpcCheer("heart");
                Notify.Show("양조장 사람: \"올해 첫 통에서 따른 거요. 흔들리지 않게 조심히!\"", 4f);
            }
        }

        void OnSold(ItemData item, TownData town, int price, int profit)
        {
            if (item == null || town == null) return;
            var player = PlayerController.Instance;
            if (Active(Q8) != null && item.id == "venice_wine" && town.id == "venice_rialto" && Once("qd_q8_sell"))
            {
                Burst("fx_note", player, 8, 1.6f);
                VeniceReactions.NpcCheer("note");
                Sfx.Play("success", .6f);
                Notify.Show("광장 악사가 포도주 노래를 켜기 시작했다♪", 4f);
            }
            var q9 = Active(Q9);
            if (q9 != null && town.id == "venice_c_caravanserai" && q9.Current != null && q9.Current.type == ObjectiveType.Sell
                && q9.amount >= 3 && Once("qd_q9_sold"))
            {
                Burst("confetti", player, 18, 2.2f);
                Sfx.Play("levelup", .5f);
                Notify.Show("대상들이 박수를 친다. 본토 장터에서도 자네 이름이 오르내린다!", 4f);
            }
        }

        void OnMap(string mapId)
        {
            flag = null; flagRaise = -1f;
            if (mapId == "venice_c_post" && Active(Q9) != null) Bunting();
            if (mapId == "venice_c_gate")
            {
                bool first = Active(Q11) != null && Once("qd_q11_flag");
                if (first || Finished(Q11) || VeniceSave.Get("qd_q11_flag") > 0f) Flag(first);
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            // V10: 유리 장식을 손에 넣은 순간
            if (Active(Q10) != null)
            {
                var art = gm.ItemById("venice_glass_art");
                bool has = art != null && gm.Player.cargo.Count(art) > 0;
                if (has && !hadGlassArt && Once("qd_q10_art"))
                {
                    Burst("fx_star", PlayerController.Instance, 10, 1.4f);
                    Sfx.Play("gold", .6f);
                    Notify.Show("유리 장식이 햇빛을 받아 무지갯빛으로 반짝인다.", 4f);
                }
                hadGlassArt = has;
            }
            if (flag != null && flagRaise >= 0f)
            {
                float k = Mathf.Clamp01((Time.time - flagRaise) / 1.6f);
                flag.localPosition = new Vector3(0f, Mathf.Round(Mathf.Lerp(-1f, 0f, k * (2f - k)) * 16f) / 16f, 0f);
                if (k >= 1f) flagRaise = -1f;
            }
            for (int i = bits.Count - 1; i >= 0; i--)
            {
                var (r, v, born, life) = bits[i];
                float t = Time.time - born;
                if (r == null || t > life) { if (r != null) Destroy(r.gameObject); bits.RemoveAt(i); continue; }
                r.transform.position += (v + Vector3.down * t * 1.2f) * Time.deltaTime;
                var c = r.color; c.a = 1f - (t / life) * (t / life); r.color = c;
            }
        }

        void Burst(string sprite, PlayerController around, int n, float life)
        {
            var sp = VeniceUtil.Sprite(sprite);
            if (sp == null || around == null) return;
            for (int i = 0; i < n; i++)
            {
                var r = new GameObject("연출 조각").AddComponent<SpriteRenderer>();
                r.sprite = sp; r.sortingOrder = 24;
                r.color = sprite == "confetti" ? Color.HSVToRGB(Random.value, .55f, 1f) : Color.white;
                r.transform.position = around.transform.position + new Vector3(Random.Range(-.4f, .4f), 1f + Random.Range(0f, .4f), 0f);
                bits.Add((r, new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1.2f, 2.4f), 0f), Time.time, life));
            }
        }

        /// <summary>V9: 대상 숙소 장터 위에 깃발 줄 세 개 (이번 방문 동안만)</summary>
        void Bunting()
        {
            var sp = VeniceUtil.Sprite("bunting");
            var market = FindAnyObjectByType<TownMarket>();
            if (sp == null || market == null) return;
            for (int i = -1; i <= 1; i++)
            {
                var r = new GameObject("큰 장 깃발 줄").AddComponent<SpriteRenderer>();
                r.sprite = sp; r.sortingOrder = 21;
                r.transform.position = market.transform.position + new Vector3(i * 2f, 2.2f + (i == 0 ? .3f : 0f), 0f);
            }
            Notify.Show("대상 숙소 마당에 큰 장 깃발이 걸렸다!", 3.5f);
        }

        /// <summary>V11: 동문 쪽(맵 동쪽 끝 출구 안쪽)에 사자 깃발. first면 천천히 올라간다</summary>
        void Flag(bool first)
        {
            var map = MapRoot.Current;
            var lion = VeniceUtil.Sprite("lion");
            if (map == null || lion == null) return;
            MapExit east = null;
            foreach (var e in FindObjectsByType<MapExit>(FindObjectsSortMode.None))
                if (east == null || e.transform.position.x > east.transform.position.x) east = e;
            if (east == null) return;
            var root = new GameObject("동문 사자 깃발").transform;
            root.position = east.transform.position + new Vector3(-3f, 1.5f, 0f);
            Box(root, new Vector2(1, 30), new Color32(92, 70, 50, 255), new Vector3(-.4f, .1f, 0f), 20);   // 깃대
            flag = new GameObject("깃발").transform; flag.SetParent(root, false);
            Box(flag, new Vector2(14, 12), new Color32(178, 40, 48, 255), new Vector3(0f, .65f, 0f), 21);
            var l = new GameObject("사자").AddComponent<SpriteRenderer>();
            l.sprite = lion; l.sortingOrder = 22; l.color = new Color32(250, 214, 110, 255);
            l.transform.SetParent(flag, false); l.transform.localPosition = new Vector3(0f, .65f, 0f);
            if (first)
            {
                flagRaise = Time.time;
                Sfx.Play("levelup", .6f);
                Notify.Show("동문 성벽에 날개 달린 사자 깃발이 올라갔다!", 4.5f);
            }
        }

        static SpriteRenderer Box(Transform parent, Vector2 px, Color c, Vector3 at, int order)
        {
            var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            t.SetPixel(0, 0, c); t.Apply();
            var r = new GameObject("칸").AddComponent<SpriteRenderer>();
            r.sprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            r.transform.SetParent(parent, false);
            r.transform.localPosition = at;
            r.transform.localScale = new Vector3(px.x / 16f, px.y / 16f, 1f);
            r.sortingOrder = order;
            return r;
        }
    }
}
