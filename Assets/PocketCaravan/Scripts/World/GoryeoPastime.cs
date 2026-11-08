using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 예성 나루 낚시꾼 (2026-11-08 은샘): 낚시 게임은 물가 낚싯대(GoryeoFishingSpot)로 옮기고, 낚시꾼은 낚시 방법을 알려 준다.
    /// 이름표 "낚시꾼"(WorldDisplayNames)과 장면의 자리는 그대로 둔다.
    /// </summary>
    public sealed class GoryeoPastime : Interactable
    {
        public override string DisplayName => "나루 낚시";
        public override string ActionVerb => "말 걸기";

        void Awake() => blocksMovement = false;

        static readonly string[] Pages =
        {
            "물가에 세워 둔 낚싯대 보이지? 거기서 Space를 누르면 낚시를 할 수 있어.\n예성 나루 말고도 버들나루, 남동 포구에도 하나씩 있어.",
            "물속 그림자를 잘 봐. 작은 건 흔한 물고기, 큰 그림자일수록 귀한 놈이야.\n힘 막대가 그림자 위에 올 때 Space로 던지면 돼.",
            "찌가 톡톡 건드리는 건 속임수야. 쑥 잠길 때 바로 Space!\n너무 일찍 채면 물고기가 달아나.",
            "걸리면 Space를 누르고 있으면 감기고, 떼면 풀려.\n긴장을 초록 칸에 두면 끌려 나오고, 너무 당기면 줄이 끊어져.",
            "한 번 낚고 나면 물이 일렁여서 물고기가 흩어져. 그림자가 다시 모일 때까지 조금 기다리렴.\n하구랑 바다는 밀물 때, 강은 새벽이랑 해질녘에 큰 놈이 잘 와.",
        };

        public override void Interact(PlayerController player)
        {
            if (UIState.Blocking) return;
            DialogueUI.Show("낚시꾼", string.Join("\n\n", Pages));   // 길면 대화창이 쪽을 나눠 Space로 넘긴다
        }
    }
}
