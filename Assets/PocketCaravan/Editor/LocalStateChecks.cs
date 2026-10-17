using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PocketCaravan.EditorTools
{
    /// <summary>비시작 씬의 격리 세션에서 실행하는 지급·회수·저장 회귀 검사. 본편 파일은 쓰지 않는다.</summary>
    public static class LocalStateChecks
    {
        [Serializable] public class Report { public List<string> passed = new List<string>(); }

        [MenuItem("Pocket Caravan/Tests/로컬 의뢰·회수·저장 검사")]
        public static void RunMenu() => Debug.Log(JsonUtility.ToJson(Run(), true));

        public static Report Run()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().buildIndex == 0 || GameManager.I == null
                || MapRoot.Current == null || PlayerController.Instance == null)
                throw new InvalidOperationException("Dakmok 같은 비시작 씬에서 Play한 뒤 실행하세요. 실행 중 진행은 격리 시험 상태로 바뀝니다.");
            var gm = GameManager.I;
            gm.EnterTestSession();
            var report = new Report();
            void Check(string name, bool ok)
            {
                if (!ok) throw new Exception("회귀 검사 실패: " + name);
                report.passed.Add(name);
            }

            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var a = ScriptableObject.CreateInstance<TownData>(); a.id = "test_a"; a.worldCoord = Vector2Int.zero;
            var b = ScriptableObject.CreateInstance<TownData>(); b.id = "test_b"; b.worldCoord = Vector2Int.right;
            var item = gm.Db.items.Find(i => i != null && !i.questItem && i.weight <= 2f);
            db.towns.Add(a); db.towns.Add(b);
            Action<Contract, bool> counted = null;
            QuestData quest = null;
            try
            {
                gm.GetComponent<AudioManager>().EnsureListener();
                int listeners = 0;
                foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                    if (listener.isActiveAndEnabled) listeners++;
                Check("2D 씬 활성 오디오 청취점 하나", listeners == 1);
                var p = new PlayerState { money = 0, credit = -5 };
                var clock = new GameClock();
                var cm = new ContractManager(db, clock, p); cm.InitBoards();
                Check("게시판 일반 의뢰 보장", cm.Available(a.id).Exists(c => !c.Premium));
                var c = cm.Available(a.id).Find(x => !x.Premium);
                Check("신용 음수·빈 지갑도 일반 배달 수락", cm.CanAccept(c) == null);
                Check("다른 게시판의 의뢰 수락 거절", !cm.Accept(b.id, c));
                Check("게시되지 않은 의뢰 수락 거절", !cm.Accept(a.id, new Contract { id = 999, fromTown = a.id, toTown = b.id }));
                Check("정상 의뢰 수락", cm.Accept(a.id, c));
                Check("같은 의뢰 재수락 거절", !cm.Accept(a.id, c) && p.contracts.Count == 1);
                Check("잘못된 도착지 납품 거절", !cm.Deliver(c, a.id).success && p.contracts.Contains(c) && p.money == 0);
                c.dropped = true;
                Check("낙하 상태 납품 거절", !cm.Deliver(c, b.id).success && p.contracts.Contains(c));
                c.dropped = false;
                int events = 0;
                counted = (finished, ok) => { if (finished == c) events++; };
                GameEvents.ContractFinished += counted;
                int previousCredit = p.credit;
                var delivered = cm.Deliver(c, b.id);
                Check("실제 배달로 잔고와 신용 회복", delivered.success && delivered.paid > 0 && p.money == delivered.paid && p.credit == previousCredit + 1);
                int money = p.money, credit = p.credit;
                var duplicate = cm.Deliver(c, b.id);
                Check("납품 재호출 지급·신용·이벤트 중복 없음", !duplicate.success && duplicate.paid == 0 && p.money == money && p.credit == credit && events == 1);
                GameEvents.ContractFinished -= counted; counted = null;

                var lost = cm.Available(a.id)[0]; lost.minCredit = 0;
                Check("다음 의뢰 수락", cm.Accept(a.id, lost));
                cm.Lose(lost); money = p.money; credit = p.credit;
                cm.Lose(lost);
                Check("분실 재호출 배상·신용 중복 없음", p.money == money && p.credit == credit);

                var expiry = cm.Available(a.id)[0]; expiry.minCredit = 0;
                cm.Accept(a.id, expiry);
                clock.Advance(expiry.hours * 1.5f + 1f); cm.OnHour();
                money = p.money; credit = p.credit;
                Check("만료 후 납품 보상 없음", !cm.Deliver(expiry, b.id).success && p.money == money && p.credit == credit);

                var premium = new Contract { id = 9999, minCredit = 3, weight = 1 };
                Check("낮은 신용의 고급 의뢰 제한 유지", cm.CanAccept(premium) != null);
                var overweight = new Contract { id = 9998, weight = p.Capacity + 1 };
                Check("일반 의뢰도 적재 한도 검사", cm.CanAccept(overweight) != null);
                foreach (var board in cm.boards) foreach (var offer in board.list) offer.minCredit = 3;
                cm.InitBoards();
                Check("이전 저장의 고급 의뢰만 있는 게시판 복구", cm.Available(a.id).Exists(x => !x.Premium));
                db.towns.Remove(b);
                var single = new ContractManager(db, new GameClock(), new PlayerState()); single.InitBoards();
                Check("마을 하나만 있는 지역 데이터 부팅", single.Available(a.id).Count == 0);
                db.towns.Clear();
                new ContractManager(db, new GameClock(), new PlayerState()).InitBoards();
                Check("마을 없는 지역 데이터 부팅", true);

                gm.Player.cargo.Clear(); gm.Player.contracts.Clear(); gm.Drops.Clear();
                var record = new DropRecord { mapId = MapRoot.Current.mapId, itemId = item.id, label = item.displayName, unitCost = 13.5f };
                gm.Drops.Add(record);
                var drop = DroppedCargo.Spawn(record, item.icon);
                drop.Interact(PlayerController.Instance); drop.Interact(PlayerController.Instance);
                Check("같은 낙하물 재호출 중복 지급 없음", gm.Player.cargo.Count(item) == 1 && !gm.Drops.Contains(record));
                Check("회수 화물의 매입 원가 보존", Mathf.Approximately(gm.Player.cargo.AverageCost(item), 13.5f));
                gm.Player.cargo.Clear();
                gm.Player.cargo.Add(item, Mathf.CeilToInt(gm.Player.Capacity / item.weight));
                var full = new DropRecord { mapId = MapRoot.Current.mapId, itemId = item.id, label = "가득 찬 상태 검사" };
                gm.Drops.Add(full); var fullDrop = DroppedCargo.Spawn(full, item.icon);
                fullDrop.Interact(PlayerController.Instance);
                Check("적재 초과 회수 시 낙하물 보존", gm.Drops.Contains(full));
                gm.Player.cargo.Clear(); fullDrop.Interact(PlayerController.Instance);
                Check("적재 여유 후 같은 짐 회수 가능", !gm.Drops.Contains(full) && gm.Player.cargo.Count(item) == 1);
                var orphan = new DropRecord { mapId = MapRoot.Current.mapId, contractId = 987654, label = "끝난 의뢰 짐" };
                gm.Drops.Add(orphan); DroppedCargo.Spawn(orphan, null).Interact(PlayerController.Instance);
                Check("끝난 의뢰의 남은 낙하물 정리", !gm.Drops.Contains(orphan));

                var saved = new SaveData { money = 42, shake = .83f, stamina = .4f, day = 2, minute = 600, nextContractId = 5000 };
                foreach (var town in gm.Db.towns)
                {
                    var board = new ContractManager.Board { town = town.id };
                    for (int n = 0; n < ContractManager.BoardSize; n++)
                        board.list.Add(new Contract { id = 6000 + saved.boards.Count * 10 + n, minCredit = 3 });
                    saved.boards.Add(board);
                }
                var roundtrip = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saved));
                typeof(GameManager).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, new object[] { roundtrip });
                Check("이전 흔들림 폐기·체력·잔고 복원", Mathf.Approximately(gm.Player.shake, 0f) && Mathf.Approximately(gm.Player.stamina, .4f) && gm.Player.money == 42);
                var ids = new HashSet<int>(); bool unique = true;
                foreach (var board in gm.Contracts.boards) foreach (var offer in board.list) unique &= ids.Add(offer.id);
                Check("불러오기 중 일반 의뢰 보충 ID 충돌 없음", unique && gm.Contracts.nextId > 6022);
                var legacy = JsonUtility.FromJson<SaveData>("{\"money\":9,\"stamina\":1,\"day\":1,\"minute\":480}");
                typeof(GameManager).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, new object[] { legacy });
                Check("이전 저장의 누락 흔들림은 0", gm.Player.money == 9 && Mathf.Approximately(gm.Player.shake, 0f));
                quest = ScriptableObject.CreateInstance<QuestData>(); quest.id = "audit_board_quest";
                quest.objectives.Add(new QuestObjective { type = ObjectiveType.CompleteContract, targetId = a.id, count = 1 });
                var qm = new QuestManager(new List<QuestData> { quest }); qm.Accept(quest);
                GameEvents.RaiseContractFinished(new Contract { fromTown = b.id }, true);
                Check("다른 마을 의뢰로 지정 게시판 퀘스트 진행 안 됨", !qm.IsFinished(quest));
                GameEvents.RaiseContractFinished(new Contract { fromTown = a.id }, false);
                Check("실패한 의뢰로 배달 성공 퀘스트 진행 안 됨", !qm.IsFinished(quest));
                GameEvents.RaiseContractFinished(new Contract { fromTown = a.id }, true);
                Check("지정 마을의 실제 성공으로 퀘스트 완료", qm.IsFinished(quest));
            }
            finally
            {
                if (counted != null) GameEvents.ContractFinished -= counted;
                UnityEngine.Object.Destroy(db); UnityEngine.Object.Destroy(a); UnityEngine.Object.Destroy(b);
                if (quest != null) UnityEngine.Object.Destroy(quest);
            }
            Directory.CreateDirectory("output/comprehensive-audit");
            File.WriteAllText("output/comprehensive-audit/local-state-checks.json", JsonUtility.ToJson(report, true));
            return report;
        }
    }
}
