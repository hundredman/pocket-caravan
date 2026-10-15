using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using PocketCaravan;

namespace CaravanMovementLab
{
    [DefaultExecutionOrder(-10000)]
    public sealed class MovementLab : MonoBehaviour
    {
        public PlayerController player;
        public VehicleData walk, cart;
        public ItemData cargoItem;
        public Sprite cartSprite, crateSprite;
        public Vector3 start = new Vector3(8.5f, 13.82f, 0);
        public bool Overview { get; private set; }
        public float TravelDistance { get; private set; }
        public float PeakShake { get; private set; }
        public int DroppedCount { get; private set; }
        public readonly List<string> Results = new List<string>();
        public int LoadPreset { get; private set; } = 5;
        public bool CartSelected => GameManager.I.Player.vehicle == cart;
        Camera view;
        CameraFollow follow;
        PixelPerfectCamera pixels;
        SpriteRenderer cartView;
        readonly List<SpriteRenderer> boxes = new List<SpriteRenderer>();
        Vector3 previous;
        int previousCount;
        readonly float[] terrainDistance = new float[4];
        bool arrived, ready, fromStart;
        string note = "R로 출발점 복귀 · 지형을 골라 도착 깃발까지 이동";

        void Awake()
        {
            GameManager.I.EnterTestSession();
        }

        void Start()
        {
            var carryVisual = player.GetComponent<CarryVisual>();
            if (carryVisual != null) carryVisual.enabled = false; // 시험장 자체의 수레 표시와 중복 방지
            view = Camera.main;
            follow = view.GetComponent<CameraFollow>();
            pixels = view.GetComponent<PixelPerfectCamera>();
            var light = FindAnyObjectByType<Light2D>();
            if (light != null) { light.color = Color.white; light.intensity = 1f; }
            var go = new GameObject("Pulled handcart");
            cartView = go.AddComponent<SpriteRenderer>();
            cartView.sprite = cartSprite;
            cartView.sortingOrder = 9;
            for (int i = 0; i < 3; i++)
            {
                var box = new GameObject("Cargo " + i).AddComponent<SpriteRenderer>();
                box.transform.SetParent(go.transform, false);
                box.transform.localPosition = new Vector3((i - 1) * .28f, -.14f, 0);
                box.transform.localScale = Vector3.one * .42f;
                box.sprite = crateSprite;
                box.sortingOrder = 10;
                boxes.Add(box);
            }
            ready = true;
            GameManager.I.Player.vehicle = walk;
            SetLoad(5);
            Restart();
        }

        void Update()
        {
            if (!ready || Keyboard.current == null) return;
            var keys = Keyboard.current;
            if (keys.digit1Key.wasPressedThisFrame) SelectVehicle(false);
            if (keys.digit2Key.wasPressedThisFrame) SelectVehicle(true);
            if (keys.digit3Key.wasPressedThisFrame) SetLoad(0);
            if (keys.digit4Key.wasPressedThisFrame) SetLoad(5);
            if (keys.digit5Key.wasPressedThisFrame) SetLoad(10);
            if (keys.digit6Key.wasPressedThisFrame) SetLoad(-1);
            if (keys.rKey.wasPressedThisFrame) Restart();
            if (keys.tabKey.wasPressedThisFrame) ToggleOverview();
        }

        public void SelectVehicle(bool useCart)
        {
            GameManager.I.Player.vehicle = useCart ? cart : walk;
            SetLoad(LoadPreset);
            note = "이동 수단 변경 · R로 같은 출발점에서 비교할 수 있습니다";
        }

        public void SetLoad(int weight)
        {
            LoadPreset = weight;
            var p = GameManager.I.Player;
            int requested = weight < 0 ? Mathf.FloorToInt(p.Capacity / cargoItem.weight) : Mathf.FloorToInt(weight / cargoItem.weight);
            int count = Mathf.Min(requested, Mathf.FloorToInt(p.Capacity / cargoItem.weight));
            p.cargo.Clear();
            if (count > 0) p.cargo.Add(cargoItem, count);
            ClearDropped();
            player.ResetCarryState();
            ResetMeasurement();
        }

        void ClearDropped()
        {
            GameManager.I.Drops.Clear();
            foreach (var drop in FindObjectsByType<DroppedCargo>(FindObjectsSortMode.None)) Destroy(drop.gameObject);
        }

