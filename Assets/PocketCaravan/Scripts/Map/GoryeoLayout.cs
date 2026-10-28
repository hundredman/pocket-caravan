using System;
using UnityEngine;
namespace PocketCaravan
{
    /// <summary>옛 저장 이주: 은퇴한 길 씬 별칭과, 옛 배치 저장의 위치를 새 맵 도착 자리로 옮기기(읽기 전용).</summary>
    public static class GoryeoLayout
    {
        [Serializable] public class Entry { public string id; public int offsetX,offsetY,height,legacyHeight; public float spawnX,spawnY,dropX,dropY; }
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
        /// <summary>2: M4 새 맵(2026-10-28). 1 이하 저장의 위치는 옛 맵 좌표라 새 맵에서 쓸 수 없다.</summary>
        public const int Current = 2;
        /// <summary>옛 배치 저장(1 이하): 같은 맵(옛 ID는 별칭으로)의 도착 자리에서 다시 시작한다. 맵·재산·진행은 그대로다.</summary>
        public static bool TryPosition(SaveData data,out Vector3 position)
        {
            position=default;if(data.layoutVersion>=Current)return false;
            var m=Find(data.scene);if(m==null)return false;
            position=new Vector3(m.spawnX,m.spawnY,0);return true;
        }
        /// <summary>옛 배치 저장의 떨어진 짐은 같은 맵 도착 자리 옆으로 옮긴다(잃지 않게).</summary>
        public static int MigrateDrops(SaveData data)
        {
            if(data.layoutVersion>=Current || data.drops==null)return 0;
            int n=0;
            foreach(var r in data.drops)
            {
                var m=Find(r.mapId);if(m==null)continue;
                r.mapId=m.id;r.x=m.dropX;r.y=m.dropY;n++;
            }
            return n;
        }
    }
}
