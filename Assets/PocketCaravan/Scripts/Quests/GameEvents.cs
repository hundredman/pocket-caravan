using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 게임 안에서 일어난 일을 알리는 곳. 퀘스트·업적·통계는 여기를 구독한다.
    /// 시스템끼리 서로 직접 부르지 않게 하려는 것.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> MapEntered;                          // mapId
        public static event Action<ItemData, TownData, int> ItemBought;          // 물건, 마을, 값
        public static event Action<ItemData, TownData, int, int> ItemSold;       // 물건, 마을, 값, 이익
        public static event Action<VehicleData> VehicleBought;

        /// <summary>
        /// Play를 누를 때마다 정적 상태를 비운다. (도메인 리로드를 끈 설정에서도 이벤트가 쌓이지 않게)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            MapEntered = null; ItemBought = null; ItemSold = null; VehicleBought = null;
            UIState.Blocking = false;
            Notify.Clear();
        }

        public static void RaiseMapEntered(string mapId) => MapEntered?.Invoke(mapId);
        public static void RaiseItemBought(ItemData item, TownData town, int price) => ItemBought?.Invoke(item, town, price);
        public static void RaiseItemSold(ItemData item, TownData town, int price, int profit) => ItemSold?.Invoke(item, town, price, profit);
        public static void RaiseVehicleBought(VehicleData v) => VehicleBought?.Invoke(v);
    }
}
