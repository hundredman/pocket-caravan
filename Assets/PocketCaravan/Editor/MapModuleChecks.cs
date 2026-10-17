using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan.EditorTools
{
    public static class MapModuleChecks
    {
        static int phase;static ModuleLabUI lab;static List<string> checks;static int[] roads;static bool[] occupied,blocked;
        [MenuItem("Pocket Caravan/Tests/환경 원형 Play 검사")]
        public static void Begin()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("ModulePreview 씬 Play에서 실행하세요.");
            lab=UnityEngine.Object.FindFirstObjectByType<ModuleLabUI>();if(lab==null || lab.lockLayout)throw new InvalidOperationException("원형 전환 시험장이 아닙니다.");
            if(GameManager.I.SaveNow())throw new Exception("시험장 저장이 활성화됐습니다.");
            EditorApplication.update-=Step;phase=0;checks=new List<string>{"시험장 저장 비활성"};EditorApplication.update+=Step;
        }
        static void Require(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
        static bool[] Cells(Tilemap map,int w,int h)
        {
            var cells=new bool[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)cells[y*w+x]=map.HasTile(new Vector3Int(x,y,0));return cells;
        }
        static void Step()
        {
            try
            {
                if(!EditorApplication.isPlaying || lab==null)throw new Exception("시험 중 Play/시험장이 종료됐습니다.");
                if(phase<lab.library.entries.Count)
                {
                    lab.Load(phase);var map=MapRoot.Current;var grid=RoadGrid.Current;
                    Require(lab.appearance.LastUnmapped==0,"원형 "+phase+" 모든 타일 스킨 매칭");
                    Require(map.IsWalkable(PlayerController.Instance.Motor.Cell),"원형 "+phase+" 실제 시작점 통행");
                    int available=0;
                    for(int y=0;y<grid.height;y++)for(int x=0;x<grid.width;x++)
                        if(grid.GradeAt(new Vector3Int(x,y,0))>0) { Require(map.IsWalkable(new Vector3Int(x,y,0)),"원형 "+phase+" 도로 통행 "+x+","+y);available++; }
                    Require(available>0,"원형 "+phase+" 도로 존재");phase++;return;
                }
                int n=phase-lab.library.entries.Count;
                if(n==0)
                {
                    roads=(int[])RoadGrid.Current.grades.Clone();int w=RoadGrid.Current.width,h=RoadGrid.Current.height;
                    occupied=Cells(MapRoot.Current.ground,w,h);blocked=Cells(MapRoot.Current.blocked,w,h);
                }
                if(n<lab.library.environments.Count)
                {
                    lab.SetEnvironment(n);int w=RoadGrid.Current.width,h=RoadGrid.Current.height;
                    Require(roads.SequenceEqual(RoadGrid.Current.grades),"환경 "+n+" 길 등급 불변");
                    Require(occupied.SequenceEqual(Cells(MapRoot.Current.ground,w,h)),"환경 "+n+" 지면 존재 불변");
                    Require(blocked.SequenceEqual(Cells(MapRoot.Current.blocked,w,h)),"환경 "+n+" 충돌 불변");
                    Require(lab.appearance.LastUnmapped==0,"환경 "+n+" 모든 타일 매칭");
                    foreach(var weather in lab.library.weather)
                    {
                        lab.appearance.SetWeather(weather);
                        Require(lab.appearance.GetComponent<WeatherOverlay>().ActiveParticleCount==weather.particleCount,"환경 "+n+" 날씨 "+weather.id+" 풀 개수");
                        Require(roads.SequenceEqual(RoadGrid.Current.grades) && blocked.SequenceEqual(Cells(MapRoot.Current.blocked,w,h)),"환경 "+n+" 날씨 "+weather.id+" 판정 불변");
                    }
                    phase++;return;
                }
                EditorApplication.update-=Step;lab.Load(lab.library.entries.FindIndex(e=>e.id=="desert_oasis"));lab.SetWeather(0);
                Directory.CreateDirectory("output/module-library");File.WriteAllText("output/module-library/unity-checks.json",JsonUtility.ToJson(new Report{passed=checks.Count,templates=lab.library.entries.Count,environments=lab.library.environments.Count,weather=lab.library.weather.Count,failed=""},true));
                Debug.Log("원형/스킨 Play 검사 통과: "+checks.Count);
            }
            catch(Exception e)
            {
                EditorApplication.update-=Step;Directory.CreateDirectory("output/module-library");File.WriteAllText("output/module-library/unity-checks.json",JsonUtility.ToJson(new Report{passed=checks.Count,failed=e.Message},true));Debug.LogException(e);
            }
        }
        [Serializable] class Report { public int passed,templates,environments,weather;public string failed; }
    }
}
