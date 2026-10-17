using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    /// <summary>시각 Tile Palette와 별도로 노면/충돌을 명시적으로 편집한다.</summary>
    public class ModuleRoadPainter : EditorWindow
    {
        bool painting,collision,blocked=true;int grade=2;bool overlay=true;
        [MenuItem("Pocket Caravan/맵 제작/노면·충돌 브러시")]
        public static void Open()=>GetWindow<ModuleRoadPainter>("노면·충돌");
        void OnEnable(){SceneView.duringSceneGui+=Draw;}
        void OnDisable(){SceneView.duringSceneGui-=Draw;}
        void OnGUI()
        {
            EditorGUILayout.HelpBox("시각 타일은 Unity Tile Palette에서 그립니다. 이 브러시는 이동 속도용 노면/충돌만 편집합니다. 원형을 독립 씬으로 복제해 사용하세요.",MessageType.Info);
            painting=EditorGUILayout.Toggle("브러시 활성",painting);collision=EditorGUILayout.Toggle("충돌 편집",collision);
            if(collision)blocked=EditorGUILayout.Toggle("통행 금지",blocked);else grade=EditorGUILayout.IntSlider("노면 등급",grade,0,3);
            overlay=EditorGUILayout.Toggle("노면 표시",overlay);
            if(EditorApplication.isPlaying)EditorGUILayout.HelpBox("Play에서는 편집하지 않습니다.",MessageType.Warning);
        }
        void Draw(SceneView view)
        {
            if(EditorApplication.isPlaying)return;
            var map=Object.FindFirstObjectByType<MapRoot>();if(map==null)return;
            var road=map.GetComponent<RoadGrid>();if(road==null)return;
            if(overlay && road.grades!=null)
                for(int y=0;y<road.height;y++)for(int x=0;x<road.width;x++)
                {
                    var cell=new Vector3Int(x,y,0);int g=road.GradeAt(cell);
                    bool b=map.blocked.HasTile(cell);if(g==0 && !b)continue;
                    Handles.color=b?new Color(1,.25f,.25f,.35f):new Color(.3f,1,.4f,.12f+.07f*g);
                    Handles.DrawWireCube(map.CellCenter(cell),Vector3.one*.94f);
                }
            if(!painting)return;
            var e=Event.current;int control=GUIUtility.GetControlID(FocusType.Passive);
            if(e.type==EventType.Layout)HandleUtility.AddDefaultControl(control);
            var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);var plane=new Plane(Vector3.forward,map.transform.position);
            if(!plane.Raycast(ray,out float distance))return;
            var target=map.WorldToCell(ray.GetPoint(distance));
            if(target.x<0||target.y<0||target.x>=road.width||target.y>=road.height)return;
            Handles.color=Color.yellow;Handles.DrawWireCube(map.CellCenter(target),Vector3.one);
            if(e.alt || e.button!=0 || (e.type!=EventType.MouseDown && e.type!=EventType.MouseDrag))return;
            if(collision)
            {
                Undo.RecordObject(map.blocked,"모듈 충돌 편집");
                var tile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/PocketCaravan/Art/NinjaAdventure/TileAssets/_blocker.asset");
                map.blocked.SetTile(target,blocked?tile:null);EditorUtility.SetDirty(map.blocked);
            }
            else
            {
                Undo.RecordObject(road,"모듈 노면 편집");
                if(road.grades==null||road.grades.Length!=road.width*road.height)road.grades=new int[road.width*road.height];
                road.grades[target.y*road.width+target.x]=grade;EditorUtility.SetDirty(road);
            }
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);e.Use();view.Repaint();
        }
    }
}
