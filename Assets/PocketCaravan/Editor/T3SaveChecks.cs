using System;
using System.IO;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public static class T3SaveChecks
    {
        [Serializable]class Frames {public GoryeoBoundaryLayout.Frame[] maps;}
        static int checks;static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
        public static string Run()
        {
            checks=0;int moved=0,recovered=0;var frames=JsonUtility.FromJson<Frames>(Resources.Load<TextAsset>("Goryeo/BoundaryLayout").text).maps;
            foreach(var old in frames)for(int version=0;version<=3;version++){   // 11/08: v3(첫 T3 틀) 저장도 v4로
                int fw=version==3?old.prevWidth:old.oldWidth,fh=version==3?old.prevHeight:old.oldHeight,fx=version==3?old.prevX:old.oldX,fy=version==3?old.prevY:old.oldY;
                // Include both interior world cells and old overlapping cells that now belong to another map.
                for(int yy=0;yy<fh;yy+=3)for(int xx=0;xx<fw;xx+=3){
                    var d=JsonUtility.FromJson<SaveData>("{\"version\":3,\"money\":12345,\"credit\":77,\"origin\":\"goryeo\",\"vehicle\":\"handcart\",\"stamina\":43.5,\"shake\":2,\"fx\":1,\"fy\":0,\"day\":8,\"minute\":750,\"finishedQuests\":[\"goryeo_q1\"],\"trackedQuest\":\"next\",\"cargoItems\":[\"grain\"],\"cargoCounts\":[3],\"cargoCosts\":[7.5],\"cooldownKeys\":[\"goryeo:daily:water_day\"],\"cooldownUntil\":[8],\"drops\":[]}");
                    d.scene=old.id;d.px=xx+.5f;d.py=fh-yy-.18f;d.mapHeight=fh;d.layoutVersion=version;
                    d.drops.Add(new DropRecord{mapId=old.id,x=d.px,y=d.py,itemId="unknown_legacy_item",unitCost=17,label="retain"});
                    var original=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));GoryeoBoundaryLayout.Migrate(d);
                    var target=GoryeoBoundaryLayout.Find(d.scene);Check(target!=null&&d.mapHeight==target.height&&d.layoutVersion==4,"new save frame");
                    int cx=Mathf.FloorToInt(d.px),cy=Mathf.FloorToInt(target.height-d.py+.32f);
                    Check(cx>=0&&cx<target.width&&cy>=0&&cy<target.height&&target.stay[cy*target.width+cx]=='1',"player reachable non-exit recovery");
                    if(version>=2){
                        float worldX=fx+original.px,worldY=fy+fh-original.py;
                        int localX=Mathf.FloorToInt(worldX-target.x),localY=Mathf.FloorToInt(worldY+.32f-target.y);
                        if(localX>=0&&localX<target.width&&localY>=0&&localY<target.height&&target.stay[localY*target.width+localX]=='1')
                            Check(Mathf.Abs(d.px+target.x-worldX)<.0001f&&Mathf.Abs(target.y+target.height-d.py-worldY)<.0001f,"valid v2 world location exactly preserved");
                        var noHeight=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original));noHeight.mapHeight=0;GoryeoBoundaryLayout.Migrate(noHeight);
                        Check(noHeight.scene==d.scene&&noHeight.px==d.px&&noHeight.py==d.py,"missing old mapHeight migration");
                    }
                    var after=JsonUtility.ToJson(d);GoryeoBoundaryLayout.Migrate(d);Check(after==JsonUtility.ToJson(d),"migration idempotent");
                    if(d.scene!=old.id)moved++;
                    if(version>=2){float wx=fx+original.px,wy=fy+fh-original.py;
                        if(Mathf.Abs(wx-(target.x+d.px))>.001f||Mathf.Abs(wy-(target.y+target.height-d.py))>.001f)recovered++;
                    }
                    original.scene=d.scene;original.px=d.px;original.py=d.py;original.mapHeight=d.mapHeight;original.layoutVersion=d.layoutVersion;
                    for(int i=0;i<original.drops.Count;i++){original.drops[i].mapId=d.drops[i].mapId;original.drops[i].x=d.drops[i].x;original.drops[i].y=d.drops[i].y;}
                    Check(JsonUtility.ToJson(original)==JsonUtility.ToJson(d),"all non-position save fields preserved");
                    var drop=d.drops[0];var df=GoryeoBoundaryLayout.Find(drop.mapId);cx=Mathf.FloorToInt(drop.x);cy=Mathf.FloorToInt(df.height-drop.y);
                    Check(cx>=0&&cx<df.width&&cy>=0&&cy<df.height&&df.stay[cy*df.width+cx]=='1',"unknown legacy cargo retained at safe drop location");
                }
                var alias=JsonUtility.FromJson<SaveData>("{\"scene\":\"Route01\",\"layoutVersion\":0}");GoryeoBoundaryLayout.Migrate(alias);Check(alias.scene=="Naru","legacy Route01 alias");
            }
            var foreign=JsonUtility.FromJson<SaveData>("{\"scene\":\"arabia_sanaa\",\"layoutVersion\":2,\"px\":12.3,\"py\":8.1,\"mapHeight\":44,\"money\":91}");var before=JsonUtility.ToJson(foreign);GoryeoBoundaryLayout.Migrate(foreign);foreign.layoutVersion=2;Check(before==JsonUtility.ToJson(foreign),"foreign save unchanged");
            var bad=new SaveData{scene="Naru",layoutVersion=2,px=float.NaN,py=1,money=19};
            bool rejected=false;try{GoryeoBoundaryLayout.Migrate(bad);}catch(InvalidOperationException){rejected=true;}
            Check(rejected&&bad.layoutVersion==2&&bad.money==19&&bad.scene=="Naru","invalid coordinate rejected before DTO mutation");
            var modern=new SaveData{scene="Naru",layoutVersion=4,px=3,py=7,money=21};var modernJson=JsonUtility.ToJson(modern);GoryeoBoundaryLayout.Migrate(modern);Check(modernJson==JsonUtility.ToJson(modern),"v4 untouched");
            string result="{\"checks\":"+checks+",\"crossMapPositions\":"+moved+",\"v2RecoveredPositions\":"+recovered+",\"complete\":true}";
            Directory.CreateDirectory("Docs/Validation/T3-Production-2026-11-08");File.WriteAllText("Docs/Validation/T3-Production-2026-11-08/save-migration.json",result);return result;
        }
    }
}
