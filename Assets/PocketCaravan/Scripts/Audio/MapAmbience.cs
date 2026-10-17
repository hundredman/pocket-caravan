using UnityEngine;

namespace PocketCaravan
{
    /// <summary>데이터로 배치한 물가 주변음. 메뉴·음소거와 표현만 연결하며 게임 상태를 바꾸지 않는다.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MapAmbience : MonoBehaviour
    {
        public string clipName = "river";
        public float radius = 10f;
        public float volume = .18f;
        AudioSource source;

        void Start()
        {
            source = GetComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>("Audio/Sfx/" + clipName);
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (source.clip != null) source.Play();
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float dt)
        {
            if (source == null) return;
            var player = PlayerController.Instance;
            float distance = player != null ? Vector2.Distance(player.Motor.Feet, transform.position) : radius;
            float target = AudioManager.Muted ? 0f : volume * Mathf.Clamp01(1f - distance / Mathf.Max(1f, radius));
            source.volume = Mathf.MoveTowards(source.volume, target, dt * .3f);
        }
    }
}
