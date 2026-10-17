namespace PocketCaravan
{
    public class ObstacleHandle : Interactable
    {
        public ObstacleField field;
        public string placementId;
        public override string DisplayName => field!=null?field.Label(placementId):"장애물";
        public override Kind Category=>Kind.Danger;
        public override void Interact(PlayerController player){if(field!=null)field.TryWork(placementId,player);}
    }
}