        public void Restart()
        {
            player.Motor.Teleport(start, Vector2Int.right);
            SetLoad(LoadPreset);
            follow.SnapNow();
            note = "새 시도 · 같은 화물 무게로 등짐과 손수레를 비교해 보세요";
        }

        public void JumpTo(int sample)
        {
            Vector3 pos = sample == 0 ? start : sample == 1 ? new Vector3(38.5f, 34.82f, 0)
                : sample == 2 ? new Vector3(38.5f, 22.82f, 0) : new Vector3(38.5f, 12.82f, 0);
            player.Motor.Teleport(pos, Vector2Int.right);
            player.ResetCarryState();
            ResetMeasurement();
            follow.SnapNow();
            note = "지형으로 바로 이동 · 이 구간의 속도와 흔들림을 확인하세요";
        }

        void ResetMeasurement()
        {
            TravelDistance = PeakShake = 0;
            DroppedCount = 0;
            previousCount = GameManager.I.Player.cargo.Count(cargoItem);
            previous = player.Motor.transform.position;
            for (int i = 0; i < terrainDistance.Length; i++) terrainDistance[i] = 0;
            arrived = false;
            fromStart = Vector2.Distance(previous, start) < 2;
        }

        public void ToggleOverview()
        {
            Overview = !Overview;
            pixels.enabled = !Overview;
            follow.enabled = !Overview;
            if (Overview)
            {
                view.rect = new Rect(0, .27f, 1, .48f);
                view.orthographicSize = Mathf.Max(23, 47 / view.aspect);
                view.transform.position = new Vector3(45, 21, -10);
            }
            else { view.rect = new Rect(0, 0, 1, 1); view.orthographicSize = 6.75f; follow.SnapNow(); }
        }

        void LateUpdate()
        {
            if (!ready) return;
            var p = GameManager.I.Player;
            Vector3 pos = player.Motor.transform.position;
            float distance = Vector2.Distance(previous, pos);
            if (distance < 1 && !arrived)
            {
                TravelDistance += distance;
                int grade = RoadGrid.Current.GradeAt(player.Motor.Cell);
                terrainDistance[Mathf.Clamp(grade, 0, 3)] += distance;
            }
            previous = pos;
            PeakShake = Mathf.Max(PeakShake, p.shake);
            int count = p.cargo.Count(cargoItem);
            if (count < previousCount) DroppedCount += previousCount - count;
            previousCount = count;
            if (!arrived && TravelDistance > 3 && new Rect(76, 7, 11, 12).Contains(player.Motor.Feet))
            {
                arrived = true;
                int main = 0;
                for (int i = 1; i < 4; i++) if (terrainDistance[i] > terrainDistance[main]) main = i;
                string road = new[] { "풀밭 중심", "오솔길 중심", "일반 길 중심", "큰길 중심" }[main];
                string result = $"{p.vehicle.displayName} · {road} · 이동 {TravelDistance:0}칸 · 낙하 {DroppedCount}개";
                Results.Insert(0, (fromStart ? "전체 경로: " : "구간 실험: ") + result);
                if (Results.Count > 3) Results.RemoveAt(3);
                note = "도착! 조건을 바꾸고 R로 다시 출발하세요";
            }
            cartView.gameObject.SetActive(CartSelected);
            if (CartSelected)
            {
                Vector2 facing = player.Motor.Facing;
                cartView.transform.position = pos - (Vector3)(facing * .95f) + new Vector3(0, -.1f, 0);
                cartView.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg);
                for (int i = 0; i < boxes.Count; i++) boxes[i].enabled = p.LoadRatio > i / 3f;
            }
        }

