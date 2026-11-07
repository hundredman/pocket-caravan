using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>ATM-B4 approved well activity. Existing art and public memo/save format only.</summary>
    public sealed class GoryeoDailyActivity : Interactable
    {
        [Serializable] public class Site
        {
            public string status, mapId, id, memoKey, name;
            public int width, height, x, y, reward;
            public float seconds;
        }
        public Site site;
        MapRoot ownerMap;
        GameManager owner;
        PlayerController actor;
        string slot;
        int startedDay;
        float elapsed;
        bool working;
        bool approved;
        public bool Working => working;
        public float Progress => site != null ? Mathf.Clamp01(elapsed / site.seconds) : 0f;
        public override string DisplayName => "우물";
        public override string ActionVerb => "물 긷기";
        public bool DoneToday => GameManager.I != null && site != null && GameManager.I.GetMemo(site.memoKey, -1) >= GameManager.I.Clock.day;

        void Awake() => blocksMovement = false; // Existing map already owns the well's collision.
        bool Allowed(PlayerController player)
        {
            var gm = GameManager.I;
            return isActiveAndEnabled && ownerMap != null && MapRoot.Current == ownerMap
                && (approved ? !ownerMap.previewOnly : ownerMap.previewOnly) && gm != null
                && (approved || !gm.AutosaveEnabled) && !gm.IsTravelling
                && player != null && player == PlayerController.Instance && player.isActiveAndEnabled
                && player.gameObject.scene == gameObject.scene && player.Motor != null
                && Vector2.Distance(player.transform.position, transform.position) <= player.talkRange + .01f;
        }

        public override void Interact(PlayerController player)
        {
            if (working || UIState.Blocking || !Allowed(player)) return;
            if (DoneToday)
            {
                DialogueUI.Show(site.name, "오늘 쓸 물은 길어 두었어요.\n내일 다시 들러 보세요.");
                return;
            }
            DialogueUI.Show(site.name, "두레박을 내려 마을에서 쓸 물을 길어요.\n하루 한 번 · 사례비 "+site.reward+"냥",
                new DialogueUI.Choice("물 긷기", () => Begin(player)),
                new DialogueUI.Choice("다음에", null));
        }

        public bool Begin(PlayerController player)
        {
            if (working || UIState.Blocking || !Allowed(player) || DoneToday) return false;
            var gm = GameManager.I;
            if (site.reward < 0 || gm.Player.money > int.MaxValue - site.reward) return false;
            if (approved && !Checkpoint(gm))
            {
                DialogueUI.Show(site.name, "진행을 저장하지 못했어요.\n잠시 뒤 다시 들러 주세요.");
                return false;
            }
            owner = gm; actor = player; slot = SaveSystem.FileName;
            startedDay = owner.Clock.day; elapsed = 0; working = true;
            UIState.Blocking = true;
            return true;
        }

        void Update()
        {
            if (!working) return;
            if (!Allowed(actor) || owner != GameManager.I || slot != SaveSystem.FileName
                || owner.Clock.day != startedDay || !UIState.Blocking || DoneToday)
            { Cancel(); return; }
            if (InputReader.CancelPressed()) { Cancel(); return; }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < site.seconds) return;
            if (owner.Player.money > int.MaxValue - site.reward) { Cancel(); return; }
            int beforeMoney = owner.Player.money;
            float beforeDay = owner.GetMemo(site.memoKey, -1);
            owner.SetMemo(site.memoKey, startedDay);
            owner.Player.money += site.reward;
            bool saved = !approved || Checkpoint(owner);
            if (!saved)
            {
                owner.Player.money = beforeMoney;
                owner.SetMemo(site.memoKey, beforeDay);
                Release();
                DialogueUI.Show(site.name, "저장하지 못해 사례비를 받지 않았어요.\n오늘 물 긷기는 다시 할 수 있어요.");
                return;
            }
            Release();
            DialogueUI.Show(site.name, "마을에서 쓸 물을 길었습니다.\n사례비 +"+site.reward+"냥");
        }

        void Release()
        {
            if (!working) return;
            working = false;
            if (owner != null && owner == GameManager.I && MapRoot.Current == ownerMap && !owner.IsTravelling) UIState.Release();
            owner = null; actor = null; slot = null;
        }
        public void Cancel() => Release();
        protected override void OnDisable() { Cancel(); base.OnDisable(); }

        static bool Checkpoint(GameManager gm)
        {
            if (gm == null || !gm.AutosaveEnabled) return false;
            try { return gm.SaveNow(); }
            catch (Exception e) { Debug.LogWarning("[물 긷기] 저장하지 못했습니다: " + e.GetType().Name); return false; }
        }

        void OnGUI()
        {
            if (!working) return;
            UIStyle.Begin();
            GUI.depth = -11;
            float w = Mathf.Min(300, UIStyle.VirtualWidth - 16);
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, UIStyle.VirtualHeight - 110, w, 98);
            CuteUI.Card(r);
            CuteUI.LabelCenter(r.center.x,r.y+19,"우물 · 물 긷기",CuteUI.Text);
            CuteUI.LabelCenter(r.center.x,r.y+42,"두레박을 천천히 끌어 올려요.",CuteUI.Sub,true);
            CuteUI.Bar(new Rect(r.x + 22, r.y + 58, w - 44, 8), Progress, CuteUI.Interact, CuteUI.InteractHi);
            if (GUI.Button(new Rect(r.x + w / 2 - 52, r.y + 72, 104, 19), "그만두기 · Esc", CuteUI.PaperButton)) Cancel();
        }

        static bool Valid(Site value, string status)
        {
            return value != null && value.status == status && value.mapId == "Naru"
                && value.id == "naru_water" && value.memoKey == "goryeo:daily:water_day"
                && ((value.width == 88 && value.height == 84 && value.x == 43 && value.y == 35)
                    || (GoryeoBoundaryLayout.Matches(value.mapId,value.width,value.height) && value.x>=0 && value.x<value.width && value.y>=0 && value.y<value.height))
                && value.seconds == 3 && (status == "approved" ? value.reward == 3 : value.reward >= 0 && value.reward <= 3);
        }
        static GoryeoDailyActivity Install(Site value, bool production)
        {
            var map = MapRoot.Current;
            if (map == null || map.mapId != value.mapId) return null;
            var road = map.GetComponent<RoadGrid>();
            if (road == null || road.width != value.width || road.height != value.height) return null;
            var well = new Vector3Int(value.x, value.height-value.y-1, 0);
            if (map.IsWalkable(well) || !map.IsWalkable(well+Vector3Int.down)
                || MapExit.At(well+Vector3Int.down) != null) return null;
            foreach (var existing in map.GetComponentsInChildren<GoryeoDailyActivity>(true))
                if (existing.site != null && existing.site.id == value.id) return existing;
            var go = new GameObject(production ? "Daily well" : "Daily well review"); go.transform.SetParent(map.transform, false);
            go.transform.position = map.CellCenter(well);
            var activity = go.AddComponent<GoryeoDailyActivity>();
            activity.site = value; activity.ownerMap = map; activity.approved = production;
            return activity;
        }
        public static GoryeoDailyActivity InstallApproved(Site value)
        {
            if (!Valid(value, "approved") || MapRoot.Current == null || MapRoot.Current.previewOnly || GameManager.I == null) return null;
            return Install(value, true);
        }
#if UNITY_EDITOR
        public static GoryeoDailyActivity InstallReview(Site value)
        {
            var map = MapRoot.Current; var gm = GameManager.I;
            if (gm == null || gm.AutosaveEnabled || map == null || !map.previewOnly)
                throw new InvalidOperationException("Explicit preview map and saves OFF required");
            if (!Valid(value, "review")) throw new ArgumentException("Invalid daily well trial");
            return Install(value, false);
        }
#endif
    }
}
