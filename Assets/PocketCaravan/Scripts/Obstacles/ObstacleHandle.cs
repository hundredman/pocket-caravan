namespace PocketCaravan
{
    public class ObstacleHandle : Interactable
    {
        public ObstacleField field;
        public string placementId;
        public override string DisplayName => field!=null?field.Label(placementId):"장애물";
        public override Kind Category=>Kind.Danger;
        public override string ActionVerb=>"살펴보기";   // 11/07(기우) 노면 장애물에 "말 걸기"가 떠서
        public override void Interact(PlayerController player){if(field!=null)field.TryWork(placementId,player);}
    }
}
