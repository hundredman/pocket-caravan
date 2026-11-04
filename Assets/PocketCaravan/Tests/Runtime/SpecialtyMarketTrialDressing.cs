#if UNITY_EDITOR
using UnityEngine;
namespace PocketCaravan.EditorTools
{
    public sealed class SpecialtyMarketTrialDressing:MonoBehaviour
    {
        public SpriteRenderer[] targets;public Sprite[] originals,variants;
        public void SetAfter(bool after){for(int i=0;i<targets.Length;i++)targets[i].sprite=after?variants[i]:originals[i];}
        void Start(){if(!GetComponent<MapRoot>().previewOnly)throw new System.InvalidOperationException("Trial requires previewOnly");SetAfter(true);}
    }
}
#endif
