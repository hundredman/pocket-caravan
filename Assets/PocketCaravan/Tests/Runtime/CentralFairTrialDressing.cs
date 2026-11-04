#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    /// <summary>Approval-only fair decoration. Never attached to production scenes.</summary>
    public sealed class CentralFairTrialDressing:MonoBehaviour
    {
        public Sprite[] variants;
        readonly List<SpriteRenderer> targets=new List<SpriteRenderer>();
        readonly List<Sprite> originals=new List<Sprite>();
        bool? previous;
        public int Count=>targets.Count;
        void Start()
        {
            if(!MapRoot.Current.previewOnly)throw new System.InvalidOperationException("Trial requires previewOnly");
            foreach(var sr in MapRoot.Current.GetComponentsInChildren<SpriteRenderer>())
                if(sr.name=="grain_stall"){targets.Add(sr);originals.Add(sr.sprite);}
            Update();
        }
        void Update()
        {
            if(GameManager.I==null||MapRoot.Current==null)return;
            bool fair=CentralFair.IsFairMap(MapRoot.Current.mapId);if(previous==fair)return;
            for(int i=0;i<targets.Count;i++)targets[i].sprite=fair?variants[i%variants.Length]:originals[i];
            previous=fair;
        }
    }
}
#endif
