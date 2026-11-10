using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 만국 상인회 이야기(NetGuild)와 어울리기 창(NetMeet)이 쓰는 그림 조각 (2026-11-10).
    /// CuteUI 판·글씨 위에 도장 · 머리 위 화살표 · 화면 가장자리 화살표 · 말풍선 · 봉인 · 상인패 메달 · 꽃가루를 그린다.
    /// 모두 OnGUI 안에서 UIStyle.Begin() 뒤에 부른다 (가상 화면 좌표, 높이 360).
    /// </summary>
    public static class NetGuildArt
    {
        static readonly Color32 StampFill = new Color32(255, 61, 95, 34);
        static readonly Color32[] ConfettiColors = { CuteUI.Deco, CuteUI.Frame, CuteUI.Interact, CuteUI.Alert, CuteUI.Buy, CuteUI.Up, CuteUI.White };

        /// <summary>월드 좌표 → 가상 화면 좌표. 카메라 뒤면 false</summary>
        public static bool ToGui(Camera cam, Vector3 world, out Vector2 p)
        {
            p = default;
            if (cam == null) return false;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            float k = UIStyle.Scale;
            p = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
            return true;
        }

        public static bool OnScreen(Vector2 p, float margin) =>
            p.x >= margin && p.x <= UIStyle.VirtualWidth - margin && p.y >= margin && p.y <= UIStyle.VirtualHeight - margin;

        /// <summary>
        /// 빨간 "완료" 도장. 비스듬히(-14도) 찍히고, 찍은 직후 0.22초는 크게 내려앉는다. age = 찍은 뒤 지난 초
        /// </summary>
        public static void Stamp(float cx, float cy, float age)
        {
            var m = GUI.matrix; var c = GUI.color;
            float k = Mathf.Clamp01(age / 0.22f);
            float s = Mathf.Lerp(2.4f, 1f, k * k);
            var pivot = new Vector2(cx, cy) * UIStyle.Scale;   // 회전·크기 기준점은 실제 화면 픽셀 (WorldLabels 화살표와 같다)
            GUIUtility.ScaleAroundPivot(new Vector2(s, s), pivot);
            GUIUtility.RotateAroundPivot(-14f, pivot);
            GUI.color = new Color(c.r, c.g, c.b, c.a * Mathf.Lerp(.25f, 1f, k));
            CuteUI.Circle(cx - 11, cy - 11, 22, StampFill, CuteUI.Alert, 2);
            CuteUI.LabelCenter(cx, cy, "완료", CuteUI.Alert, true, true);
            GUI.matrix = m; GUI.color = c;
        }

        /// <summary>찾아갈 상대 머리 위에서 통통 튀는 노란 화살표 (p = 화살표 끝)</summary>
        public static void DownMarker(Vector2 p, float time)
        {
            float x = Mathf.Round(p.x), y = Mathf.Round(p.y - 7f - Mathf.Abs(Mathf.Sin(time * 5f)) * 3f);
            for (int i = 0; i < 6; i++) { float w = 11 - 2 * i; CuteUI.Fill(new Rect(x - (w - 1) / 2f, y + i, w, 1), CuteUI.Ink); }
            for (int i = 0; i < 4; i++) { float w = 7 - 2 * i; CuteUI.Fill(new Rect(x - (w - 1) / 2f, y + 1 + i, w, 1), CuteUI.Deco); }
        }

        /// <summary>상대가 화면 밖이면 그쪽 가장자리에 동그란 나침반 화살표</summary>
        public static void EdgeArrow(Vector2 target)
        {
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            var c = new Vector2(W / 2f, H / 2f);
            var d = target - c;
            if (d.sqrMagnitude < 1f) return;
            float tx = Mathf.Abs(d.x) > .01f ? ((d.x > 0 ? W - 26f : 26f) - c.x) / d.x : float.MaxValue;
            float ty = Mathf.Abs(d.y) > .01f ? ((d.y > 0 ? H - 60f : 44f) - c.y) / d.y : float.MaxValue;
            var a = c + d * Mathf.Min(tx, ty);
            a = new Vector2(Mathf.Round(a.x), Mathf.Round(a.y));
            CuteUI.FramedCircle(a.x - 11, a.y - 11, 22);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a * UIStyle.Scale);
            CuteUI.Icon(CuteUI.Pic.Arrow, a.x - 4, a.y - 3, 1, CuteUI.Interact);
            GUI.matrix = m;
        }

        /// <summary>말풍선 (p = 꼬리 끝, 말하는 사람 머리 위). 길면 두 줄로</summary>
        public static void Bubble(Vector2 p, string text, Color32? line = null)
        {
            if (string.IsNullOrEmpty(text)) return;
            float w = Mathf.Min(150f, CuteUI.Width(text, true) + 4f);
            float h = CuteUI.WrapHeight(text, w, true);
            var r = new Rect(Mathf.Round(p.x - w / 2f - 6f), Mathf.Round(p.y - h - 10f), Mathf.Round(w + 12f), Mathf.Round(h + 7f));
            CuteUI.Bubble(r, p.x, line ?? CuteUI.Ink);
            CuteUI.Wrap(r.x + 6f, r.y + 3f, w, text, CuteUI.Text, true);
        }

        /// <summary>편지 봉인 (빨간 밀랍 + 흰 별)</summary>
        public static void Seal(float cx, float cy)
        {
            CuteUI.Circle(cx - 12, cy - 12, 24, CuteUI.Alert, CuteUI.Ink, 1, 2);
            CuteUI.Circle(cx - 9, cy - 9, 18, CuteUI.Alert, new Color32(255, 140, 160, 255), 1);
            CuteUI.Icon(CuteUI.Pic.Star4, cx - 7, cy - 7, 2, CuteUI.White);
        }

        /// <summary>상인패 메달 (지름 64, top = 위 끝). 별이 천천히 숨쉰다</summary>
        public static void Medal(float cx, float top, float time)
        {
            float bob = Mathf.Round(Mathf.Sin(time * 3f) * 1.5f);
            CuteUI.Circle(cx - 32, top, 64, CuteUI.Frame, CuteUI.Ink, 2, 3);
            CuteUI.Circle(cx - 26, top + 6, 52, CuteUI.Deco, CuteUI.FrameLo, 2);
            CuteUI.Icon(CuteUI.Pic.Star4, cx - 14, top + 18 + bob, 4);
            var c = GUI.color;
            float tw = .5f + .5f * Mathf.Sin(time * 5f);
            GUI.color = new Color(c.r, c.g, c.b, c.a * tw);
            CuteUI.Icon(CuteUI.Pic.Star4, cx - 50, top + 10, 1);
            GUI.color = new Color(c.r, c.g, c.b, c.a * (1f - tw));
            CuteUI.Icon(CuteUI.Pic.Star4, cx + 42, top + 42, 1);
            GUI.color = c;
        }

        // ---------------- 꽃가루 ----------------

        public struct Bit { public float x, y0, vy, amp, freq, phase, size; public Color32 col; }

        public static Bit[] MakeConfetti(int n)
        {
            var bits = new Bit[n];
            float W = UIStyle.VirtualWidth, H = UIStyle.VirtualHeight;
            for (int i = 0; i < n; i++)
                bits[i] = new Bit
                {
                    x = Random.Range(0f, W), y0 = Random.Range(-H * .7f, -6f), vy = Random.Range(55f, 120f),
                    amp = Random.Range(4f, 14f), freq = Random.Range(2f, 5f), phase = Random.Range(0f, 6.3f),
                    size = Random.Range(2, 5), col = ConfettiColors[Random.Range(0, ConfettiColors.Length)],
                };
            return bits;
        }

        /// <summary>위에서 흩날려 떨어지는 꽃가루 (t = 시작한 뒤 지난 초, 4.5초 뒤 사라진다)</summary>
        public static void DrawConfetti(Bit[] bits, float t)
        {
            if (bits == null || t > 4.5f) return;
            var c = GUI.color;
            float alpha = t > 3.5f ? 4.5f - t : 1f;
            foreach (var b in bits)
            {
                float y = b.y0 + b.vy * t;
                if (y < -6f || y > UIStyle.VirtualHeight) continue;
                float x = b.x + Mathf.Sin(t * b.freq + b.phase) * b.amp;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                bool flat = Mathf.Sin(t * b.freq * 2f + b.phase) > 0f;   // 뒤집히며 납작해진다
                CuteUI.Fill(new Rect(x, y, b.size, flat ? Mathf.Max(1f, b.size - 2f) : b.size), b.col);
            }
            GUI.color = c;
        }
    }
}
