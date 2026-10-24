using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>Read-only migration from retired road scenes into the five settlement regions.</summary>
    public static class GoryeoLayout
    {
        [Serializable] public class Entry { public string id; public int offsetX,offsetY,height,legacyHeight; public float spawnX,spawnY; }
        [Serializable] public class Alias { public string oldId,newId; }
        [Serializable] class Config { public Entry[] maps; public Alias[] aliases; }
        static Config config;
        static Config Data { get { if(config==null){var text=Resources.Load<TextAsset>("GoryeoLayout");config=text!=null?JsonUtility.FromJson<Config>(text.text):new Config();}return config;} }
        public static string Resolve(string id)
        {
            if(Data.aliases!=null)foreach(var a in Data.aliases)if(a.oldId==id)return a.newId;
            return id;
        }
        public static Entry Find(string id)
        {
            id=Resolve(id);if(Data.maps!=null)foreach(var m in Data.maps)if(m.id==id)return m;
            return null;
        }
        public static bool TryPosition(SaveData data,out Vector3 position)
        {
            position=default;var m=Find(data.scene);if(m==null || data.layoutVersion>=1)return false;
            if(Resolve(data.scene)!=data.scene){position=new Vector3(m.spawnX,m.spawnY,0);return true;}
            int previous=data.mapHeight>0?data.mapHeight:m.legacyHeight;
            position=new Vector3(data.px+m.offsetX,data.py+m.height-previous-m.offsetY,0);return true;
        }
    }
}
