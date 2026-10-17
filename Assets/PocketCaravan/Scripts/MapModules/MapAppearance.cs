using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    /// <summary>표현 전용. Blocked/RoadGrid/출구/생산/경제 데이터에 접근하지 않는다.</summary>
    public class MapAppearance : MonoBehaviour
    {
        public EnvironmentSkin environment;
        public WeatherSkin weather;
        public Tilemap[] visualLayers;
        public int LastUnmapped { get; private set; }
        class Snapshot { public Tilemap map; public BoundsInt bounds; public TileBase[] tiles; public Color color; }
        readonly List<Snapshot> snapshots=new List<Snapshot>();
        readonly List<Object> generated=new List<Object>();
        WeatherOverlay overlay;
        void Start() { if(snapshots.Count==0) Apply(); }
        public void Capture()
        {
            if(visualLayers==null || visualLayers.Length==0)
            {
                var layers=new List<Tilemap>();
                foreach(var name in new[]{"Ground","Deco","Objects","Above"})
                {
                    var child=transform.Find(name);if(child!=null && child.TryGetComponent<Tilemap>(out var layer))layers.Add(layer);
                }
                visualLayers=layers.ToArray();
            }
            snapshots.Clear();
            foreach(var map in visualLayers)
                if(map!=null) snapshots.Add(new Snapshot{map=map,bounds=map.cellBounds,tiles=map.GetTilesBlock(map.cellBounds),color=map.color});
        }
        public void Restore()
        {
            foreach(var s in snapshots) if(s.map!=null) { s.map.SetTilesBlock(s.bounds,s.tiles);s.map.color=s.color; }
            snapshots.Clear();ReleaseGenerated();
        }
        void ReleaseGenerated()
        {
            foreach(var obj in generated) if(obj!=null) Destroy(obj);
            generated.Clear();
        }
        [ContextMenu("스킨 다시 적용 (Play)")]
        public void Apply()
        {
            if(!Application.isPlaying) return; // Runtime tiles must never be serialized into an editable scene.
            if(snapshots.Count==0)Capture();
            ReleaseGenerated();LastUnmapped=0;
            var names=new Dictionary<string,int>();
            if(environment!=null && environment.tileNames!=null)
                for(int i=0;i<environment.tileNames.Length;i++) names[environment.tileNames[i]]=i;
            var custom=new Dictionary<TileBase,TileBase>();
            if(environment!=null)
                foreach(var rule in environment.overrides) if(rule!=null && rule.source!=null && rule.replacement!=null)custom[rule.source]=rule.replacement;
            var cache=new Dictionary<TileBase,TileBase>();
            foreach(var s in snapshots)
            {
                var output=new TileBase[s.tiles.Length];
                for(int i=0;i<s.tiles.Length;i++)
                {
                    var source=s.tiles[i]; if(source==null) continue;
                    if(cache.TryGetValue(source,out var known)){output[i]=known;continue;}
                    TileBase result=source;
                    if(custom.TryGetValue(source,out var replacement)) result=replacement;
                    else if(environment!=null && environment.atlas!=null && source is Tile original && names.TryGetValue(source.name,out int index))
                    {
                        int size=environment.tileSize,cols=Mathf.Max(1,environment.columns);
                        int x=index%cols*size,y=environment.atlas.height-(index/cols+1)*size;
                        if(x+size<=environment.atlas.width && y>=0)
                        {
                            var sprite=Sprite.Create(environment.atlas,new Rect(x,y,size,size),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
                            sprite.name=source.name;generated.Add(sprite);
                            var tile=ScriptableObject.CreateInstance<Tile>();tile.name=source.name;
                            tile.sprite=sprite;tile.color=original.color;tile.transform=original.transform;tile.flags=original.flags;tile.colliderType=original.colliderType;
                            generated.Add(tile);result=tile;
                        }
                    }
                    else if(environment!=null)LastUnmapped++;
                    output[i]=result;cache[source]=result;
                }
                s.map.SetTilesBlock(s.bounds,output);s.map.RefreshAllTiles();
            }
            SetWeather(weather);
        }
        public void SetWeather(WeatherSkin value)
        {
            weather=value;
            foreach(var s in snapshots) if(s.map!=null)s.map.color=s.color*(weather!=null?weather.sceneTint:Color.white);
            if(overlay==null)overlay=gameObject.GetComponent<WeatherOverlay>() ?? gameObject.AddComponent<WeatherOverlay>();
            overlay.Configure(weather);
        }
        void OnDestroy(){ReleaseGenerated();}
    }
}
