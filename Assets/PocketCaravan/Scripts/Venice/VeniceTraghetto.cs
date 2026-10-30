using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 곤돌라 나루 (2026-10-27, 제안). 석호 다섯 섬의 물가 나루(VeniceLife.json "dock")에서 사공에게 6냥을 내면
    /// 다른 섬으로 바로 건너간다(30분 지남). 걷는 둑길보다 빠르지만 짐이 무거우면(적재 7할 넘음) 태워 주지 않는다 — 손수레 교역은 둑길로.
    /// 리알토 나루에서는 곤돌라 경주(VeniceRace)도 할 수 있다.
    /// 도착 자리는 그 섬의 도착 자리 중 나루·장터에 가장 가까운 곳(VeniceLife.json "spawns"). 이동은 공용 GameManager.TravelTo.
    /// </summary>
    public class VeniceTraghetto : Interactable
    {
        public override string DisplayName => VeniceLabels.Place(this, "곤돌라 나루");   // 가까이 가야 이름표가 뜬다
        public override Kind Category => Kind.Market;
        const int Fare = 6;

        SpriteRenderer boat;
        Vector3 boatHome;

        void Start()
        {
            var r = gameObject.AddComponent<SpriteRenderer>();
            r.sprite = VeniceUtil.Sprite("traghetto");
            r.sortingOrder = 10;
            transform.position += new Vector3(0f, -.5f, 0f);
            var map = MapRoot.Current;
            if (map == null) return;
            var cell = map.WorldToCell(transform.position + new Vector3(0f, .5f, 0f));
            foreach (var d in new[] { Vector3Int.down, Vector3Int.right, Vector3Int.left })
            {
                if (map.IsWalkable(cell + d)) continue;
                boat = new GameObject("나룻 곤돌라").AddComponent<SpriteRenderer>();
                boat.transform.SetParent(transform, true);
                boat.transform.position = map.CellCenter(cell + d) + new Vector3(d.x * .4f, -.1f, 0f);
                boat.sprite = VeniceUtil.Sprite("gondola");
                boat.sortingOrder = 4;
                boatHome = boat.transform.localPosition;
                break;
            }
        }

        void Update()
        {
            if (boat != null) boat.transform.localPosition = boatHome + new Vector3(0f, Mathf.Round(Mathf.Sin(Time.time * 1.8f) * 1.2f) / 16f, 0f);   // 물에 살짝 일렁
        }

        public override void Interact(PlayerController player)
        {
            var gm = GameManager.I;
            if (gm.Player.LoadRatio > .7f)
            {
                DialogueUI.Show("곤돌라 사공", "짐이 너무 많소! 곤돌라가 가라앉겠어. 무거운 짐은 둑길로 끌고 가시오.");
                return;
            }
            var choices = new List<VeniceMenu.Option>();
            foreach (var (map, name) in VeniceDaily.Lagoon)
            {
                if (map == VeniceUtil.Map) continue;
                string m = map;
                choices.Add(new VeniceMenu.Option($"{name}로 건너가기 ({Fare}냥)", () => Go(m)));
            }
            if (VeniceUtil.Map == "venice_rialto")
                choices.Insert(0, new VeniceMenu.Option($"곤돌라 경주 ({VeniceRace.Fee}냥{(VeniceRace.Best > 0 ? $", 최고 {VeniceRace.Best}점" : "")})", VeniceRace.Begin));
            choices.Add(new VeniceMenu.Option("그만", () => { }));
            VeniceMenu.Show("곤돌라 사공", VeniceUtil.Map == "venice_rialto" ? "어디로 모실까? 아니면 운하 경주 한판?" : "어디로 모실까? 둑길로 걷는 것보다 빠르지.", choices.ToArray());
        }

        public static void Go(string map)
        {
            var gm = GameManager.I;
            if (gm.Player.money < Fare) { Notify.Show("뱃삯이 모자란다.", 2.5f); return; }
            var data = VeniceData.Get(map);
            string spawn = BestSpawn(data);
            if (spawn == null) { Notify.Show("그 섬에 댈 나루가 없다.", 2.5f); return; }
            gm.Player.money -= Fare;
            gm.Clock.Advance(.5f);
            Sfx.Play("coin");
            VeniceSave.Set("traghetto_rides", VeniceSave.Get("traghetto_rides") + 1);
            VeniceTravelWave.ByBoat = true;          // 건너는 동안 발자국 대신 곤돌라
            gm.TravelTo(map, spawn);
        }

        /// <summary>도착 자리 중 나루(없으면 장터)에 가장 가까운 것</summary>
        public static string BestSpawn(VeniceData.MapLife data)
        {
            if (data == null || data.spawn_ids == null || data.spawns == null || data.spawn_ids.Length == 0) return null;
            int[] at = data.dock != null && data.dock.Length >= 2 ? data.dock : data.market;
            if (at == null || at.Length < 2) return data.spawn_ids[0];
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < data.spawn_ids.Length && i * 2 + 1 < data.spawns.Length; i++)
            {
                float d = Mathf.Abs(data.spawns[i * 2] - at[0]) + Mathf.Abs(data.spawns[i * 2 + 1] - at[1]);
                if (d < bd) { bd = d; best = i; }
            }
            return data.spawn_ids[best];
        }
    }
}
