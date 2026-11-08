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
        public static event Action<ReputationContribution> ReputationContributed;
        public static event Action<string> MapEntered;                          // mapId
        public static event Action<ItemData, TownData, int> ItemBought;          // 물건, 마을, 값
        public static event Action<ItemData, TownData, int, int> ItemSold;       // 물건, 마을, 값, 이익
        public static event Action<VehicleData> VehicleBought;
        public static event Action<Contract, bool> ContractFinished;            // 의뢰, 성공(부분 성공 포함)
        public static event Action<TownData, int> TownLeveled;                   // 마을, 새 등급

        /// <summary>
        /// Play를 누를 때마다 정적 상태를 비운다. (도메인 리로드를 끈 설정에서도 이벤트가 쌓이지 않게)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ClearAll();

        /// <summary>모든 구독을 지운다 (새 게임을 시작할 때도 부른다)</summary>
        public static void ClearAll()
        {
            MapEntered = null; ItemBought = null; ItemSold = null; VehicleBought = null;
            ContractFinished = null; TownLeveled = null; ReputationContributed = null;
            UIState.Reset();
            Notify.Clear();
        }

        /// <summary>
        /// 게임 안 "처음부터 시작"(GameManager.NewGame)용 (11/08 은샘). 씬을 넘어 살아 있는 물체(DontDestroyOnLoad)의 구독은 남긴다.
        /// 네 지역 첫걸음 안내(Intro)·온라인 장터처럼 게임을 켤 때 한 번만 구독하는 것들이 ClearAll로 지워지면,
        /// 같은 실행 안에서 새로 시작한 게임에서는 튜토리얼이 뜨지 않았다. 지워질 옛 GameManager(dying) 아래 구독과
        /// 일반 C# 객체·정적 구독은 예전처럼 지운다.
        /// </summary>
        public static void ClearForNewGame(GameObject dying)
        {
            ReputationContributed = Keep(ReputationContributed, dying); MapEntered = Keep(MapEntered, dying);
            ItemBought = Keep(ItemBought, dying); ItemSold = Keep(ItemSold, dying); VehicleBought = Keep(VehicleBought, dying);
            ContractFinished = Keep(ContractFinished, dying); TownLeveled = Keep(TownLeveled, dying);
            UIState.Reset();
            Notify.Clear();
        }

        static T Keep<T>(T handlers, GameObject dying) where T : Delegate
        {
            if (handlers == null) return null;
            Delegate kept = null;
            foreach (var d in handlers.GetInvocationList())
                if (d.Target is MonoBehaviour mb && mb != null && mb.gameObject.scene.name == "DontDestroyOnLoad"
                    && (dying == null || !mb.transform.IsChildOf(dying.transform)))
                    kept = Delegate.Combine(kept, d);
            return (T)kept;
        }

        public static void RaiseReputationContribution(ReputationContribution value) => ReputationContributed?.Invoke(value);
        public static void RaiseMapEntered(string mapId) => MapEntered?.Invoke(mapId);
        public static void RaiseItemBought(ItemData item, TownData town, int price) => ItemBought?.Invoke(item, town, price);
        public static void RaiseItemSold(ItemData item, TownData town, int price, int profit) => ItemSold?.Invoke(item, town, price, profit);
        public static void RaiseVehicleBought(VehicleData v) => VehicleBought?.Invoke(v);
        public static void RaiseContractFinished(Contract c, bool ok) => ContractFinished?.Invoke(c, ok);
        public static void RaiseTownLeveled(TownData t, int level) => TownLeveled?.Invoke(t, level);
    }
}
