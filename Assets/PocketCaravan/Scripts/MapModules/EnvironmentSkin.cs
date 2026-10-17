using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName="Pocket Caravan/Map Modules/Environment Skin")]
    public class EnvironmentSkin : ScriptableObject
    {
        public string id, displayName;
        public Texture2D atlas;
        public int columns=32, tileSize=16;
        [Tooltip("위에서 아래, 왼쪽에서 오른쪽 atlas 순서. 기존 Tile.name과 매칭")]
        public string[] tileNames;
        [Serializable] public class Override { public TileBase source, replacement; }
        [Tooltip("새 아트로 특정 타일을 바꾸는 우선 규칙. 길/충돌 데이터는 바꾸지 않음")]
        public List<Override> overrides=new List<Override>();
    }
}
