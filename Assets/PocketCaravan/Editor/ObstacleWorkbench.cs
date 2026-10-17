using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PocketCaravan.EditorTools
{
    public class ObstacleWorkbench : EditorWindow
    {
        public const string LibraryPath="Assets/PocketCaravan/Resources/Obstacles/ObstacleLibrary.asset";
        public const string ScenePath="Assets/PocketCaravan/Scenes/ObstacleLab/ObstaclePreview.unity";
        const string Base="Assets/PocketCaravan/Obstacles";
        [Serializable] class Source {public Definition[] definitions;}
        [Serializable] class Definition
        {
            public string id,label,category,visual,mode,action,description,itemId;
            public int width,height,money,itemCount;
            public float walkSpeed,cartSpeed,traction,driftX,driftY,staminaDrain,visibility,workSeconds,activeSeconds,openSeconds,threatSpeed,threatRadius,leash,weightLimit;
        }
        ObstacleLibrary library;int selected;Vector2 scroll;string search="";bool placing;
        [MenuItem("Pocket Caravan/맵 제작/장애물 라이브러리")]
        public static void Open(){var w=GetWindow<ObstacleWorkbench>("장애물 라이브러리");w.minSize=new Vector2(400,350);}
        static T Asset<T>(string path) where T:ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();var a=AssetDatabase.LoadAssetAtPath<T>(path);
            if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;
        }
        [MenuItem("Pocket Caravan/맵 제작/장애물 데이터 가져오기")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Play 종료 후 실행하세요.");
            AssetDatabase.Refresh();var source=JsonUtility.FromJson<Source>(File.ReadAllText(Base+"/definitions.json"));
            var lib=Asset<ObstacleLibrary>(LibraryPath);lib.definitions.Clear();var imported=new HashSet<string>();
            foreach(var s in source.definitions)
            {
                string png=Base+"/Art/"+s.visual+".png";
                if(imported.Add(png))
                {
                    var importer=AssetImporter.GetAtPath(png) as TextureImporter;
                    importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                }
                var d=Asset<ObstacleDefinition>(Base+"/Definitions/"+s.id+".asset");
                d.id=s.id;d.displayName=s.label;d.category=s.category;d.description=s.description;d.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);
                d.mode=(ObstacleMode)Enum.Parse(typeof(ObstacleMode),s.mode);d.action=(ObstacleAction)Enum.Parse(typeof(ObstacleAction),s.action);
                d.width=s.width;d.height=s.height;d.walkSpeed=s.walkSpeed;d.cartSpeed=s.cartSpeed;d.traction=s.traction;d.drift=new Vector2(s.driftX,s.driftY);d.staminaDrain=s.staminaDrain;d.visibility=s.visibility;
                d.workSeconds=s.workSeconds;d.money=s.money;d.itemId=s.itemId;d.itemCount=s.itemCount;d.activeSeconds=s.activeSeconds;d.openSeconds=s.openSeconds;d.threatSpeed=s.threatSpeed;d.threatRadius=s.threatRadius;d.leash=s.leash;d.weightLimit=s.weightLimit;
                EditorUtility.SetDirty(d);lib.definitions.Add(d);
            }
            EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
        }
        [MenuItem("Pocket Caravan/맵 제작/장애물 걷기 시험장")]
        public static void BuildLab()
        {
            var modules=AssetDatabase.LoadAssetAtPath<MapModuleLibrary>(MapModuleWorkbench.LibraryPath);
            int index=modules.entries.FindIndex(e=>e.id=="bridge_pair");
            MapModuleWorkbench.BuildPreview(index,0,0,ScenePath,true);
            var ui=UnityEngine.Object.FindFirstObjectByType<ModuleLabUI>();ui.lockLayout=true;
            var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();var field=map.gameObject.AddComponent<ObstacleField>();
            field.library=AssetDatabase.LoadAssetAtPath<ObstacleLibrary>(LibraryPath);
            field.placements=new List<ObstaclePlacement>{new ObstaclePlacement{id="trial",definitionId="fallen_log",x=18,y=18}};
            var lab=new GameObject("Obstacle Lab").AddComponent<ObstacleLabUI>();lab.library=field.library;lab.field=field;
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene,ScenePath);AssetDatabase.SaveAssets();
        }
        void OnEnable(){SceneView.duringSceneGui+=Place;}
        void OnDisable(){SceneView.duringSceneGui-=Place;}
        void OnGUI()
        {
            if(library==null)library=AssetDatabase.LoadAssetAtPath<ObstacleLibrary>(LibraryPath);
            if(library==null){if(GUILayout.Button("장애물 데이터 가져오기"))Import();return;}
            search=EditorGUILayout.TextField("검색",search);scroll=EditorGUILayout.BeginScrollView(scroll);
            for(int i=0;i<library.definitions.Count;i++){var d=library.definitions[i];if(!(d.displayName+d.category+d.id).Contains(search))continue;if(GUILayout.Toggle(selected==i,d.displayName+" · "+d.category,"Button"))selected=i;}
            EditorGUILayout.EndScrollView();EditorGUILayout.ObjectField("정의/교체할 아트",library.definitions[selected],typeof(ObstacleDefinition),false);
            EditorGUILayout.HelpBox("Scene 뷰에서 배치합니다. 크기/효과/비용은 정의 에셋, 좌표·단계는 Map의 ObstacleField에서 수정하세요. 연결부/스폰을 막지 않는지 별도 확인하세요.",MessageType.Info);
            placing=EditorGUILayout.Toggle("Scene 클릭 배치",placing);
            if(GUILayout.Button("장애물 걷기 시험장"))BuildLab();
        }
        void Place(SceneView view)
        {
            if(!placing || EditorApplication.isPlaying || library==null)return;
            var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();if(map==null)return;var road=map.GetComponent<RoadGrid>();
            var e=Event.current;int control=GUIUtility.GetControlID(FocusType.Passive);if(e.type==EventType.Layout)HandleUtility.AddDefaultControl(control);
            var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);if(!new Plane(Vector3.forward,map.transform.position).Raycast(ray,out float n))return;
            var cell=map.WorldToCell(ray.GetPoint(n));var d=library.definitions[selected];
            Handles.color=Color.yellow;Handles.DrawWireCube(map.CellCenter(cell)+new Vector3((d.width-1)*.5f,(d.height-1)*.5f),new Vector3(d.width,d.height,0));
            if(e.alt || e.type!=EventType.MouseDown || e.button!=0 || cell.x<0 || cell.y<0 || cell.x+d.width>road.width || cell.y+d.height>road.height)return;
            var field=map.GetComponent<ObstacleField>();if(field==null)field=Undo.AddComponent<ObstacleField>(map.gameObject);
            Undo.RecordObject(field,"장애물 배치");field.library=library;field.placements.Add(new ObstaclePlacement{id="obs_"+Guid.NewGuid().ToString("N").Substring(0,12),definitionId=d.id,x=cell.x,y=road.height-cell.y-d.height});EditorUtility.SetDirty(field);EditorSceneManager.MarkSceneDirty(map.gameObject.scene);e.Use();
        }
    }
}
