using UnityEngine;

namespace PocketCaravan
{
    // 11/19: Crafting.cs 안에 있던 것을 따로 뺐다. MonoBehaviour는 클래스 이름과 파일 이름이 같아야
    //   씬에 제대로 저장된다 (전엔 스크립트 참조가 빈 채로 저장돼 빌드에서 사나·타이즈 씬이 "corrupted"로 멈췄다)
    /// <summary>맵에 놓는 공방 자리 (지역 담당이 공방을 더할 때 이걸 붙이고 recipeId만 적으면 된다)</summary>
    public class WorkshopSpot : Interactable
    {
        public string recipeId;
        public override string DisplayName => Crafting.Label(recipeId);
        public override Kind Category => Kind.Market;
        public override void Interact(PlayerController player) => Crafting.Open(recipeId);
    }
}
