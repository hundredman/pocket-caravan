// R3·R4 규칙 원본(Haggle·StandingRecovery·TradeActivity·ReputationState)만 연결한다. 나머지 게임 타입만 대체한다.
namespace PocketCaravan {
 public class RegionData { public string id,displayName; public bool townReputation; }
 public class OriginData { public string id; public RegionData homeRegion; }
 public class TownData { public string id,displayName; public RegionData region; }
 public class GameDatabase { public List<TownData> towns=new(); }
 public class PlayerState { public ReputationState reputation=new(); }
 public class GameManager { public static GameManager I=new(); public PlayerState Player=new(); }
 public static class GameEvents { public static void RaiseReputationContribution(ReputationContribution c){} }
 public class SaveData { public int version; public ReputationState.Save reputation; }
 public static class SaveSystem { public const int CurrentVersion=3; }
}
