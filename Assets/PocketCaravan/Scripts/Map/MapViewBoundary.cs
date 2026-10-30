using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>현재 맵의 표시 경계. 이웃 지형은 바깥 한 칸에서 가려지며 충돌·카메라는 바꾸지 않는다.</summary>
    public sealed class MapViewBoundary : MonoBehaviour
    {
        public int width,height;
        public int[] ownership;
        public Shader boundaryShader;
        Mesh mesh;Material material;
        static readonly Color Shade=new Color(28f/255,34f/255,40f/255).linear;
        static readonly Vector2Int[] Corners={Vector2Int.zero,Vector2Int.right,Vector2Int.one,Vector2Int.up};
        public bool Owns(int x,int y)=>x>=0&&y>=0&&x<width&&y<height&&ownership!=null&&ownership.Length==width*height&&ownership[(height-y-1)*width+x]!=0;
        public static bool InVisibleBand(int[] own,int w,int h,int x,int y)
        {
            if(own==null||own.Length!=w*h)return true;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {int xx=x+dx,yy=y+dy;if(xx>=0&&yy>=0&&xx<w&&yy<h&&own[yy*w+xx]!=0)return true;}
            return false;
        }
        float CornerAlpha(int x,int y)=>Owns(x,y)||Owns(x-1,y)||Owns(x,y-1)||Owns(x-1,y-1)?0f:1f;
        void Awake()
        {
            if(ownership==null||ownership.Length!=width*height){enabled=false;return;}
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();var uv=new List<Vector2>();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                if(Owns(x,y))continue;int n=vertices.Count;
                foreach(var offset in Corners)
                {int px=x+offset.x,py=y+offset.y;vertices.Add(new Vector3(px,py,-1));colors.Add(new Color(Shade.r,Shade.g,Shade.b,CornerAlpha(px,py)));uv.Add(Vector2.zero);}
                indices.Add(n);indices.Add(n+2);indices.Add(n+1);indices.Add(n);indices.Add(n+3);indices.Add(n+2);
            }
            mesh=new Mesh{name="Map view boundary - generated visibility only"};
            if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            var child=new GameObject("View boundary (no collision)");child.transform.SetParent(transform,false);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sortingOrder=100;
            material=new Material(boundaryShader!=null?boundaryShader:Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")){name="Map boundary visibility"};material.mainTexture=Texture2D.whiteTexture;renderer.sharedMaterial=material;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
