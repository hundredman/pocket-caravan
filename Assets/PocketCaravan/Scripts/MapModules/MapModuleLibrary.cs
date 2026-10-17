using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName="Pocket Caravan/Map Modules/Library")]
    public class MapModuleLibrary : ScriptableObject
    {
        [Serializable] public class Entry
        {
            public string id,label,category,environment;
            public string[] ports;
            public TextAsset layout,source;
            public Texture2D preview;
        }
        public List<Entry> entries=new List<Entry>();
        public List<EnvironmentSkin> environments=new List<EnvironmentSkin>();
        public List<WeatherSkin> weather=new List<WeatherSkin>();
        public List<TileBase> tiles=new List<TileBase>();
    }
    [Serializable] public class ModuleMapFile
    {
        public string id,name;public int w,h,hours;
        public int[] ground,deco,obj,high,road,blocked;public string[] palette;
        public ObstaclePlacement[] obstacles;
    }
}
