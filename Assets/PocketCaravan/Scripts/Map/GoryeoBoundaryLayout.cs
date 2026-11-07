using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>T3 사각 경계. v2의 실제 지형 위치를 보존하고 고립·출구 위 저장은 안전한 자리로 복구한다.</summary>
    public static class GoryeoBoundaryLayout
    {
        [Serializable] public class Frame
        {
            public string id, main, stay, safe;
            public int oldX,oldY,oldWidth,oldHeight,x,y,width,height;
            public float legacyX,legacyY,legacyDropX,legacyDropY;
        }
        [Serializable] class Config { public int version; public Frame[] maps; }
        static Config config;
        static Config Data
        {
            get {
                if(config==null){var text=Resources.Load<TextAsset>("Goryeo/BoundaryLayout");
                    if(text==null)throw new InvalidOperationException("고려 경계 이주 데이터가 없습니다.");
                    config=JsonUtility.FromJson<Config>(text.text);
                    if(config==null||config.version!=3||config.maps==null||config.maps.Length!=13)throw new InvalidOperationException("고려 경계 이주 데이터가 올바르지 않습니다.");
                    foreach(var f in config.maps)if(f.width<=0||f.height<=0||f.main==null||f.stay==null||f.stay.Length!=f.width*f.height||f.safe==null||f.main.Length!=f.width*f.height||f.safe.Length!=f.main.Length||!f.safe.Contains("1"))throw new InvalidOperationException("고려 경계 복구 영역이 올바르지 않습니다.");
                }return config;
            }
        }
        public static Frame Find(string id) { id=GoryeoLayout.Resolve(id);foreach(var f in Data.maps)if(f.id==id)return f;return null; }
        public static bool Matches(string id,int width,int height) { var f=Find(id);return f!=null&&f.width==width&&f.height==height; }
        public static bool MainCell(string id,Vector3Int cell)
        {
            var f=Find(id);if(f==null)return true;int y=f.height-cell.y-1;
            return cell.x>=0&&cell.x<f.width&&y>=0&&y<f.height&&f.main[y*f.width+cell.x]=='1';
        }
        struct Location { public string id; public float x,y; public int height; }
        static Location Convert(string id,float px,float py,int savedHeight,int version,bool drop)
        {
            var old=Find(id);
            if(old==null)return new Location{id=id,x=px,y=py,height=savedHeight};
            if(version<2){px=drop?old.legacyDropX:old.legacyX;py=drop?old.legacyDropY:old.legacyY;savedHeight=old.oldHeight;}
            float wx=old.oldX+px,wy=old.oldY+(savedHeight>0?savedHeight:old.oldHeight)-py;
            if(float.IsNaN(wx)||float.IsInfinity(wx)||float.IsNaN(wy)||float.IsInfinity(wy))throw new InvalidOperationException("저장 위치가 올바르지 않습니다.");
            // Saves store body position; feet are .32 tiles below the body. Drops use their own point.
            float footY=wy+(drop?0:.32f);Frame target=null;
            foreach(var f in Data.maps)if(wx>=f.x&&wx<f.x+f.width&&footY>=f.y&&footY<f.y+f.height){target=f;break;}
            if(target==null)target=old; // The approved four-cell west trim also recovers here.
            float lx=wx-target.x,ly=footY-target.y;int cx=Mathf.FloorToInt(lx),cy=Mathf.FloorToInt(ly);
            bool valid=cx>=0&&cx<target.width&&cy>=0&&cy<target.height&&target.stay[cy*target.width+cx]=='1';
            if(!valid){
                float nearest=float.PositiveInfinity;int best=-1;
                for(int i=0;i<target.safe.Length;i++)if(target.safe[i]=='1'){
                    float dx=i%target.width+.5f-lx,dy=i/target.width+.5f-ly,dist=dx*dx+dy*dy;
                    if(dist<nearest){nearest=dist;best=i;}
                }
                if(best<0)throw new InvalidOperationException("고려 저장 위치를 복구할 수 없습니다.");
                wx=target.x+best%target.width+.5f;wy=target.y+best/target.width+.5f-(drop?0:.32f);
            }
            return new Location{id=target.id,x=wx-target.x,y=target.y+target.height-wy,height=target.height};
        }
        /// <summary>모든 계산 성공 뒤 메모리 DTO의 위치 필드만 변경. 디스크·재산·의뢰는 건드리지 않는다.</summary>
        public static void Migrate(SaveData data)
        {
            if(data==null||data.layoutVersion>=3)return;
            var player=Convert(data.scene,data.px,data.py,data.mapHeight,data.layoutVersion,false);
            var drops=new List<Location>();
            if(data.drops!=null)foreach(var r in data.drops)drops.Add(Convert(r.mapId,r.x,r.y,0,data.layoutVersion,true));
            data.scene=player.id;data.px=player.x;data.py=player.y;data.mapHeight=player.height;
            if(data.drops!=null)for(int i=0;i<drops.Count;i++){var r=data.drops[i];var p=drops[i];r.mapId=p.id;r.x=p.x;r.y=p.y;}
            data.layoutVersion=3;
        }
    }
}
