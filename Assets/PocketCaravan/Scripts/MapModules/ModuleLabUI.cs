using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>별도 제작 시험장. 본편/세계 등록부/사용자 저장을 변경하지 않는다.</summary>
    public class ModuleLabUI : MonoBehaviour
    {
        public MapModuleLibrary library;
        public MapAppearance appearance;
        public int selected,environmentIndex,weatherIndex;
        public bool lockLayout;
        public bool visible=true;
        void Start()
        {
            var gm=GameManager.I;
            if(gm!=null)
            {
                gm.enabled=false; // Freeze the world clock in the authoring lab.
                foreach(var component in gm.GetComponents<MonoBehaviour>())
                    if(component is HUD || component is QuestTracker || component is CommerceGuideUI || component is WorldMapUI || component is TradeLedgerUI || component is TownFacilitiesUI || component is PauseMenu || component is AmbientFX)component.enabled=false;
            }
            int chosenEnvironment=environmentIndex;
            if(!lockLayout)Load(selected);else appearance.Apply();
            SetEnvironment(chosenEnvironment);
            SetWeather(weatherIndex);
        }
        public void Load(int index)
        {
            if(library==null || library.entries.Count==0)return;
            selected=(index%library.entries.Count+library.entries.Count)%library.entries.Count;
            var entry=library.entries[selected];var f=JsonUtility.FromJson<ModuleMapFile>(entry.layout.text);
            var map=MapRoot.Current;appearance.Restore();
            var lookup=new Dictionary<string,TileBase>();foreach(var tile in library.tiles)if(tile!=null)lookup[tile.name]=tile;
            var layers=new[]{f.ground,f.deco,f.obj,f.high};
            for(int i=0;i<4;i++)
            {
                var target=appearance.visualLayers[i];target.ClearAllTiles();
                for(int n=0;n<layers[i].Length;n++)if(layers[i][n]>0 && lookup.TryGetValue(f.palette[layers[i][n]],out var tile))
                    target.SetTile(new Vector3Int(n%f.w,f.h-1-n/f.w,0),tile);
                target.CompressBounds();
            }
            map.blocked.ClearAllTiles();
            if(blocker==null){blocker=ScriptableObject.CreateInstance<Tile>();blocker.name="Module lab collision";}
            for(int n=0;n<f.blocked.Length;n++)if(f.blocked[n]!=0)map.blocked.SetTile(new Vector3Int(n%f.w,f.h-1-n/f.w,0),blocker);
            var roads=RoadGrid.Current;roads.width=f.w;roads.height=f.h;roads.grades=new int[f.w*f.h];
            for(int n=0;n<f.road.Length;n++)roads.grades[(f.h-1-n/f.w)*f.w+n%f.w]=f.road[n];
            map.mapId=f.id;map.displayName=entry.label;
            var hazards=map.GetComponent<ObstacleField>();
            if(hazards!=null || (f.obstacles!=null && f.obstacles.Length>0)){if(hazards==null)hazards=map.gameObject.AddComponent<ObstacleField>();hazards.Replace(f.obstacles);}
            environmentIndex=Mathf.Max(0,library.environments.FindIndex(s=>s.id==entry.environment));
            appearance.environment=library.environments[environmentIndex];appearance.Apply();
            var player=PlayerController.Instance;
            if(player!=null)player.Motor.Teleport(map.CellCenter(new Vector3Int(2,f.h-21,0))-(Vector3)player.Motor.feetOffset,Vector2Int.right);
            var follow=FindFirstObjectByType<CameraFollow>();if(follow!=null)follow.SnapNow();
        }
        Tile blocker;
        public void SetEnvironment(int index)
        {
            environmentIndex=(index%library.environments.Count+library.environments.Count)%library.environments.Count;
            appearance.environment=library.environments[environmentIndex];appearance.Apply();
        }
        public void SetWeather(int index)
        {
            weatherIndex=(index%library.weather.Count+library.weather.Count)%library.weather.Count;
            appearance.SetWeather(library.weather[weatherIndex]);
        }
        void OnGUI()
        {
            if(library==null || library.entries.Count==0)return;
            UIStyle.Begin();GUI.depth=-20;
            float width=Mathf.Min(610,UIStyle.VirtualWidth-12);
            GUILayout.BeginArea(new Rect((UIStyle.VirtualWidth-width)/2,UIStyle.VirtualHeight-(visible?113:28),width,visible?110:26),UIStyle.Box);
            if(GUILayout.Button(visible?"제작 시험장 접기":"제작 시험장 펼치기",UIStyle.Button))visible=!visible;
            if(visible)
            {
                var entry=library.entries[selected];
                GUILayout.BeginHorizontal();
                if(!lockLayout && GUILayout.Button("◀ 원형",UIStyle.Button,GUILayout.Width(67)))Load(selected-1);
                GUILayout.Label(lockLayout?MapRoot.Current.displayName:$"{selected+1}/{library.entries.Count} {entry.label} · {entry.category}",UIStyle.Small);
                if(!lockLayout && GUILayout.Button("원형 ▶",UIStyle.Button,GUILayout.Width(67)))Load(selected+1);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if(GUILayout.Button("환경 ◀",UIStyle.Button,GUILayout.Width(67)))SetEnvironment(environmentIndex-1);
                GUILayout.Label(library.environments[environmentIndex].displayName,UIStyle.Small);
                if(GUILayout.Button("환경 ▶",UIStyle.Button,GUILayout.Width(67)))SetEnvironment(environmentIndex+1);
                if(GUILayout.Button("날씨 ◀",UIStyle.Button,GUILayout.Width(67)))SetWeather(weatherIndex-1);
                GUILayout.Label(library.weather[weatherIndex].displayName,UIStyle.Small);
                if(GUILayout.Button("날씨 ▶",UIStyle.Button,GUILayout.Width(67)))SetWeather(weatherIndex+1);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("WASD 이동 · Shift 달리기 | 환경/날씨는 외형만 변경",UIStyle.Small);
                string target=lockLayout?"ModulePreview":"ModuleComposition";
                if(UnityEngine.Application.CanStreamedLevelBeLoaded(target) && GUILayout.Button(lockLayout?"원형 보기":"4구역 조합 보기",UIStyle.Button,GUILayout.Width(110)))UnityEngine.SceneManagement.SceneManager.LoadScene(target);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }
        void OnDestroy(){if(blocker!=null)Destroy(blocker);}
    }
}
