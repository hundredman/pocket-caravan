// Platform-only stand-ins. Market math/state and item/town/origin/database classes link production C#.
namespace UnityEngine {
 public class ScriptableObject {}
 public class TextAsset {public string text;}
 public static class Resources {public static string Folder;public static T Load<T>(string name) where T:class=>new TextAsset{text=System.IO.File.ReadAllText(System.IO.Path.Combine(Folder,name+".json"))} as T;}
 public static class JsonUtility {public static T FromJson<T>(string s)=>System.Text.Json.JsonSerializer.Deserialize<T>(s,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});} public class Sprite {}
 public class CreateAssetMenuAttribute:System.Attribute {public string menuName,fileName;}
 public class MinAttribute(float x):System.Attribute {} public class RangeAttribute(float a,float b):System.Attribute {}
 public class TooltipAttribute(string x):System.Attribute {} public class HeaderAttribute(string x):System.Attribute {} public class TextAreaAttribute:System.Attribute {}
 public struct Vector2Int { public int x,y; public Vector2Int(int a,int b){x=a;y=b;}public static Vector2Int operator -(Vector2Int a,Vector2Int b)=>new(a.x-b.x,a.y-b.y); }
 public static class Mathf {
 public static float Min(float a,float b)=>MathF.Min(a,b);
 public static int Abs(int x)=>Math.Abs(x); public static float Abs(float x)=>MathF.Abs(x);
 public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);
 public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);
 public static int RoundToInt(float x)=>(int)MathF.Round(x,MidpointRounding.ToEven);public static int FloorToInt(float x)=>(int)MathF.Floor(x);
 public static float Lerp(float a,float b,float t)=>a+(b-a)*Math.Clamp(t,0,1);
 }
}
namespace PocketCaravan {
 public class RegionData {public string id;}
 public class VehicleData {} public class QuestData {} public class TownFacility {}
 public class Cargo {public int Count(ItemData i)=>0;public float AverageCost(ItemData i)=>0;}
 public class PlayerState {public OriginData origin;public int money;public Cargo cargo=new();public bool CanCarry(ItemData i,int n)=>true;}
 public class Clock {public bool IsMarketDay;}
 public class GameManager {public static GameManager I;public PlayerState Player=new();public Clock Clock=new();}
 public static class GameEvents {public static void RaiseTownLeveled(TownData t,int level) {}}
 public class MarketEstimate {public MarketEstimate(TownData t,ItemData i,bool selling,List<int> prices,float average,string blocker,int money){}}
}

namespace PocketCaravan {
 public class WorldIndex {public class MapInfo {public List<string> neighbors=new();}public string MapOf(string kind,string id)=>null;public MapInfo Map(string id)=>null;}
}
