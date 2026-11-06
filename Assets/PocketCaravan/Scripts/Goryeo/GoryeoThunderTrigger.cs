using UnityEngine;
namespace PocketCaravan
{
    /// <summary>One attempt per eligible update; session/window history lives outside disposable map instances.</summary>
    public sealed class GoryeoThunderTrigger:MonoBehaviour
    {
        public GoryeoThunder effect;public int entryWindow;
        void Update()=>GoryeoThunderDirector.TryPlay(this);
    }
}
