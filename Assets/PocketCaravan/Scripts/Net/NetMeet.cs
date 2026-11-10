using System.Collections.Generic;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>
    /// 어울리기 창 (2026-11-09, 11/10 다시 그림): 다른 상인 앞에서 Space를 누르면 그 사람 머리 위에 말풍선 모양 카드가 뜬다.
    /// - 첫 장: [인사] [리액션] [선물] 세 칸. 숫자 1~3 · 방향키+Space · 마우스로 고른다. Esc = 닫기(안쪽 장에서는 뒤로).
    /// - 리액션 장: 박수 · 하트 · 춤 · 깡총. 선물 장: 짐에서 많이 가진 물건 넷 + 10냥.
    /// - 다른 플레이어(RemotePlayers)에게: 인사 = 채팅 한 줄 + 인사 동작, 리액션 = 같은 맵에 보이는 동작, 선물 = NetGift.
    ///   채팅 명령(/선물 이름 물건)을 몰라도 처음 온 사람이 Space만으로 어울릴 수 있게.
    /// - 만국 장터 상인회 상인(NetGuild가 세운 NPC)에게: 같은 세 가지를 혼자서도 한다. 상인이 말풍선으로 답하고 깡총 뛴다.
    /// 만국 상인회 수첩 체크는 NetGuild.Mark (리액션은 NetEmote.Sent로 NetGuild가 직접 듣는다).
    /// 공용 코드는 공개된 것만 쓴다 (UIState · InputReader · PlayerState.cargo · CuteUI).
    /// </summary>
    public class NetMeet : MonoBehaviour
    {
        public const int GiftMoney = 10;
        const int GiftChoices = 4;
        const float CardW = 180f;

        /// <summary>상인회 상인 한 명 (NetGuild가 채운다)</summary>
        public class Npc
        {
            public string name, shortName, hello, cheer, thanks, near;
            public Transform anchor;
            /// <summary>리액션·선물을 받으면 깡총</summary>
            public System.Action<string> react;
            /// <summary>머리 위 말풍선</summary>
            public System.Action<string> say;
        }

        enum Page { Main, Moves, Gift }
        static readonly (string id, string name, string mark, Color32 col)[] Moves =
        {
            ("clap", "박수", "★", CuteUI.Deco), ("love", "하트", "♥", CuteUI.Alert), ("dance", "춤", "♪", CuteUI.Buy), ("hop", "깡총", "!", CuteUI.Frame),
        };
        static readonly (string label, CuteUI.Pic pic)[] Mains = { ("인사", CuteUI.Pic.People), ("리액션", CuteUI.Pic.Star4), ("선물", CuteUI.Pic.Sack) };

        static NetMeet inst;
        bool open;
        Page page;
        int focus, openedFrame;
        float openedAt;
        string peerId, who;
        Npc npc;
        /// <summary>선물 장 목록 (item이 null이면 돈)</summary>
        readonly List<(ItemData item, int have)> gifts = new List<(ItemData, int)>();
        GUIStyle markStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<NetMeet>() != null) return;
            var go = new GameObject("NetMeet");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<NetMeet>();
        }

        void Awake() => inst = this;

        static PlayerState Player => GameManager.I != null ? GameManager.I.Player : null;

        public static void OpenPlayer(string id, string name) { if (inst != null) inst.Open(id, name, null); }
        public static void OpenNpc(Npc n) { if (inst != null && n != null) inst.Open(null, n.name, n); }

        void Open(string id, string name, Npc n)
        {
            if (string.IsNullOrEmpty(name) || !UIState.TryOpen(this, Close)) return;
            peerId = id; who = name; npc = n;
            open = true; page = Page.Main; focus = 0;
            openedFrame = Time.frameCount; openedAt = Time.unscaledTime;
            Sfx.Play("open", .5f);
        }

        void Close()
        {
            if (!open) return;
            open = false;
            UIState.Release(this);
        }

        void OnDisable() => Close();

        int Count => page == Page.Main ? Mains.Length : page == Page.Moves ? Moves.Length : gifts.Count;

        // ---------------- 고르기 ----------------

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            // 상대가 맵을 떠났으면 닫는다
            if (npc == null && (peerId == null || !RemotePlayers.TryGetPosition(peerId, out _))) { Close(); return; }
            if (npc != null && npc.anchor == null) { Close(); return; }
            if (InputReader.CancelPressed()) { if (page == Page.Main) Close(); else { page = Page.Main; focus = 0; Sfx.Play("cancel", .4f); } return; }
            int n = InputReader.NumberPressed();
            if (n > 0 && n <= Count) { Pick(n - 1); return; }
            var mv = InputReader.MovePressed();
            int step = page == Page.Gift ? -mv.y : mv.x != 0 ? mv.x : -mv.y;
            if (step != 0 && Count > 0) { focus = (focus + step + Count) % Count; Sfx.Play("move", .3f); }
            if (InputReader.InteractPressed()) Pick(focus);
        }

        void Pick(int i)
        {
            if (i < 0 || i >= Count) return;
            switch (page)
            {
                case Page.Main:
                    if (i == 0) { Greet(); Close(); }
                    else if (i == 1) { page = Page.Moves; focus = 0; Sfx.Play("open", .3f); }
                    else
                    {
                        BuildGifts();
                        if (gifts.Count == 0) { Close(); Notify.Show("줄 만한 물건도 돈도 없어요. 노점에서 하나 사 와요!", 3.5f); }
                        else { page = Page.Gift; focus = 0; Sfx.Play("open", .3f); }
                    }
                    break;
                case Page.Moves:
                {
                    var (id, name, _, _) = Moves[i];
                    Close();
                    Self(id, name);
                    if (npc != null) { npc.react?.Invoke(id); npc.say?.Invoke(npc.cheer); }
                    break;
                }
                case Page.Gift:
                {
                    var (item, _) = gifts[i];
                    Close();
                    if (npc != null) GiveNpc(item);
                    else NetGift.Give(who, item, 1, item == null ? GiftMoney : 0);
                    break;
                }
            }
        }

        /// <summary>내 캐릭터가 리액션을 하고 같은 맵 사람들에게 알린다</summary>
        static void Self(string id, string name)
        {
            NetEmote.Send(id, name);
            RemotePlayers.PlaySelf(id, name);
        }

        void Greet()
        {
            Self("bow", "인사");
            if (npc != null)
            {
                NetGuild.Mark(NetGuild.Task.Hello, npc.shortName);
                npc.react?.Invoke("bow");
                npc.say?.Invoke(npc.hello);
                return;
            }
            NetGuild.Mark(NetGuild.Task.Hello, who + " 님");
            var net = NetClient.I;
            if (net != null) net.Say($"{who} 님, 안녕하세요!");
        }

        void BuildGifts()
        {
            gifts.Clear();
            var p = Player;
            if (p == null) return;
            var items = new List<(ItemData, int)>();
            foreach (var kv in p.cargo.Items)
                if (kv.Key != null && !kv.Key.questItem && kv.Value > 0) items.Add((kv.Key, kv.Value));
            items.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            for (int i = 0; i < items.Count && i < GiftChoices; i++) gifts.Add(items[i]);
            if (p.money >= GiftMoney) gifts.Add((null, p.money));
        }

        void GiveNpc(ItemData item)
        {
            var p = Player;
            if (p == null) return;
            string label;
            if (item != null)
            {
                if (item.questItem || p.cargo.Count(item) < 1 || !p.cargo.Remove(item, 1)) { Notify.Show("짐에서 꺼내지 못했어요", 2.5f); return; }
                label = item.displayName + " 1개";
            }
            else
            {
                if (p.money < GiftMoney) { Notify.Show($"돈이 모자라요 (가진 돈 {p.money}냥)", 2.5f); return; }
                p.money -= GiftMoney;
                label = GiftMoney + "냥";
            }
            Sfx.Play("coin");
            Notify.Show($"{npc.shortName}에게 {Josa.Eul(label)} 건넸다", 3f);
            NetGuild.Mark(NetGuild.Task.Gift, npc.shortName);
            npc.react?.Invoke("love");
            npc.say?.Invoke(npc.thanks);
        }

        // ---------------- 그리기 ----------------

        void OnGUI()
        {
            if (!open) return;
            UIStyle.Begin();
            GUI.depth = -9;
            if (Event.current.type == EventType.Repaint)
            {
                var mp = Event.current.mousePosition;
                mouseMoved = (mp - lastMouse).sqrMagnitude > .25f && Time.frameCount > openedFrame + 1;
                lastMouse = mp;
            }
            if (markStyle == null || markStyle.font != UIStyle.RegularFont)
                markStyle = new GUIStyle(GUI.skin.label) { font = UIStyle.RegularFont, fontSize = UIStyle.BodySize + 4, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow, fontStyle = FontStyle.Bold };

            float h = page == Page.Main ? 98f : page == Page.Moves ? 90f : 34f + gifts.Count * 18f + 18f;
            // 11/10 너비는 글에 맞춰 넓힌다 (긴 이름·긴 물건 이름이 "Esc 닫기"·"가진 N개"와 겹치거나 잘리지 않게)
            string back = page == Page.Main ? "Esc 닫기" : "Esc 뒤로", kind = npc != null ? "만국 상인회" : "접속한 상인";
            float nameW = CuteUI.Width(who, false, true), backW = CuteUI.Width(back, true);
            float need = 9f + nameW + 8f + backW + 9f;
            if (page == Page.Gift)
                foreach (var (item, have) in gifts)
                    need = Mathf.Max(need, 6f + 16f + CuteUI.Width(item != null ? item.displayName + " 1개" : GiftMoney + "냥", false, true) + 12f
                                           + CuteUI.Width(item != null ? $"가진 {have}개" : $"가진 {have}냥", true) + 6f + 6f + (item == null ? 11f : 0f));
            float cardW = Mathf.Round(Mathf.Clamp(need, CardW, Mathf.Max(CardW, UIStyle.VirtualWidth - 12f)));
            // 카드는 상대 머리 위 (꼬리가 상대를 가리킨다). 화면 밖으로 나가면 안쪽으로
            Vector3 world = default; bool has = false;
            if (npc != null && npc.anchor != null) { world = npc.anchor.position + new Vector3(0f, 1.3f, 0f); has = true; }
            else if (peerId != null && RemotePlayers.TryGetPosition(peerId, out var pos)) { world = pos + new Vector3(0f, 1.75f, 0f); has = true; }
            Vector2 tail = new Vector2(UIStyle.VirtualWidth / 2f, UIStyle.VirtualHeight / 2f);
            if (has && NetGuildArt.ToGui(Camera.main, world, out var g)) tail = g;
            float x = Mathf.Round(Mathf.Clamp(tail.x - cardW / 2f, 6f, UIStyle.VirtualWidth - cardW - 6f));
            float y = Mathf.Round(Mathf.Clamp(tail.y - h - 4f, 34f, UIStyle.VirtualHeight - h - 60f));
            float pop = Mathf.Clamp01((Time.unscaledTime - openedAt) / .12f);
            var r = new Rect(x, y + Mathf.Round((1f - pop) * 6f), cardW, h);
            CuteUI.Bubble(r, Mathf.Clamp(tail.x, r.x + 12f, r.xMax - 12f), CuteUI.Interact);

            // 머리: 이름 + 어떤 사람인지 (어떤 사람인지는 자리가 남을 때만)
            float top = r.y + 6f;
            float nx = CuteUI.Label(r.x + 9f, top + 6f, who, CuteUI.Text, false, true);
            float backX = r.xMax - 9f - CuteUI.Width(back, true);
            if (nx + 5f + CuteUI.Width(kind, true) < backX - 4f) CuteUI.Label(nx + 5f, top + 6.5f, kind, CuteUI.Sub, true);   // 이름이 길면 빼고
            CuteUI.Label(backX, top + 6.5f, back, CuteUI.Sub, true);
            top += 15f;

            if (page == Page.Main) DrawMain(r, top);
            else if (page == Page.Moves) DrawMoves(r, top);
            else DrawGifts(r, top);
        }

        /// <summary>마우스를 움직여 올린 칸을 고른 칸으로, 누르면 고른다 (게임 화면엔 MouseMove 사건이 안 와서 자리 변화로 본다)</summary>
        void Tile(Rect t, int i)
        {
            if (mouseMoved && t.Contains(Event.current.mousePosition)) focus = i;
            if (GUI.Button(t, GUIContent.none, GUIStyle.none)) Pick(i);
        }

        Vector2 lastMouse;
        bool mouseMoved;

        void DrawMain(Rect r, float top)
        {
            float gap = 6f, tw = Mathf.Floor((r.width - 16f - gap * 2f) / 3f), th = 50f;
            for (int i = 0; i < Mains.Length; i++)
            {
                var t = new Rect(r.x + 8f + i * (tw + gap), top + 2f, tw, th);
                bool on = i == focus;
                CuteUI.Box(t, 7, on ? CuteUI.Paper2 : CuteUI.White, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1, 1, CuteUI.SoftShadow);
                var pic = Mains[i].pic; var ps = CuteUI.PicSize(pic);
                CuteUI.Icon(pic, t.center.x - ps.x, t.y + 15f - ps.y, 2);
                CuteUI.LabelCenter(t.center.x, t.y + 38f, Mains[i].label, on ? CuteUI.Text : CuteUI.Sub, false, on);
                CuteUI.Label(t.x + 4f, t.y + 6f, (i + 1).ToString(), CuteUI.KeyLine, true);
                Tile(t, i);
            }
            CuteUI.LabelCenter(r.center.x, r.yMax - 10f, "1~3 또는 Space로 고르기", CuteUI.Sub, true);
        }

        void DrawMoves(Rect r, float top)
        {
            float gap = 5f, tw = Mathf.Floor((r.width - 16f - gap * 3f) / 4f), th = 44f;
            for (int i = 0; i < Moves.Length; i++)
            {
                var t = new Rect(r.x + 8f + i * (tw + gap), top + 2f, tw, th);
                bool on = i == focus;
                CuteUI.Box(t, 7, on ? CuteUI.Paper2 : CuteUI.White, on ? CuteUI.Interact : CuteUI.Line, on ? 2 : 1, 1, CuteUI.SoftShadow);
                markStyle.normal.textColor = UIStyle.Lin(Moves[i].col);
                GUI.Label(new Rect(t.x, t.y + 4f, t.width, 20f), Moves[i].mark, markStyle);
                CuteUI.LabelCenter(t.center.x, t.y + 33f, Moves[i].name, on ? CuteUI.Text : CuteUI.Sub, true, on);
                CuteUI.Label(t.x + 3f, t.y + 6f, (i + 1).ToString(), CuteUI.KeyLine, true);
                Tile(t, i);
            }
            CuteUI.LabelCenter(r.center.x, r.yMax - 10f, npc != null ? "고르면 상인도 같이 반응해요" : "같은 맵 사람들에게 보여요", CuteUI.Sub, true);
        }

        void DrawGifts(Rect r, float top)
        {
            for (int i = 0; i < gifts.Count; i++)
            {
                var t = new Rect(r.x + 6f, top + i * 18f, r.width - 12f, 17f);
                bool on = i == focus;
                if (on) CuteUI.Box(t, 5, CuteUI.Paper2, CuteUI.Interact, 1);
                var (item, have) = gifts[i];
                CuteUI.Label(t.x + 5f, t.center.y, (i + 1).ToString(), CuteUI.KeyLine, true);
                if (item != null)
                {
                    CuteUI.Label(t.x + 16f, t.center.y, item.displayName + " 1개", CuteUI.Text, false, on);
                    CuteUI.LabelRight(t.xMax - 6f, t.center.y, $"가진 {have}개", CuteUI.Sub, true);
                }
                else
                {
                    CuteUI.Icon(CuteUI.Pic.Coin7, t.x + 16f, t.center.y - 3.5f);
                    CuteUI.Label(t.x + 27f, t.center.y, GiftMoney + "냥", CuteUI.Text, false, on);
                    CuteUI.LabelRight(t.xMax - 6f, t.center.y, $"가진 {have}냥", CuteUI.Sub, true);
                }
                Tile(t, i);
            }
            CuteUI.LabelCenter(r.center.x, r.yMax - 10f, npc != null ? "하나를 건네요" : "받는 사람 짐으로 바로 가요", CuteUI.Sub, true);
        }
    }
}
