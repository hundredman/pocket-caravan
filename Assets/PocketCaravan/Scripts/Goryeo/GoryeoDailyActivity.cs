using System;
using UnityEngine;

namespace PocketCaravan
{
    /// <summary>ATM-B4 trial. No automatic installation; existing well art and public memo/save format only.</summary>
    public sealed class GoryeoDailyActivity : Interactable
    {
        [Serializable] public class Site
        {
            public string status, mapId, id, memoKey, name;
            public int width, height, x, y;
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
                && ownerMap.previewOnly && gm != null && !gm.AutosaveEnabled && !gm.IsTravelling
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
            DialogueUI.Show(site.name, "두레박을 내려 마을에서 쓸 물을 길어요.\n하루 한 번 · 돈이나 물건은 들지 않아요.",
                new DialogueUI.Choice("물 긷기", () => Begin(player)),
                new DialogueUI.Choice("다음에", null));
        }

        public bool Begin(PlayerController player)
        {
            if (working || UIState.Blocking || !Allowed(player) || DoneToday) return false;
            owner = GameManager.I; actor = player; slot = SaveSystem.FileName;
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
            owner.SetMemo(site.memoKey, startedDay);
            Release();
            DialogueUI.Show(site.name, "물을 길어 두었어요.\n우물가에 시원한 물소리가 맴돌아요.");
        }

        void Release()
        {
            if (!working) return;
            working = false;
            if (owner == GameManager.I && MapRoot.Current == ownerMap && !owner.IsTravelling) UIState.Release();
            owner = null; actor = null; slot = null;
        }
        public void Cancel() => Release();
        protected override void OnDisable() { Cancel(); base.OnDisable(); }

        void OnGUI()
        {
            if (!working) return;
            UIStyle.Begin();
            GUI.depth = -11;
            float w = Mathf.Min(300, UIStyle.VirtualWidth - 16);
            var r = new Rect((UIStyle.VirtualWidth - w) / 2, UIStyle.VirtualHeight - 110, w, 98);
            GUI.Box(r, "", UIStyle.Box);
            GUI.Label(new Rect(r.x + 12, r.y + 9, w - 24, 20), "우물 · 물 긷기", UIStyle.Center);
            GUI.Label(new Rect(r.x + 12, r.y + 32, w - 24, 20), "두레박을 천천히 끌어 올려요.", UIStyle.Center);
            UIStyle.Bar(new Rect(r.x + 22, r.y + 58, w - 44, 8), Progress, new Color32(109, 176, 204, 255));
            if (GUI.Button(new Rect(r.x + w / 2 - 44, r.y + 72, 88, 19), "그만두기 · Esc", UIStyle.Button)) Cancel();
        }

#if UNITY_EDITOR
        public static GoryeoDailyActivity InstallReview(Site value)
        {
            var map = MapRoot.Current; var gm = GameManager.I;
            if (gm == null || gm.AutosaveEnabled || map == null || !map.previewOnly)
                throw new InvalidOperationException("Explicit preview map and saves OFF required");
            if (value == null || value.status != "review" || value.mapId != "Naru" || map.mapId != value.mapId
                || value.id != "naru_water" || value.memoKey != "goryeo:daily:water_day"
                || value.width != 88 || value.height != 84 || value.x != 43 || value.y != 35
                || value.seconds < 1 || value.seconds > 10 || float.IsNaN(value.seconds))
                throw new ArgumentException("Invalid daily well trial");
            var road = map.GetComponent<RoadGrid>();
            if (road == null || road.width != value.width || road.height != value.height)
                throw new ArgumentException("Well map dimensions differ");
            foreach (var existing in map.GetComponentsInChildren<GoryeoDailyActivity>(true))
                if (existing.site != null && existing.site.id == value.id) return existing;
            var go = new GameObject("Daily well review"); go.transform.SetParent(map.transform, false);
            go.transform.position = map.CellCenter(new Vector3Int(value.x, value.height - value.y - 1, 0));
            var activity = go.AddComponent<GoryeoDailyActivity>(); activity.site = value; activity.ownerMap = map;
            return activity;
        }
#endif
    }
}
