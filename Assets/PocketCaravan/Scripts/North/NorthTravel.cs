using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 북유럽 이동 (10/24): 날씨와 순록 썰매가 걷는 속도에 주는 영향, 그리고 썰매 모습·방울 소리·눈보라 자국.
    /// 공용 코드는 고치지 않고, PlayerMotor.walkSpeed(기본 3.8칸/초)에 배율만 곱했다가 북유럽을 벗어나면 되돌린다.
    /// - 걷기: 함박눈 0.92배, 눈보라 0.8배 (중앙 구역은 날씨 자체가 약해짐)
    /// - 순록 썰매(north_sled): 함박눈 1배, 눈보라 0.94배 → "눈보라 속 순록 썰매". 눈 없는 땅에선 느려짐
    ///   (중앙 가운데 줄 0.9배, 북문 큰길 0.8배, 다른 나라 0.7배). 길 등급별 속도는 VehicleData가 맡음 (눈밭 1.45배)
    /// - 온천에서 몸을 녹이면(NorthSave "warm") 몇 시간 동안 날씨 영향 없음
    /// </summary>
    public class NorthTravel : MonoBehaviour
    {
        PlayerMotor motor;
        float baseSpeed;
        Transform sled;
        SpriteRenderer sledSr;
        AudioSource bells;
        Sprite puff;
        readonly System.Collections.Generic.List<(SpriteRenderer sr, float born)> puffs = new System.Collections.Generic.List<(SpriteRenderer, float)>();
        float puffAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NorthTravel>() != null) return;
            var go = new GameObject("NorthTravel");
            DontDestroyOnLoad(go);
            go.AddComponent<NorthTravel>();
        }

        void Awake()
        {
            bells = gameObject.AddComponent<AudioSource>();
            bells.clip = MakeBells(); bells.loop = true; bells.volume = 0f; bells.playOnAwake = false;
            puff = NorthUtil.Pixel(new Color32(250, 250, 255, 255), 2, 2);
        }

        public static bool OnSled => GameManager.I != null && GameManager.I.Player.vehicle != null && GameManager.I.Player.vehicle.id == "north_sled";

        public static bool Warm => GameManager.I != null && NorthSave.Get("warm") > GameManager.I.Clock.TotalHours;

        float Factor()
        {
            string map = NorthUtil.Map;
            bool north = NorthUtil.InNorth;
            float f = 1f;
            if (north && !Warm)
            {
                var w = NorthAtmosphere.Shown;
                if (OnSled) f *= w == NorthAtmosphere.Weather.Blizzard ? 0.94f : 1f;
                else f *= w == NorthAtmosphere.Weather.Blizzard ? 0.8f : w == NorthAtmosphere.Weather.Heavy ? 0.92f : 1f;
            }
            if (OnSled)
            {
                if (!north) f *= 0.7f;                                   // 눈 없는 다른 나라
                else if (map == "north_c_road_sw" || map == "north_c_road_se" || map == "north_c_crossing") f *= 0.8f;      // 눈 거의 녹은 아랫줄
                else if (map == "north_c_snowwood" || map == "north_c_creek" || map == "north_c_runeway") f *= 0.9f;
            }
            return f;
        }

        void Update()
        {
            var pc = PlayerController.Instance;
            if (pc == null || GameManager.I == null) { motor = null; return; }
            if (pc.Motor != motor) { motor = pc.Motor; baseSpeed = motor.walkSpeed; BuildSled(pc.transform); }
            motor.walkSpeed = baseSpeed * Factor();
            UpdateSled(pc);
        }

        void OnDisable() { if (motor != null) motor.walkSpeed = baseSpeed; }

        void BuildSled(Transform player)
        {
            if (sled != null) Destroy(sled.gameObject);
            var go = new GameObject("North Sled");
            go.transform.SetParent(player, false);
            sled = go.transform;
            sledSr = go.AddComponent<SpriteRenderer>();
            sledSr.sprite = NorthUtil.Load("cargo_v", new Vector2(0.5f, 0.5f));
            go.SetActive(false);
        }

        void UpdateSled(PlayerController pc)
        {
            bool show = OnSled && sledSr != null && sledSr.sprite != null && MapRoot.Current != null;
            if (sled != null) sled.gameObject.SetActive(show);
            float moving = motor != null ? motor.Velocity.magnitude : 0f;
            float target = show && moving > 0.3f && !UIState.Blocking && !AudioManager.Muted ? Mathf.Clamp01(moving / 5f) * 0.25f : 0f;
            bells.volume = Mathf.MoveTowards(bells.volume, target, Time.deltaTime * 0.8f);
            if (bells.volume > 0f && !bells.isPlaying) bells.Play();
            else if (bells.volume <= 0f && bells.isPlaying) bells.Stop();
            if (!show) { ClearPuffs(); return; }
            Vector2 facing = (Vector2)motor.Facing;
            sled.localPosition = -(Vector3)(facing * 0.95f) + new Vector3(0f, -0.15f, 0f);
            sled.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg);
            sledSr.sortingOrder = facing.y > 0 ? 12 : 8;
            // 눈 위를 달리면 썰매 뒤로 눈가루가 날린다
            if (NorthUtil.InNorth && moving > 1f && Time.time >= puffAt)
            {
                puffAt = Time.time + 0.06f;
                var go = new GameObject("Sled Puff");
                go.transform.position = sled.position - (Vector3)(facing * 0.4f) + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.1f), 0f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = puff; sr.sortingOrder = 7;
                puffs.Add((sr, Time.time));
            }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var (sr, born) = puffs[i];
                float age = Time.time - born;
                if (sr == null || age > 0.6f) { if (sr != null) Destroy(sr.gameObject); puffs.RemoveAt(i); continue; }
                sr.color = new Color(1f, 1f, 1f, 1f - age / 0.6f);
                sr.transform.position += new Vector3(0f, Time.deltaTime * 0.6f, 0f);
            }
        }

        void ClearPuffs()
        {
            foreach (var p in puffs) if (p.sr != null) Destroy(p.sr.gameObject);
            puffs.Clear();
        }

        /// <summary>썰매 방울 소리 (2초 반복): 높은 방울 두 개가 엇갈려 딸랑</summary>
        static AudioClip MakeBells()
        {
            const int rate = 22050; int n = rate * 2;
            var data = new float[n];
            float[] hits = { 0f, 0.25f, 0.5f, 0.8f, 1.0f, 1.25f, 1.5f, 1.8f };
            for (int h = 0; h < hits.Length; h++)
            {
                int start = (int)(hits[h] * rate);
                float f = h % 2 == 0 ? 2350f : 2790f;
                for (int i = 0; i < rate / 4 && start + i < n; i++)
                {
                    float t = (float)i / rate;
                    data[start + i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.7f * t)) * Mathf.Exp(-t * 14f) * 0.3f;
                }
            }
            var clip = AudioClip.Create("north_sled_bells", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
