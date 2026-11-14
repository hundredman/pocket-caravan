using System;
using System.Collections.Generic;
using System.IO;

namespace PocketCaravan
{
    /// <summary>Resolve persistent references before Apply mutates live state. Unknown cargo/progress is never silently dropped.</summary>
    public static class SaveCompatibility
    {
        static void Require(bool condition,string message) { if(!condition)throw new InvalidDataException(message); }

        /// <summary>
        /// 11/11 윤채: 실행 중에만 있는 물건 아이디. 게임 데이터(.asset)에 없고 만든 쪽이 이야기 단계에 맞춰 다시 넣는다.
        /// 예전 저장에 들어가 버린 것은 불러오기 전에 그 한 줄만 뺀다 (모르는 화물로 저장 복원이 통째로 막혔다).
        /// 새 저장에는 처음부터 들어가지 않는다 (GameManager 저장: 게임 데이터에 없는 물건은 건너뜀)
        /// </summary>
        public static readonly HashSet<string> RuntimeOnlyItems=new HashSet<string>{"guild_parcel"};

        public static void DropRuntimeOnly(SaveData data)
        {
            if(data?.cargoItems==null||data.cargoCounts==null||data.cargoCosts==null)return;
            if(data.cargoItems.Count!=data.cargoCounts.Count||data.cargoItems.Count!=data.cargoCosts.Count)return;   // 모양이 틀리면 Validate가 알린다
            for(int i=data.cargoItems.Count-1;i>=0;i--)
                if(RuntimeOnlyItems.Contains(data.cargoItems[i])){data.cargoItems.RemoveAt(i);data.cargoCounts.RemoveAt(i);data.cargoCosts.RemoveAt(i);}
        }
        public static void Validate(SaveData data,GameDatabase db,Func<string,bool> sceneAvailable)
        {
            SaveSystem.Validate(data);
            Require(db!=null,"게임 데이터가 없습니다.");
            if(!string.IsNullOrEmpty(data.scene) && sceneAvailable!=null)
                Require(sceneAvailable(GoryeoLayout.Resolve(data.scene)),"현재 빌드에 없는 저장 맵: "+data.scene);
            foreach(string id in data.cargoItems)
                Require(db.items.Exists(i=>i!=null && i.id==id),"현재 데이터에 없는 화물: "+id);
            if(!string.IsNullOrEmpty(data.vehicle))
                Require(db.vehicles.Exists(v=>v!=null && v.id==data.vehicle),"현재 데이터에 없는 이동 수단: "+data.vehicle);
            if (data.q1MigrationVersion == 1)
            {
                var legacyIds = new HashSet<string>();
                foreach (string id in data.legacyMainFinishedIds)
                    Require(!string.IsNullOrEmpty(id) && legacyIds.Add(id) && data.finishedQuests.Contains(id)
                        && db.quests.Exists(q => q != null && q.id == id), "옛 메인 완료 이주 기록이 맞지 않습니다.");
            }
            if(data.tradeActivity!=null&&data.tradeActivity.schemaVersion==1)
            {
                foreach(var p in data.tradeActivity.purchases)
                    Require(db.towns.Exists(t=>t!=null&&t.id==p.town)&&db.items.Exists(i=>i!=null&&i.id==p.item),"최근 매입 기록 대상을 찾을 수 없습니다.");
                foreach(var d in data.tradeActivity.daily)Require(db.towns.Exists(t=>t!=null&&t.id==d.town),"일일 판매 마을을 찾을 수 없습니다.");
            }
            var activeIds=new HashSet<string>();
            foreach(var progress in data.quests)
            {
                Require(progress!=null && !string.IsNullOrEmpty(progress.id),"의뢰 진행 ID가 없습니다.");
                var quest=db.quests.Find(q=>q!=null && q.id==progress.id);
                Require(quest!=null,"현재 데이터에 없는 진행 의뢰: "+progress.id);
                Require(activeIds.Add(progress.id),"의뢰 진행 ID가 중복됐습니다: "+progress.id);
                if (progress.rewardCaptured && progress.acceptedReputationSize != ReputationSize.None)
                {
                    Require(Enum.IsDefined(typeof(ReputationSize), progress.acceptedReputationSize)
                        && Enum.IsDefined(typeof(ReputationScope), progress.acceptedReputationScope)
                        && !string.IsNullOrWhiteSpace(progress.acceptedReputationTarget), "의뢰 평판 보상 기록이 올바르지 않습니다: " + progress.id);
                }
                Require(progress.step>=0 && progress.step<=quest.objectives.Count && progress.amount>=0,"의뢰 진행 단계가 맞지 않습니다: "+progress.id);
            }
            foreach(string id in data.finishedQuests)
                Require(db.quests.Exists(q=>q!=null && q.id==id),"현재 데이터에 없는 완료 의뢰: "+id);
            foreach(var market in data.markets)
            {
                Require(market!=null && db.towns.Exists(t=>t!=null && t.id==market.town),"현재 데이터에 없는 장터 기록입니다.");
                Require(market.items!=null && market.stocks!=null && market.items.Count==market.stocks.Count,"장터 재고 목록이 맞지 않습니다.");
                for(int i=0;i<market.items.Count;i++)
                {
                    string id=market.items[i];
                    Require(db.items.Exists(item=>item!=null && item.id==id),"현재 데이터에 없는 재고 품목: "+id);
                    Require(Finite(market.stocks[i]) && market.stocks[i]>=0,"장터 재고 수치가 올바르지 않습니다.");
                }
                Require(Finite(market.volume) && market.volume>=0,"장터 거래액이 올바르지 않습니다.");
            }
            foreach(var board in data.boards)
                Require(board!=null && board.list!=null && !board.list.Contains(null) && db.towns.Exists(t=>t!=null && t.id==board.town),"게시판 기록을 현재 지역에서 찾을 수 없습니다.");
            var contractIds=new HashSet<int>();
            foreach(var contract in data.contracts)
            {
                Require(contract!=null && contract.id>0 && contractIds.Add(contract.id),"맡은 배달 ID가 없거나 중복됐습니다.");
                Require(db.towns.Exists(t=>t!=null && t.id==contract.fromTown) && db.towns.Exists(t=>t!=null && t.id==contract.toTown),"맡은 배달의 출발지/목적지를 찾을 수 없습니다.");
                Require(Finite(contract.weight) && contract.weight>0 && contract.reward>=0 && Finite(contract.condition) && contract.condition>=0 && contract.condition<=1,"맡은 배달의 화물/보수가 올바르지 않습니다.");
                Require(Finite(contract.hours) && Finite(contract.acceptedAt) && Finite(contract.deadline) && Finite(contract.finalDeadline),"맡은 배달의 기한이 올바르지 않습니다.");
            }
            Require(Finite(data.playHours) && data.playHours>=0,"의뢰 시간 기록이 올바르지 않습니다.");   // 0 = 2026-11-08 이전 저장
            // Unknown storedCargo/drop IDs intentionally remain recoverable records under existing migration rules.
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
