using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    [CreateAssetMenu(menuName = "Pocket Caravan/Region", fileName = "Region")]
    public class RegionData : ScriptableObject
    {
        public string id = "goryeo";
        public string displayName = "고려";
        [TextArea] public string note;
    }
}
