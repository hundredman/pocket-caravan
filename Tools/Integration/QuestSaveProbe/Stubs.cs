// Only platform/unrelated dependencies are replaced. Quest/save logic is linked unchanged.
namespace UnityEngine {
 public class ScriptableObject { }
 [System.AttributeUsage(System.AttributeTargets.Class)] public class CreateAssetMenuAttribute:System.Attribute { public string menuName,fileName; }
 [System.AttributeUsage(System.AttributeTargets.Field)] public class TooltipAttribute(string text):System.Attribute { }
 [System.AttributeUsage(System.AttributeTargets.Field)] public class HeaderAttribute(string text):System.Attribute { }
 [System.AttributeUsage(System.AttributeTargets.Field)] public class TextAreaAttribute:System.Attribute { public TextAreaAttribute(){} public TextAreaAttribute(int a,int b){} }
 [System.AttributeUsage(System.AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type):System.Attribute { }
 public enum RuntimeInitializeLoadType { SubsystemRegistration }
 public struct Vector3(float a,float b,float c) { }
 public static class Mathf { public static int Min(int a,int b)=>System.Math.Min(a,b); }
 public static class Application { public static string persistentDataPath=>throw new System.InvalidOperationException("Production save path forbidden in probe"); }
 public static class Debug { public static void LogWarning(string s){} }
 public static class JsonUtility { public static string ToJson(object o)=>throw new System.NotSupportedException("Unity serializer not tested"); public static T FromJson<T>(string s)=>throw new System.NotSupportedException("Unity serializer not tested"); }
}
namespace PocketCaravan {
 public class RegionData { public string id,displayName; public bool townReputation; }
 public class OriginData { public string id; public RegionData homeRegion; }
 public class ItemData { public string id,displayName; public bool questItem; public TownData originTown; }
 public class VehicleData { public string id; }
 public class TownData { public string id,displayName; public RegionData region; public List<VehicleData> vehiclesForSale=new(); }
 public class GameDatabase { public List<ItemData> items=new(); public List<VehicleData> vehicles=new(); public List<QuestData> quests=new(); public List<TownData> towns=new(); }
 public class Contract { public int id,reward; public string fromTown,toTown; public float weight,condition,hours,acceptedAt,deadline,finalDeadline; }
 public class Inventory { readonly Dictionary<ItemData,int> counts=new(); public int Count(ItemData item)=>counts.GetValueOrDefault(item); public bool Remove(ItemData item,int count){if(Count(item)<count)return false;counts[item]-=count;return true;} public void Add(ItemData item,int count,float price=0){counts[item]=Count(item)+count;} }
 public class PlayerState { public ReputationState reputation=new();public TradeActivity tradeActivity=new();public int money,credit; public VehicleData vehicle; public Inventory cargo=new(); public List<Contract> contracts=new(); public bool CanCarry(ItemData item,int count)=>true; }
 public class GameManager { public static GameManager I=new(); public PlayerState Player=new(); public GameDatabase Db=new(); }
 public class MapRoot { public static MapRoot Current; public string mapId; }
 public static class GoryeoLayout { public static string Resolve(string s)=>s; public static bool TryPosition(SaveData d,out UnityEngine.Vector3 v){v=default;return false;} }
 public class WorldIndex { public class Info {public int height,legacyHeight;} public static WorldIndex Get()=>null; public Info Map(string id)=>null; }
 public static class Notify { public static void Show(string s,float seconds=0){} public static void Clear(){} }
 public static class Sfx { public static void Play(string s){} }
 public static class UIState { public static bool Blocking; }
 public class StoredCargo { }
 public class WarehouseStore { public void Import(List<StoredCargo> data){} }
 public class MarketState { public class Save { public string town; public float volume; public List<string> items=new(); public List<float> stocks=new(); } }
 public class ContractManager { public class Board {public string town;public List<Contract> list=new();} }
 public class NpcTraders { public class Trader{} }
 public class DropRecord{} public class TradeLedger { public class Quote{} }
}
