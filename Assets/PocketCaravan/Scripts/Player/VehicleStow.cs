using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 11/17 팀 피드백 "운반 수단(낙타·수레·썰매)을 넣었다가 다시 꺼낼 수 있게": V로 넣기 · 꺼내기.
    /// - 넣으면 쓰는 수단이 등짐(walk)이 되고, 산 수단은 PlayerState.stowedVehicle에 남는다 (저장에도 들어간다).
    ///   쓰는 수단(vehicle)만 바꾸므로 짐 한도·속도·그림(손수레·짐말·낙타·썰매)·온라인 모습은 그대로 따라 바뀐다.
    /// - 짐이 등짐 한도보다 많으면 넣을 수 없다 (먼저 팔거나 창고에 맡기기).
    /// - 장터에서 새 수단을 사면 넣어 둔 것은 없어진다 (수단은 하나만 가진다, TownMarket).
    /// </summary>
    public sealed class VehicleStow : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<VehicleStow>() != null) return;
            var go = new GameObject("VehicleStow"); DontDestroyOnLoad(go); go.AddComponent<VehicleStow>();
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Player == null || MapRoot.Current == null || gm.IsTravelling || UIState.Blocking) return;
            if (InputReader.VehicleTogglePressed()) Toggle(gm);
        }

        static VehicleData Walk(GameManager gm) => gm.Db.vehicles.Find(v => v != null && v.id == "walk") ?? gm.Db.startVehicle;
        static bool IsWalk(VehicleData v) => v == null || v.id == "walk";

        public static void Toggle(GameManager gm)
        {
            var p = gm.Player;
            if (!IsWalk(p.vehicle))
            {
                var walk = Walk(gm);
                float cap = p.CapacityOf(walk);
                if (p.TotalWeight > cap + 0.001f)
                {
                    Notify.Show($"짐이 {p.TotalWeight:0.#}이라 {Josa.Eul(p.vehicle.displayName)} 넣을 수 없어요 (등짐은 {cap:0}까지)");
                    Sfx.Play("cancel");
                    return;
                }
                p.stowedVehicle = p.vehicle;
                p.vehicle = walk;
                Notify.Show($"{Josa.Eul(p.stowedVehicle.displayName)} 넣어 두었어요 · V로 다시 꺼내요");
                Sfx.Play("move", .6f);
            }
            else if (p.stowedVehicle != null)
            {
                p.vehicle = p.stowedVehicle;
                p.stowedVehicle = null;
                Notify.Show($"{Josa.Eul(p.vehicle.displayName)} 꺼냈어요");
                Sfx.Play("open", .6f);
            }
            else
            {
                Notify.Show("넣어 둘 운반 수단이 없어요 · 장터에서 살 수 있어요");
                Sfx.Play("cancel");
            }
        }
    }
}
