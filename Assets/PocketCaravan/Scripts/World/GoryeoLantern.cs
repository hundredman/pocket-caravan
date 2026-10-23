using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PocketCaravan
{
    /// <summary>Local atmosphere only; lighting follows the existing world clock.</summary>
    public sealed class GoryeoLantern : MonoBehaviour
    {
        public Light2D lamp;
        public float phase;

        void Update()
        {
            if (lamp == null) return;
            float daylight = GameManager.I == null ? 1f : GameManager.I.Clock.Daylight;
            float glow = Mathf.Lerp(.03f, .75f, 1f - daylight);
            lamp.intensity = glow * (.96f + .04f * Mathf.Sin(Time.time * 2.3f + phase));
        }
    }
}
