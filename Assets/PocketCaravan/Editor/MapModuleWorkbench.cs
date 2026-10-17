using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    public class MapModuleWorkbench : EditorWindow
    {
        const string Root="Assets/PocketCaravan";
        const string Base=Root+"/MapModules";
        public const string LibraryPath=Root+"/Resources/MapModules/ModuleLibrary.asset";
        public const string ScenePath=Root+"/Scenes/ModuleLab/ModulePreview.unity";
        [Serializable] class SourceEntry { public string id,label,category,environment,jsonPath,sourcePath,previewPath;public string[] ports; }
        [Serializable] class SourceCatalogue { public SourceEntry[] entries; }
        [Serializable] class SkinInfo { public string id,label; }
        [Serializable] class SkinIndex { public int columns,tileSize;public string[] tileNames;public SkinInfo[] environments; }
        MapModuleLibrary library;int selected,env,weather;Vector2 scroll;string search="";
        [MenuItem("Pocket Caravan/맵 제작/환경 원형 라이브러리")]
        public static void Open()=>GetWindow<MapModuleWorkbench>("맵 원형 라이브러리");
        static T Asset<T>(string path) where T:ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
            var a=AssetDatabase.LoadAssetAtPath<T>(path);
            if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;
        }
        static void Texture(string path,bool sprite)
        {
            var i=AssetImporter.GetAtPath(path) as TextureImporter;if(i==null)throw new IOException("이미지를 먼저 내보내세요: "+path);
            i.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;i.filterMode=FilterMode.Point;
            i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;
            i.spritePixelsPerUnit=16;i.maxTextureSize=2048;i.alphaIsTransparency=true;i.SaveAndReimport();
        }
        [MenuItem("Pocket Caravan/맵 제작/내보낸 라이브러리 가져오기")]
        public static void Import()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Play 종료 후 가져오세요.");
            AssetDatabase.Refresh();
            var source=JsonUtility.FromJson<SourceCatalogue>(File.ReadAllText(Base+"/catalogue.json"));
            var skinInfo=JsonUtility.FromJson<SkinIndex>(File.ReadAllText(Base+"/skin-index.json"));
            var lib=Asset<MapModuleLibrary>(LibraryPath);lib.entries.Clear();lib.environments.Clear();lib.weather.Clear();lib.tiles.Clear();
            foreach(var s in source.entries)
            {
                lib.entries.Add(new MapModuleLibrary.Entry{id=s.id,label=s.label,category=s.category,environment=s.environment,ports=s.ports,
                    layout=AssetDatabase.LoadAssetAtPath<TextAsset>(s.jsonPath),source=AssetDatabase.LoadAssetAtPath<TextAsset>(s.sourcePath),preview=AssetDatabase.LoadAssetAtPath<Texture2D>(s.previewPath)});
            }
            foreach(var name in skinInfo.tileNames)
            {
                string path=Root+"/Art/NinjaAdventure/TileAssets/"+name+".asset";
                var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                if(tile==null)
                {
                    string png=Root+"/Art/NinjaAdventure/Tiles/"+name+".png";Texture(png,true);
                    tile=Asset<Tile>(path);tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);tile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(tile);
                }
                lib.tiles.Add(tile);
            }
            foreach(var s in skinInfo.environments)
            {
                string path=Base+"/Skins/"+s.id+".png";Texture(path,false);
                var skin=Asset<EnvironmentSkin>(Base+"/Profiles/Environment_"+s.id+".asset");
                skin.id=s.id;skin.displayName=s.label;skin.atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(path);skin.tileNames=skinInfo.tileNames;
                skin.columns=skinInfo.columns;skin.tileSize=skinInfo.tileSize;EditorUtility.SetDirty(skin);lib.environments.Add(skin);
            }
            void Weather(string id,string label,Color tint,Color haze,Color particles,int count,Vector2 drift,Vector2 size,float flutter=0)
            {
                var s=Asset<WeatherSkin>(Base+"/Profiles/Weather_"+id+".asset");s.id=id;s.displayName=label;s.sceneTint=tint;s.haze=haze;s.particleColor=particles;
                s.particleCount=count;s.drift=drift;s.size=size;s.flutter=flutter;EditorUtility.SetDirty(s);lib.weather.Add(s);
            }
            Weather("clear","맑음",Color.white,Color.clear,Color.clear,0,Vector2.zero,Vector2.one);
            Weather("overcast","흐림",new Color(.83f,.87f,.93f,1),new Color(.55f,.62f,.68f,.06f),Color.clear,0,Vector2.zero,Vector2.one);
            Weather("rain","비",new Color(.77f,.84f,.92f,1),new Color(.5f,.6f,.7f,.08f),new Color(.7f,.85f,1,.6f),110,new Vector2(-3,-16),new Vector2(.025f,.35f));
            Weather("snow","눈",new Color(.92f,.96f,1,1),new Color(.8f,.86f,.92f,.09f),new Color(.95f,.98f,1,.85f),100,new Vector2(-.4f,-2),new Vector2(.065f,.065f),.7f);
            Weather("fog","안개",new Color(.88f,.92f,.94f,1),new Color(.75f,.82f,.82f,.34f),Color.clear,0,Vector2.zero,Vector2.one);
            Weather("dust","모래바람",new Color(1,.9f,.76f,1),new Color(.79f,.63f,.37f,.19f),new Color(.91f,.77f,.49f,.6f),100,new Vector2(7,-.6f),new Vector2(.1f,.025f),1);
            Weather("leaves","낙엽바람",new Color(1,.96f,.88f,1),Color.clear,new Color(.81f,.48f,.21f,.9f),60,new Vector2(1.7f,-1.4f),new Vector2(.1f,.06f),1);
            Weather("fireflies","반딧불 밤",new Color(.55f,.62f,.8f,1),new Color(.18f,.23f,.38f,.12f),new Color(.92f,1,.4f,.85f),45,new Vector2(.15f,.2f),new Vector2(.06f,.06f),.6f);
            EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();Debug.Log($"맵 원형 {lib.entries.Count} / 환경 {lib.environments.Count} / 날씨 {lib.weather.Count}");
        }
        public static void BuildPreview(int index=0,int environment=-1,int weather=0,string destination=ScenePath,bool lab=true)
        {
            var lib=AssetDatabase.LoadAssetAtPath<MapModuleLibrary>(LibraryPath);if(lib==null)throw new InvalidOperationException("라이브러리를 먼저 가져오세요.");
            var entry=lib.entries[index];
            int env=environment>=0?environment:Mathf.Max(0,lib.environments.FindIndex(s=>s.id==entry.environment));
            BuildMap(entry.layout.text,index,env,weather,destination,lab,false);
        }
        [MenuItem("Pocket Caravan/맵 제작/네 구역 조합 시험장")]
        public static void BuildComposition()
        {
            BuildMap(File.ReadAllText(Base+"/Compositions/four_crossroads.json"),0,0,0,Root+"/Scenes/ModuleLab/ModuleComposition.unity",true,true);
        }
        static void BuildMap(string json,int index,int env,int weather,string destination,bool lab,bool fixedLayout)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Play 종료 후 실행하세요.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("수정 중인 씬을 먼저 저장하세요.");
            var lib=AssetDatabase.LoadAssetAtPath<MapModuleLibrary>(LibraryPath);if(lib==null)throw new InvalidOperationException("라이브러리를 먼저 가져오세요.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination));AssetDatabase.Refresh();
            MapJsonBuilder.BuildSingleMap(json,destination);
            var map=UnityEngine.Object.FindFirstObjectByType<MapRoot>();map.previewOnly=true;
            var appearance=map.gameObject.AddComponent<MapAppearance>();
            appearance.visualLayers=new[]{map.transform.Find("Ground").GetComponent<Tilemap>(),map.transform.Find("Deco").GetComponent<Tilemap>(),map.transform.Find("Objects").GetComponent<Tilemap>(),map.transform.Find("Above").GetComponent<Tilemap>()};
            appearance.environment=lib.environments[env];appearance.weather=lib.weather[weather];
            if(lab){var ui=new GameObject("Module Library Lab").AddComponent<ModuleLabUI>();ui.library=lib;ui.appearance=appearance;ui.selected=index;ui.environmentIndex=env;ui.weatherIndex=weather;ui.lockLayout=fixedLayout;}
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene,destination);AssetDatabase.SaveAssets();
            if(lab)
            {
                var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                if(!scenes.Exists(s=>s.path==destination))scenes.Add(new EditorBuildSettingsScene(destination,true));
                EditorBuildSettings.scenes=scenes.ToArray();
            }
        }
        void OnGUI()
        {
            if(library==null)library=AssetDatabase.LoadAssetAtPath<MapModuleLibrary>(LibraryPath);
            if(library==null){if(GUILayout.Button("내보낸 라이브러리 가져오기"))Import();return;}
            EditorGUILayout.HelpBox("원형 → 독립 편집 씬. 환경/날씨는 Play에서 적용됩니다. 제작 시험장은 본편 저장/세계 등록부와 분리됩니다.",MessageType.Info);
            search=EditorGUILayout.TextField("검색",search);
            scroll=EditorGUILayout.BeginScrollView(scroll,GUILayout.Height(180));
            for(int i=0;i<library.entries.Count;i++)
            {
                var e=library.entries[i];if(!string.IsNullOrEmpty(search) && !(e.label+e.category+e.id).Contains(search))continue;
                if(GUILayout.Toggle(selected==i,$"{e.label} / {e.category} / {string.Join("",e.ports)}","Button")){if(selected!=i){selected=i;env=Mathf.Max(0,library.environments.FindIndex(s=>s.id==e.environment));}}
            }
            EditorGUILayout.EndScrollView();var current=library.entries[selected];
            env=EditorGUILayout.Popup("환경 스킨",env,library.environments.ConvertAll(s=>s.displayName).ToArray());
            weather=EditorGUILayout.Popup("날씨 스킨",weather,library.weather.ConvertAll(s=>s.displayName).ToArray());
            if(current.preview!=null){var rect=GUILayoutUtility.GetRect(250,250);GUI.DrawTexture(rect,current.preview,ScaleMode.ScaleToFit);}
            EditorGUILayout.HelpBox("아래 썸네일은 원형 기본 환경입니다. 선택한 스킨 조합은 Play에서 확인하세요. 환경 프로필의 Overrides로 개별 타일 아트를 교체할 수 있습니다.",MessageType.None);
            EditorGUILayout.ObjectField("수정용 원본",current.source,typeof(TextAsset),false);
            if(GUILayout.Button("제작 시험장 만들기 / 열기"))BuildPreview(selected,env,weather);
            if(GUILayout.Button("네 구역 조합 시험장 열기"))BuildComposition();
            if(GUILayout.Button("독립 편집 씬으로 복제…"))
            {
                string path=EditorUtility.SaveFilePanelInProject("독립 편집 씬","CustomMap","unity","원형을 복제한 뒤 Unity Tilemap에서 수정하세요.",Root+"/Scenes");
                if(!string.IsNullOrEmpty(path))BuildPreview(selected,env,weather,path,false);
            }
        }
    }
}