        void OnGUI()
        {
            if (!ready) return;
            UIStyle.Begin();
            float width = Mathf.Min(626, UIStyle.VirtualWidth - 12), x = (UIStyle.VirtualWidth - width) / 2;
            GUI.Box(new Rect(x, 5, width, 83), "", UIStyle.Box);
            GUI.Label(new Rect(x + 10, 9, width - 20, 16), "이동·운반 테스트    |    1 등짐 / 2 손수레    ·    R 재시작    ·    Tab 전체 보기", UIStyle.Rich);
            float bx = x + 10;
            bool Button(string label, float w) { bool hit = GUI.Button(new Rect(bx, 29, w, 20), label, UIStyle.Button); bx += w + 5; return hit; }
            if (Button("등짐 (1)", 68)) SelectVehicle(false);
            if (Button("손수레 (2)", 76)) SelectVehicle(true);
            if (Button("빈짐 (3)", 68)) SetLoad(0);
            if (Button("무게 5 (4)", 79)) SetLoad(5);
            if (Button("무게 10 (5)", 83)) SetLoad(10);
            if (Button("가득 (6)", 68)) SetLoad(-1);
            if (Button("처음으로 R", 85)) Restart();
            bool stamina = GUI.Toggle(new Rect(x + 10, 58, 92, 18), player.StaminaEnabled, "체력 소모");
            bool shake = GUI.Toggle(new Rect(x + 104, 58, 92, 18), player.CargoShakeEnabled, "짐 흔들림");
            if (stamina != player.StaminaEnabled || shake != player.CargoShakeEnabled)
            { player.StaminaEnabled = stamina; player.CargoShakeEnabled = shake; player.ResetCarryState(); ResetMeasurement(); }
            GUI.Label(new Rect(x + 203, 59, 48, 16), "바로 이동", UIStyle.Small);
            string[] locations = { "출발", "큰길", "오솔길", "풀밭" };
            for (int i = 0; i < locations.Length; i++)
                if (GUI.Button(new Rect(x + 250 + i * 58, 55, 54, 20), locations[i], UIStyle.Button)) JumpTo(i);
            if (GUI.Button(new Rect(x + width - 127, 55, 115, 20), Overview ? "Tab 가까이 보기" : "Tab 전체 보기", UIStyle.Button)) ToggleOverview();

            var p = GameManager.I.Player;
            int gradeNow = RoadGrid.Current.GradeAt(player.Motor.Cell);
            string[] grades = { "풀밭·여울", "오솔길", "길", "큰길" };
            float bottom = UIStyle.VirtualHeight - 96;
            GUI.Box(new Rect(x, bottom, width, 91), "", UIStyle.Box);
            GUI.Label(new Rect(x + 10, bottom + 5, width - 20, 16),
                $"{p.vehicle.displayName} · 화물 {p.TotalWeight:0}/{p.Capacity:0}    발밑: {grades[gradeNow]}    이동 속도 {player.Motor.Velocity.magnitude:0.0}칸/s", UIStyle.Rich);
            GUI.Label(new Rect(x + 10, bottom + 22, width - 20, 14),
                $"체력 {p.stamina:0%} · 흔들림 {p.shake:0%} · 이동 {TravelDistance:0}칸 · 낙하 {DroppedCount}개    |    WASD 이동 / Shift 달리기 / Space 짐 줍기", UIStyle.Small);
            GUI.Label(new Rect(x + 10, bottom + 37, width - 20, 14), note, UIStyle.Small);
            for (int i = 0; i < Results.Count; i++)
                GUI.Label(new Rect(x + 10, bottom + 51 + i * 11, width - 20, 12), Results[i], UIStyle.Small);
            Label(new Vector3(31, 34, 0), "큰길 · 길지만 손수레가 빠르고 안정적");
            Label(new Vector3(32, 24, 0), "오솔길 · 중간 거리 / 완만한 흔들림");
            Label(new Vector3(32, 12, 0), "풀밭 지름길 · 짧지만 수레는 느리고 덜컹");
            Label(new Vector3(81, 15, 0), "도착 깃발");
            if (Overview) Label(new Vector3(8, 17, 0), "출발");
            GUI.FocusControl(null);
        }

        void Label(Vector3 world, string text)
        {
            Vector3 point = view.WorldToScreenPoint(world);
            float px = point.x / UIStyle.Scale, py = (Screen.height - point.y) / UIStyle.Scale;
            if (point.z < 0 || px < 0 || px > UIStyle.VirtualWidth || py < 90 || py > UIStyle.VirtualHeight - 96) return;
            if (Overview) text = text.Split('·')[0].Trim();
            float labelWidth = Mathf.Min(270, UIStyle.CenterRich.CalcSize(new GUIContent(text)).x + 12);
            var rect = new Rect(px - labelWidth / 2, py - 17, labelWidth, 16);
            GUI.Box(rect, "", UIStyle.Box);
            GUI.Label(rect, text, UIStyle.CenterRich);
        }
    }
}
