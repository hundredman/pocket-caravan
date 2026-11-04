using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아 하루 생활 (2026-10-27, 제안). 맵에 들어올 때마다 그 맵의 생활 자리를 놓는다(VeniceLife.json, venice_life.py가 만든다).
    /// - 떠돌이 상인: 공용 Peddler (2026-11-05). 날마다 장터 마을 8곳 중 한 곳, 고유 상품은 사자 문장 소문·가면 춤 비법서.
    /// - 벤치 앞 칸에 앉기(VeniceSeat), 석호 나루의 곤돌라 배(VeniceTraghetto). 여관은 2026-11-03부터 공용 Inn(여관 주인에게 말 걸기).
    /// - 하루 처음 베네치아 맵에 들어오면 떠돌이 상인·특가 마을을 알린다.
    /// - (2026-11-04 공용 StoryGuide로 옮김) 다음 이야기 길잡이: 진행 중인 베네치아 퀘스트가 없을 때 다음 퀘스트를 누가 주는지 화면 아래 가운데에 한 줄 (10-30 북유럽 NorthGuide와 같은 자리·모양).
    /// - 벤치 한숨 돌림은 공용 StaminaMods에 배율만 등록한다(푹 쉼 적용은 공용 Inn).
    /// 저장: VeniceSave (deal_n 오늘 산 특가 수, announce 날짜). rested_until은 공용 RestBuff로 옮김.
    /// </summary>
    public class VeniceDaily : MonoBehaviour
    {
        public static readonly string[] MerchantMaps =
            { "venice_rialto", "venice_murano", "venice_c_landing", "venice_burano", "venice_c_post", "venice_chioggia", "venice_c_vineyard", "venice_santerasmo" };
        public static readonly string[] DealItems = { "venice_glass", "venice_salt", "venice_wine", "venice_fruit", "venice_lace", "venice_fish", "venice_glass_art" };
        public static readonly (string map, string name)[] Lagoon =
            { ("venice_rialto", "리알토"), ("venice_murano", "무라노"), ("venice_burano", "부라노"), ("venice_chioggia", "키오자"), ("venice_santerasmo", "산테라스모") };

        static int Day => GameManager.I != null ? GameManager.I.Clock.day : 0;
        public static string MerchantMap => MerchantMaps[(Day * 3 + 1) % MerchantMaps.Length];
        public static string DealItem => DealItems[(Day * 5 + 2) % DealItems.Length];
        public static string DealMap => MerchantMap;      // 특가는 떠돌이 상인이 판다

        /// <summary>공용 Peddler에 베네치아 규칙 등록: 통합 전 순서·특가 후보 그대로, 고유 상품은 사자 문장 소문과 카니발 밤 가면 춤 비법서</summary>
        static void RegisterPeddler()
        {
            Peddler.Schedule[Reactions.Region.Venice] = () => MerchantMap;
            Peddler.DealItems[Reactions.Region.Venice] = DealItems;
            Peddler.Extras[Reactions.Region.Venice] = () =>
            {
                var list = new List<DialogueUI.Choice>();
                if (VeniceAtmosphere.CarnivalNight && !Reactions.Knows("mask"))
                    list.Add(new DialogueUI.Choice("가면 춤 비법서 (60냥) — 카니발 밤 한정", () => Peddler.Pay(60, () =>
                    {
                        string t = Reactions.Teach("mask");
                        Notify.Show(t != null ? $"비법서를 읽고 리액션 '{t}'을(를) 배웠다! (R)" : "이미 아는 동작이다.", 4f);
                    })));
                if (!Collectibles.Complete(Reactions.Region.Venice))
                    list.Add(new DialogueUI.Choice("사자 문장 소문 (30냥)", () =>
                    {
                        for (int i = 0; i < VeniceLegacy.LionMaps.Length; i++)
                        {
                            if (Collectibles.Read(Reactions.Region.Venice, i)) continue;
                            Peddler.Pay(30, () => DialogueUI.Show(Peddler.Name, $"{MapName(VeniceLegacy.LionMaps[i])} 어딘가, 길에서 가장 먼 구석에 이끼 낀 사자가 있다더군."));
                            return;
                        }
                    }));
                return list;
            };
        }

        public static bool Rested => RestBuff.Rested;   // 2026-11-03 공용 여관으로 통합

        Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            RegisterPeddler();
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceDaily>() == null)
                GameManager.I.gameObject.AddComponent<VeniceDaily>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Build(); }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; if (root != null) Destroy(root.gameObject); }
        void OnLoaded(Scene s, LoadSceneMode m) => Build();

        public static string MapName(string id)
        {
            var wi = WorldIndex.Get();
            return wi != null ? wi.MapName(id) : id;
        }

        void Build()
        {
            if (root != null) Destroy(root.gameObject);
            root = null;
            if (!VeniceUtil.InVenice || MapRoot.Current == null) return;
            var data = VeniceData.Get(VeniceUtil.Map);
            if (data == null) return;
            root = new GameObject("베네치아 하루 생활").transform;

            // 떠돌이 상인은 2026-11-05부터 공용 Peddler가 장터 옆에 세운다 (순서·특가 후보는 아래 RegisterPeddler로 그대로)
            foreach (var p in VeniceData.Points(data.seats)) Spot<VeniceSeat>(new[] { p.x, p.y }, "벤치");
            if (data.dock != null && data.dock.Length >= 2) Spot<VeniceTraghetto>(data.dock, "곤돌라 나루");

            // 2026-11-04 키 안내(R·G·F·P)는 공용 Social이, 떠돌이 상인 소식은 공용 Peddler가 띄운다
        }

        T Spot<T>(int[] xy, string name) where T : Interactable
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = VeniceUtil.Cell(xy[0], xy[1]);
            return go.AddComponent<T>();
        }

        void Update()
        {
            // 2026-11-03 체력 소모 배율은 공용 StaminaMods에 등록만 한다 (적용은 공용 Inn). 베네치아 밖에서는 배율을 거둔다.
            StaminaMods.Set("venice_breather", VeniceUtil.InVenice && VeniceSeat.Breather ? .75f : 1f);
        }

        // 2026-11-04 다음 이야기 길잡이는 공용 StoryGuide, "푹 쉼" 표시는 공용 Inn이 한다
    }
}
