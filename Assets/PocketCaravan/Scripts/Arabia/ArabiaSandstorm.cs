using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 아라비아 맵(arabia_*)을 오갈 때의 이동 효과 (2026-11-07 기우). 북 눈보라·베 고양이 구멍·고려 전용 연출처럼 지역마다 한 벌.
    /// 오른쪽에서 모래바람 장막이 들쭉날쭉한 앞머리를 세우고 왼쪽으로 쓸고 지나간다 → 덮인 동안 종이 이름표와 낙타 발자국 →
    /// 새 맵에서는 장막 꼬리가 왼쪽으로 빠져나가며 열린다. 화면 표시만 하며 이동·시간·저장은 GameManager.Travel 그대로.
    /// 덮인 동안 움직이는 것은 발자국뿐이라 씬 불러오며 잠깐 멈춰도 끊겨 보이지 않는다.
    /// </summary>
    public class ArabiaSandstorm : TravelTransition
    {
        public override float CoverSeconds => 0.34f;
        public override float RevealSeconds => 0.4f;
        public override float MinHoldSeconds => 0.25f;

        static readonly Color Sand = new Color32(214, 178, 110, 255);
        static readonly Color SandDark = new Color32(186, 146, 84, 255);
        static readonly Color SandLight = new Color32(236, 208, 150, 255);
        static readonly Color Paper = new Color32(244, 236, 214, 255);
        static readonly Color Ink = new Color32(70, 48, 30, 255);
        static readonly Color Ribbon = new Color32(176, 72, 46, 255);

        const int Bands = 18;          // 장막 앞머리를 이루는 가로 띠 수
        static readonly float[] Lead = new float[Bands];
        static readonly float[] Grain = new float[48];
        static bool built;
        GUIStyle title;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterSelf() => Register(id => id.StartsWith("arabia_"), new ArabiaSandstorm());

        static void Build()
        {
            built = true;
            var rng = new System.Random(1107);
            for (int i = 0; i < Bands; i++) Lead[i] = (float)rng.NextDouble();
            for (int i = 0; i < Grain.Length; i++) Grain[i] = (float)rng.NextDouble();
        }

        public override void Draw(float cover, bool covering, string nextName, float heldFor)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!built) Build();
            GUI.matrix = Matrix4x4.identity;
            float sw = Screen.width, sh = Screen.height;
            float k = EaseInOut(Mathf.Clamp01(cover));
            float ragged = sw * 0.28f;                     // 앞머리 들쭉날쭉 폭
            float bandH = Mathf.Ceil(sh / Bands);

            if (cover >= .995f)
            {
                Box(new Rect(0, 0, sw, sh), Sand, 1f);
            }
            else if (covering)
            {
                // 장막이 오른쪽(sw)에서 왼쪽으로: 띠마다 앞머리 x가 조금씩 다르다
                float head = sw - k * (sw + ragged);
                for (int i = 0; i < Bands; i++)
                {
                    float x = head + Lead[i] * ragged;
                    var c = (i % 3 == 0) ? SandDark : (i % 3 == 1) ? Sand : SandLight;
                    Box(new Rect(x, i * bandH, sw - x + 4f, bandH + 1f), c, 1f);
                    // 앞머리 앞으로 날리는 모래알
                    Box(new Rect(x - 10f - Lead[i] * 26f, i * bandH + bandH * .4f, 8f + Lead[i] * 10f, 2f), SandLight, .9f);
                }
                Grains(head, sw, sh, k);
            }
            else
            {
                // 드러내기: 장막 꼬리가 왼쪽으로 빠져나간다 (k=1 다 덮임 → 0 다 열림)
                float tail = -ragged + k * (sw + ragged);
                for (int i = 0; i < Bands; i++)
                {
                    float x = tail + Lead[i] * ragged;
                    var c = (i % 3 == 0) ? SandDark : (i % 3 == 1) ? Sand : SandLight;
                    Box(new Rect(-4f, i * bandH, x + 4f, bandH + 1f), c, 1f);
                    Box(new Rect(x + 4f + Lead[i] * 20f, i * bandH + bandH * .6f, 8f + Lead[i] * 10f, 2f), SandLight, .9f);
                }
                Grains(0f, tail, sh, k);
            }

            // 덮인 동안·열리는 초반에 이름표
            float alpha = covering ? Mathf.InverseLerp(.75f, 1f, cover) : Mathf.InverseLerp(.55f, .95f, cover);
            if (alpha > 0f) Card(nextName, alpha, heldFor);
            GUI.color = Color.white;
        }

        /// <summary>장막 안쪽에 흩어진 모래알 줄무늬 (고정 자리라 멈춰도 티 안 남)</summary>
        static void Grains(float x0, float x1, float sh, float k)
        {
            if (x1 - x0 < 20f) return;
            for (int i = 0; i < Grain.Length; i += 2)
            {
                float gx = Mathf.Lerp(x0, x1, Grain[i]), gy = Grain[i + 1] * sh;
                Box(new Rect(gx, gy, 6f + Grain[i] * 14f, 2f), i % 4 == 0 ? SandDark : SandLight, .7f);
            }
        }

        void Card(string name, float alpha, float held)
        {
            if (string.IsNullOrEmpty(name)) return;
            UIStyle.Begin();
            float w = UIStyle.VirtualWidth, h = UIStyle.VirtualHeight;
            title ??= new GUIStyle(UIStyle.CenterRich) { fontSize = 16 };
            float bw = Mathf.Max(120f, title.CalcSize(new GUIContent(name)).x + 40f), bh = 28f;
            var r = new Rect(Mathf.Round(w / 2f - bw / 2f), Mathf.Round(h / 2f - bh / 2f), Mathf.Round(bw), bh);
            Box(new Rect(r.x - 10f, r.y + 7f, 14f, 14f), Ribbon, alpha);
            Box(new Rect(r.xMax - 4f, r.y + 7f, 14f, 14f), Ribbon, alpha);
            Box(new Rect(r.x + 2f, r.y, r.width - 4f, r.height), Paper, alpha);
            Box(new Rect(r.x, r.y + 2f, r.width, r.height - 4f), Paper, alpha);
            GUI.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            GUI.Label(r, name, title);
            // 낙타 발자국(둥근 발바닥 두 쪽)이 이름표 아래로 차례로 찍힌다
            int steps = Mathf.Clamp(Mathf.FloorToInt(held / .08f) + 1, 1, 4);
            for (int i = 0; i < steps; i++)
            {
                float px = w / 2f - 30f + i * 20f, py = r.yMax + 12f + (i % 2 == 0 ? 0f : 6f);
                Box(new Rect(px, py, 5f, 6f), Ink, alpha * .8f);
                Box(new Rect(px + 6f, py, 5f, 6f), Ink, alpha * .8f);
            }
            GUI.matrix = Matrix4x4.identity;
        }

        static void Box(Rect r, Color c, float a)
        {
            GUI.color = new Color(c.r, c.g, c.b, a);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }

        static float EaseInOut(float v) => v * v * (3f - 2f * v);
    }
}
