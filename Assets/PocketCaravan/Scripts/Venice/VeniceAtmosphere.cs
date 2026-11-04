using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan
{
    /// <summary>
    /// 베네치아·본토 서부 날씨와 밤 축제 (2026-10-26, 제안). 보이기만 하고 이동·충돌·저장은 바꾸지 않는다(카니발 덤만 돈에 더함).
    /// - 날씨: 게임 시간 4시간마다 바뀐다. 맑음이 잦고, 아침(5~11시)에는 석호 안개, 가끔 비. 화면 위에 옅은 안개 띠·빗줄기.
    /// - 카니발 밤: 7일마다 하루(날짜 % 7 == 5) 19~23시 리알토. 색종이가 흩날리고 가면 쓴 사람들이 광장을 돈다.
    ///   (2026-11-04: 덤은 공용 NightFestival) 그동안 리알토 장터에서 유리그릇·유리 장식·레이스를 팔면 판 값의 3할을 더 받는다(GameEvents.ItemSold).
    /// 공용 AmbientFX(꽃잎)는 그대로 둔다.
    /// </summary>
    public class VeniceAtmosphere : MonoBehaviour
    {
        public enum Weather { Clear, Fog, Rain }
        Transform carnival;
        string lastWeatherKey;

        public static Weather Now
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null) return Weather.Clear;
                int h = gm.Clock.Hour;
                int roll = Mathf.Abs((gm.Clock.day * 7919 + (h / 4) * 104729) % 10);
                if (roll >= 6 && roll <= 7 && h >= 5 && h < 11) return Weather.Fog;
                if (roll >= 8) return Weather.Rain;
                return Weather.Clear;
            }
        }

        public static bool CarnivalNight
        {
            get
            {
                var gm = GameManager.I;
                return gm != null && gm.Clock.day % 7 == 5 && gm.Clock.Hour >= 19 && gm.Clock.Hour < 23;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (GameManager.I != null && GameManager.I.GetComponent<VeniceAtmosphere>() == null)
                GameManager.I.gameObject.AddComponent<VeniceAtmosphere>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) { lastWeatherKey = null; }
        // 2026-11-04 카니발 덤(유리·레이스 3할)은 공용 NightFestival로 옮김


        void Update()
        {
            if (!VeniceUtil.InVenice) { if (carnival != null) Destroy(carnival.gameObject); return; }
            // 날씨가 바뀌면 한 번 알림
            var gm = GameManager.I;
            string key = gm != null ? gm.Clock.day + ":" + gm.Clock.Hour / 4 + ":" + Now : null;
            if (key != null && key != lastWeatherKey)
            {
                if (lastWeatherKey != null && Now != Weather.Clear) Notify.Show(Now == Weather.Fog ? "석호에 안개가 깔렸다" : "비가 내리기 시작했다", 3f);
                lastWeatherKey = key;
            }
            // 카니발: 리알토 광장에 가면 쓴 사람들
            bool show = CarnivalNight && VeniceUtil.Map == "venice_rialto";
            if (show && carnival == null) SpawnCarnival();
            if (!show && carnival != null) { Destroy(carnival.gameObject); carnival = null; }
            if (carnival != null)
            {
                int i = 0;
                foreach (Transform t in carnival)
                {
                    t.localPosition = new Vector3(Mathf.Cos(Time.time * .8f + i * 1.6f) * 2.2f, Mathf.Sin(Time.time * .8f + i * 1.6f) * 1.2f + Mathf.Abs(Mathf.Sin(Time.time * 6f + i)) * .15f, 0f);
                    i++;
                }
            }
        }

        static Texture2D fogTex;

        /// <summary>세로로 가운데가 가장 짙고 위아래로 사라지는 안개 띠 그림 (1×32)</summary>
        static Texture2D FogTexture()
        {
            var tex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 32; y++)
            {
                float a = Mathf.Sin(Mathf.PI * y / 31f);
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, a * a * .32f));
            }
            tex.Apply();
            return tex;
        }

        void SpawnCarnival()
        {
            var data = VeniceData.Get(VeniceUtil.Map);
            var spots = data != null ? VeniceData.Points(data.pigeons) : null;
            if (spots == null || spots.Count == 0) return;
            carnival = new GameObject("카니발").transform;
            carnival.position = VeniceUtil.Cell(spots[spots.Count / 2].x, spots[spots.Count / 2].y);
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject("가면 쓴 사람");
                go.transform.SetParent(carnival, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = VeniceUtil.Sprite("mask");
                r.sortingOrder = 11;
                r.transform.localScale = Vector3.one * 1.5f;
            }
            Notify.Show("카니발 밤! 리알토에서 유리·레이스를 3할 더 쳐 준다", 5f);
        }

        void OnGUI()
        {
            if (!VeniceUtil.InVenice || UIState.Blocking && GameManager.I != null && GameManager.I.IsTravelling) return;
            var w = Now;
            bool confetti = carnival != null;
            if (w == Weather.Clear && !confetti) return;
            UIStyle.Begin();
            GUI.depth = 5;
            float vw = UIStyle.VirtualWidth, vh = UIStyle.VirtualHeight, t = Time.time;
            var c = GUI.color;
            if (w == Weather.Fog)
            {
                // 위아래가 부드럽게 사라지는 안개 띠 (경계가 딱 떨어지는 사각형이면 줄무늬처럼 보인다)
                if (fogTex == null) fogTex = FogTexture();
                for (int i = 0; i < 4; i++)
                {
                    float y = Mathf.Repeat(i * 97f + t * 2f, vh + 120f) - 60f;
                    GUI.color = new Color(.92f, .96f, .98f, .5f);
                    GUI.DrawTexture(new Rect(-10f, y, vw + 20f, 90f), fogTex, ScaleMode.StretchToFill);
                }
            }
            else if (w == Weather.Rain)
            {
                GUI.color = new Color(.2f, .25f, .35f, .12f);
                GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);
                GUI.color = new Color(.75f, .85f, .95f, .55f);
                for (int i = 0; i < 70; i++)
                {
                    float x = Mathf.Repeat(i * 53.7f + t * 40f, vw + 20f) - 10f;
                    float y = Mathf.Repeat(i * 97.3f + t * 260f, vh + 20f) - 10f;
                    GUI.DrawTexture(new Rect(x, y, 1f, 6f), Texture2D.whiteTexture);
                }
            }
            if (confetti)
            {
                var cols = new[] { new Color(.9f, .3f, .35f), new Color(.98f, .82f, .3f), new Color(.3f, .55f, .9f), new Color(.45f, .8f, .45f) };
                for (int i = 0; i < 40; i++)
                {
                    float x = Mathf.Repeat(i * 41.3f + Mathf.Sin(t + i) * 12f, vw);
                    float y = Mathf.Repeat(i * 23.9f + t * (30f + i % 5 * 6f), vh + 10f) - 5f;
                    GUI.color = cols[i % cols.Length];
                    GUI.DrawTexture(new Rect(x, y, 3f, 2f), Texture2D.whiteTexture);
                }
            }
            GUI.color = c;
        }
    }
}
